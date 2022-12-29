using Wave.Binding;
using Wave.Binding.BoundNodes;

namespace Wave
{
    public class EvaluationResult
    {
        public EvaluationResult(IEnumerable<Diagnostic> diagnostics, object? value)
        {
            Diagnostics = diagnostics.ToArray();
            Value = value;
        }

        public IReadOnlyList<Diagnostic> Diagnostics { get; }
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
            IEnumerable<Diagnostic> diagnostics = SyntaxTree.Diagnostics.Concat(binder.Diagnostics);
            if (diagnostics.Any())
                return new(diagnostics, null);

            Evaluator evaluator = new(boundExpr, variables);
            object value = evaluator.Evaluate();
            return new(Array.Empty<Diagnostic>(), value);
        }
    }
}