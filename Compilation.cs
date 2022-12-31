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
        public SyntaxTree SyntaxTree { get; }
        public Compilation(SyntaxTree syntaxTree) => SyntaxTree = syntaxTree;

        public EvaluationResult Evaluate(Dictionary<VariableSymbol, object?> variables)
        {
            Binder binder = new(variables);
            BoundExpr boundExpr = binder.BindExpr(SyntaxTree.Root);
            ImmutableArray<Diagnostic> diagnostics = SyntaxTree.Diagnostics.Concat(binder.Diagnostics).ToImmutableArray();
            if (diagnostics.Any())
                return new(diagnostics, null);

            Evaluator evaluator = new(boundExpr, variables);
            object value = evaluator.Evaluate();
            return new(ImmutableArray<Diagnostic>.Empty, value);
        }
    }
}