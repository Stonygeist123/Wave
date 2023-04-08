using System.CodeDom.Compiler;
using System.Collections.Immutable;
using Wave.IO;
using Wave.Source.Binding;
using Wave.Source.Binding.BoundNodes;
using Wave.Source.Syntax;
using Wave.Source.Syntax.Nodes;
using Wave.src.Binding.BoundNodes;
using Wave.Symbols;

namespace Wave.Source.Compilation
{
    public class EvaluationResult
    {
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public object? Value { get; }
        public EvaluationResult(ImmutableArray<Diagnostic> diagnostics, object? value)
        {
            Diagnostics = diagnostics;
            Value = value;
        }
    }

    public sealed class Compilation
    {
        private BoundGlobalScope? _globalScope;
        public FunctionSymbol? MainFn => GlobalScope.MainFn;
        public ImmutableArray<FunctionSymbol> Functions => GlobalScope.Functions;
        public ImmutableArray<VariableSymbol> Variables => GlobalScope.Variables;
        public bool IsScript { get; }
        public Compilation? Previous { get; }
        public ImmutableArray<SyntaxTree> SyntaxTrees { get; }
        public Compilation(bool isScript, Compilation? previous, params SyntaxTree[] syntaxTrees)
        {
            IsScript = isScript;
            Previous = previous;
            SyntaxTrees = syntaxTrees.ToImmutableArray();
        }

        public static Compilation Create(params SyntaxTree[] syntaxTrees) => new(false, null, syntaxTrees);
        public static Compilation CreateScript(Compilation? previous, SyntaxTree syntaxTree) => new(true, previous, syntaxTree);
        public BoundGlobalScope GlobalScope
        {
            get
            {
                if (_globalScope is null)
                {
                    BoundGlobalScope globalScope = Binder.BindGlobalScope(IsScript, Previous?.GlobalScope, SyntaxTrees);
                    Interlocked.CompareExchange(ref _globalScope, globalScope, null);
                }

                return _globalScope;
            }
        }

        private BoundProgram GetProgram() => Binder.BindProgram(IsScript, Previous?.GetProgram(), GlobalScope);
        public IEnumerable<Symbol> GetSymbols()
        {
            Compilation? submission = this;
            HashSet<string> seenNames = new();
            while (submission is not null)
            {
                foreach (FunctionSymbol fn in submission.Functions)
                    if (seenNames.Add(fn.Name))
                        yield return fn;

                foreach (VariableSymbol var in submission.Variables)
                    if (seenNames.Add(var.Name))
                        yield return var;

                List<FunctionSymbol?> builtInFns = typeof(BuiltInFunctions).GetFields().Where(fi => fi.FieldType == typeof(FunctionSymbol)).Select(fi => (FunctionSymbol?)fi.GetValue(null)).ToList();
                foreach (FunctionSymbol? biFn in builtInFns)
                    if (biFn is not null && seenNames.Add(biFn.Name))
                        yield return biFn;

                submission = submission.Previous;
            }
        }

        public Compilation ContinueWith(SyntaxTree syntaxTree) => new(IsScript, this, syntaxTree);
        public EvaluationResult Evaluate(Dictionary<VariableSymbol, object?> variables)
        {
            ImmutableArray<Diagnostic> diagnostics = SyntaxTrees.SelectMany(s => s.Diagnostics).Concat(GlobalScope.Diagnostics).ToImmutableArray();
            if (diagnostics.Any())
                return new(diagnostics, null);

            BoundProgram program = GetProgram()!;
            /* string appPath = Environment.GetCommandLineArgs()[0];
            string? appDirectory = Path.GetDirectoryName(appPath);
            string cfgPath = Path.Combine(appDirectory ?? "", "cfg.dot");

            BoundBlockStmt cfgStmt = !program.Stmt.Stmts.Any() && program.Functions.Any()
                            ? program.Functions.Last().Value
                            : program.Stmt;

            ControlFlowGraph cfg = ControlFlowGraph.CreateGraph(cfgStmt);
            using (StreamWriter writer = new(cfgPath))
                cfg.WriteTo(writer); */

            if (program.Diagnostics.Any())
                return new EvaluationResult(program.Diagnostics, null);

            Evaluator evaluator = new(program, variables);
            object? value = evaluator.Evaluate();
            return new(ImmutableArray<Diagnostic>.Empty, value);
        }

        public void EmitTree(TextWriter writer)
        {
            if (GlobalScope.MainFn is not null)
                EmitTree(GlobalScope.MainFn!, writer);
            else if (GlobalScope.ScriptFn is not null)
                EmitTree(GlobalScope.ScriptFn!, writer);
        }

        public void EmitTree(FunctionSymbol symbol, TextWriter writer)
        {
            BoundProgram program = Binder.BindProgram(IsScript, GetProgram(), GlobalScope);
            symbol.WriteTo(writer);
            if (!program.Functions.TryGetValue(symbol, out BoundBlockStmt? body))
                return;

            writer.WriteSpace();
            if (body.Stmts.Length == 1 && body.Stmts.First() is BoundExpressionStmt e)
            {
                writer.WritePunctuation(SyntaxKind.LBrace);
                writer.WriteLine();
                if (writer is IndentedTextWriter iw)
                    ++iw.Indent;
                else
                    writer.Write(IndentedTextWriter.DefaultTabString);

                writer.WriteKeyword(SyntaxKind.Ret);
                writer.WriteSpace();
                e.Expr.WriteTo(writer);
                writer.WritePunctuation(SyntaxKind.Semicolon);
                writer.WriteLine();

                if (writer is IndentedTextWriter iw1)
                    --iw1.Indent;

                writer.WritePunctuation(SyntaxKind.RBrace);
                writer.WriteLine();
            }
            else
                body.WriteTo(writer);
        }
    }
}