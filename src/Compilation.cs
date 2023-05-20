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
    public sealed class Compilation
    {
        private BoundGlobalScope? _globalScope;
        public FunctionSymbol? MainFn => GlobalScope.MainFn;
        public ImmutableArray<FunctionSymbol> Functions => GlobalScope.Functions;
        public ImmutableArray<VariableSymbol> Variables => GlobalScope.Variables;
        public ImmutableArray<ClassSymbol> Classes => GlobalScope.Classes;
        public ImmutableArray<ADTSymbol> ADTs => GlobalScope.ADTs;
        public ImmutableArray<NamespaceSymbol> Namespaces => GlobalScope.Namespaces;
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

                foreach (ClassSymbol c in submission.Classes)
                    if (seenNames.Add(c.Name))
                        yield return c;

                foreach (ADTSymbol adt in submission.ADTs)
                    if (seenNames.Add(adt.Name))
                        yield return adt;

                foreach (NamespaceSymbol ns in submission.Namespaces)
                    if (seenNames.Add(ns.Name))
                    {
                        foreach (Symbol symbol in ns.Classes.Cast<Symbol>()
                            .Concat(ns.ADTs).Cast<Symbol>()
                            .Concat(ns.Fns.Select(x => x.Key)).Cast<Symbol>()
                            .Concat(ns.Namespaces)
                            .Select(s => { s.Name = $"{ns.Name}::{s.Name}"; return s; }))
                            yield return symbol;
                    }

                List<NamespaceSymbol_Std?> stdLib = typeof(StdLib).GetFields().Where(fi => fi.FieldType == typeof(NamespaceSymbol_Std)).Select(fi => (NamespaceSymbol_Std?)fi.GetValue(null)).ToList();
                foreach (NamespaceSymbol_Std? std in stdLib)
                    if (std is not null && seenNames.Add(std.Name))
                        yield return std;

                submission = submission.Previous;
            }
        }

        public Compilation ContinueWith(SyntaxTree syntaxTree) => new(IsScript, this, syntaxTree);
        public ImmutableArray<Diagnostic> Evaluate(Dictionary<VariableSymbol, object?> variables)
        {
            ImmutableArray<Diagnostic> diagnostics = SyntaxTrees.SelectMany(s => s.Diagnostics).Concat(GlobalScope.Diagnostics).ToImmutableArray();
            if (diagnostics.Any())
                return diagnostics;

            BoundProgram program = GetProgram()!;
            if (program.Diagnostics.Any())
                return program.Diagnostics;

            Evaluator evaluator = new(program, variables);
            evaluator.Evaluate();
            return ImmutableArray<Diagnostic>.Empty;
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