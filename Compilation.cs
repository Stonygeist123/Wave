using System.Collections.Immutable;
using Wave.Binding;
using Wave.Binding.BoundNodes;

namespace Wave
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
        public SyntaxTree SyntaxTree { get; }
        public Compilation? Previous { get; }
        public Compilation(SyntaxTree syntaxTree)
            : this(null, syntaxTree)
        { }

        public Compilation(Compilation? previous, SyntaxTree syntaxTree)
        {
            SyntaxTree = syntaxTree;
            Previous = previous;
        }

        internal BoundGlobalScope GlobalScope
        {
            get
            {
                if (_globalScope is null)
                {
                    BoundGlobalScope globalScope = Binder.BindGlobalScope(Previous?.GlobalScope, SyntaxTree.Root);
                    Interlocked.CompareExchange(ref _globalScope, globalScope, null);
                }

                return _globalScope;
            }
        }


        public Compilation ContinueWith(SyntaxTree syntaxTree) => new(this, syntaxTree);

        public EvaluationResult Evaluate(Dictionary<VariableSymbol, object?> variables)
        {
            ImmutableArray<Diagnostic> diagnostics = SyntaxTree.Diagnostics.Concat(GlobalScope.Diagnostics).ToImmutableArray();
            if (diagnostics.Any())
                return new(diagnostics, null);

            Evaluator evaluator = new(GetStmt(), variables);
            object? value = evaluator.Evaluate();
            return new(ImmutableArray<Diagnostic>.Empty, value);
        }

        public void EmitTree(TextWriter writer)
        {
            BoundStmt stmt = GetStmt();
            stmt.WriteTo(writer);
        }

        private BoundBlockStmt GetStmt() => Lowerer.Lowerer.Lower(GlobalScope.Stmt);
    }
}