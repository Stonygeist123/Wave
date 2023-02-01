using System.CodeDom.Compiler;
using System.Collections.Immutable;
using Wave.IO;
using Wave.Lowering;
using Wave.Source.Binding;
using Wave.Source.Binding.BoundNodes;
using Wave.Source.Syntax;
using Wave.Source.Syntax.Nodes;
using Wave.src.Binding.BoundNodes;
using Wave.Symbols;
using BindingFlags = System.Reflection.BindingFlags;

namespace Wave.Source
{
    public class EvaluationResult
    {
        public EvaluationResult(ImmutableArray<Diagnostic> diagnostics, object? value)
        {
            Diagnostics = diagnostics;
            Value = value;
        }

        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public object? Value { get; }
    }

    public sealed class Compilation
    {
        private BoundGlobalScope? _globalScope;
        public ImmutableArray<SyntaxTree> SyntaxTrees { get; }
        public ImmutableArray<FunctionSymbol> Functions => GlobalScope.Functions;
        public ImmutableArray<VariableSymbol> Variables => GlobalScope.Variables;
        public Compilation? Previous { get; }
        public Compilation(params SyntaxTree[] syntaxTrees)
            : this(null, syntaxTrees) { }

        public Compilation(Compilation? previous, params SyntaxTree[] syntaxTrees)
        {
            Previous = previous;
            SyntaxTrees = syntaxTrees.ToImmutableArray();
        }

        internal BoundGlobalScope GlobalScope
        {
            get
            {
                if (_globalScope is null)
                {
                    BoundGlobalScope globalScope = Binder.BindGlobalScope(Previous?.GlobalScope, SyntaxTrees);
                    Interlocked.CompareExchange(ref _globalScope, globalScope, null);
                }

                return _globalScope;
            }
        }

        public IEnumerable<Symbol> GetSymbols()
        {
            Compilation? submission = this;
            HashSet<string> seenNames = new();
            while (submission is not null)
            {
                const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
                List<FunctionSymbol?> builtInFns = typeof(BuiltInFunctions).GetFields().Where(fi => fi.FieldType == typeof(FunctionSymbol)).Select(fi => (FunctionSymbol?)fi.GetValue(null)).ToList();

                foreach (FunctionSymbol? biFn in builtInFns)
                    if (biFn is not null && seenNames.Add(biFn.Name))
                        yield return biFn;

                foreach (FunctionSymbol fn in submission.Functions)
                    if (seenNames.Add(fn.Name))
                        yield return fn;

                foreach (VariableSymbol var in submission.Variables)
                    if (seenNames.Add(var.Name))
                        yield return var;

                submission = submission.Previous;
            }
        }

        public Compilation ContinueWith(SyntaxTree syntaxTree) => new(this, syntaxTree);
        public EvaluationResult Evaluate(Dictionary<VariableSymbol, object?> variables)
        {
            ImmutableArray<Diagnostic> diagnostics = SyntaxTrees.SelectMany(s => s.Diagnostics).Concat(GlobalScope.Diagnostics).ToImmutableArray();
            if (diagnostics.Any())
                return new(diagnostics, null);

            BoundProgram program = Binder.BindProgram(GlobalScope);

            string appPath = Environment.GetCommandLineArgs()[0];
            string? appDirectory = Path.GetDirectoryName(appPath);
            string cfgPath = Path.Combine(appDirectory ?? "", "cfg.dot");

            BoundBlockStmt cfgStmt = !program.Stmt.Stmts.Any() && program.Functions.Any()
                            ? program.Functions.Last().Value
                            : program.Stmt;

            ControlFlowGraph cfg = ControlFlowGraph.CreateGraph(cfgStmt);
            using (StreamWriter writer = new(cfgPath))
                cfg.WriteTo(writer);

            if (program.Diagnostics.Any())
                return new EvaluationResult(program.Diagnostics, null);

            Evaluator evaluator = new(program.Functions, GetStmt(), variables);
            object? value = evaluator.Evaluate();
            return new(ImmutableArray<Diagnostic>.Empty, value);
        }

        public void EmitTree(TextWriter writer)
        {
            BoundProgram program = Binder.BindProgram(GlobalScope);
            if (program.Stmt.Stmts.Length > 0)
                program.Stmt.WriteTo(writer);
            else
                foreach (KeyValuePair<FunctionSymbol, BoundBlockStmt> fn in program.Functions)
                {
                    if (!GlobalScope.Functions.Contains(fn.Key))
                        continue;

                    fn.Key.WriteTo(writer);
                    fn.Value.WriteTo(writer);
                }
        }

        public void EmitTree(FunctionSymbol symbol, TextWriter writer)
        {
            BoundProgram program = Binder.BindProgram(GlobalScope);
            symbol.WriteTo(writer);
            if (!program.Functions.TryGetValue(symbol, out BoundBlockStmt? body))
                return;

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

        private BoundBlockStmt GetStmt() => Lowerer.Lower(GlobalScope.Stmt);
    }
}