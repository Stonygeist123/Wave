using System.Collections.Immutable;
using Wave.Binding.BoundNodes;

namespace Wave.Binding
{
    internal sealed class BoundScope
    {
        private readonly Dictionary<string, VariableSymbol> _variables = new();
        public BoundScope(BoundScope? parent)
        {
            Parent = parent;
        }

        public bool TryDeclare(VariableSymbol variable)
        {
            if (_variables.ContainsKey(variable.Name) || (Parent is not null && Parent.TryLookup(variable.Name, out _)))
                return false;

            _variables.Add(variable.Name, variable);
            return true;
        }

        public bool TryLookup(string name, out VariableSymbol? variable)
        {
            if (_variables.TryGetValue(name, out variable))
                return true;
            if (Parent is null)
                return false;
            return Parent.TryLookup(name, out variable);
        }

        public ImmutableArray<VariableSymbol> GetDeclaredVars() => _variables.Values.ToImmutableArray();

        public BoundScope? Parent { get; }
    }

    internal sealed class BoundGlobalScope
    {
        public BoundGlobalScope(BoundGlobalScope? previous, ImmutableArray<Diagnostic> diagnostics, ImmutableArray<VariableSymbol> variables, BoundStmt stmt)
        {
            Previous = previous;
            Diagnostics = diagnostics;
            Variables = variables;
            Stmt = stmt;
        }

        public BoundGlobalScope? Previous { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public ImmutableArray<VariableSymbol> Variables { get; }
        public BoundStmt Stmt { get; }
    }
}
