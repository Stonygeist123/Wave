using System.Collections.Immutable;
using Wave.Lowering;
using Wave.Source.Binding;
using Wave.Source.Binding.BoundNodes;
using Wave.Source.Syntax;
using Wave.Symbols;

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

            ControlFlowGraph cfg = ControlFlowGraph.Create(cfgStmt);
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

        private BoundBlockStmt GetStmt() => Lowerer.Lower(GlobalScope.Stmt);
    }
}