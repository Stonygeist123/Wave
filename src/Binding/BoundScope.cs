using System.Collections.Immutable;
using Wave.Source.Binding.BoundNodes;
using Wave.Symbols;

namespace Wave.Source.Binding
{
    internal sealed class BoundScope
    {
        private readonly Dictionary<string, VariableSymbol> _variables = new();
        private readonly Dictionary<string, FunctionSymbol> _functions = new();
        public BoundScope(BoundScope? parent) => Parent = parent;
        public bool TryDeclareVar(VariableSymbol variable, bool force = false)
        {
            if (TryLookupVar(variable.Name, out _) && !force)
                return false;

            _variables.Add(variable.Name, variable);
            return true;
        }

        public bool TryLookupVar(string name, out VariableSymbol? variable)
        {
            if (_variables.TryGetValue(name, out variable))
                return true;
            if (Parent is null)
                return false;
            return Parent.TryLookupVar(name, out variable);
        }

        public bool TryDeclareFn(FunctionSymbol fn)
        {
            if (TryLookupFn(fn.Name, out _))
                return false;

            _functions.Add(fn.Name, fn);
            return true;
        }

        public bool TryLookupFn(string name, out FunctionSymbol? function)
        {
            function = _functions.Any(fn => fn.Key == name) ? _functions.SingleOrDefault(f => f.Key == name).Value : null;
            if (function is not null)
                return true;
            if (Parent is null)
                return false;
            return Parent.TryLookupFn(name, out function);
        }

        public ImmutableArray<VariableSymbol> GetDeclaredVars() => _variables.Values.ToImmutableArray();
        public ImmutableArray<FunctionSymbol> GetDeclaredFns() => _functions.Values.ToImmutableArray();
        public ImmutableArray<VariableSymbol> GetVariables()
        {
            ImmutableArray<VariableSymbol>.Builder vars = ImmutableArray.CreateBuilder<VariableSymbol>();
            BoundScope? scope = this;
            while (scope is not null)
            {
                foreach (VariableSymbol v in scope.GetDeclaredVars())
                    vars.Add(v);

                scope = scope.Parent;
            }

            return vars.ToImmutableArray();
        }

        public ImmutableArray<FunctionSymbol> GetFunctions()
        {
            ImmutableArray<FunctionSymbol>.Builder fns = ImmutableArray.CreateBuilder<FunctionSymbol>();
            BoundScope? scope = this;
            while (scope is not null)
            {
                foreach (FunctionSymbol fn in scope.GetDeclaredFns())
                    fns.Add(fn);

                scope = scope.Parent;
            }

            return fns.ToImmutableArray();
        }

        public BoundScope? Parent { get; }
    }

    internal sealed class BoundGlobalScope
    {
        public BoundGlobalScope(BoundGlobalScope? previous, ImmutableArray<Diagnostic> diagnostics, ImmutableArray<VariableSymbol> variables, ImmutableArray<FunctionSymbol> functions, BoundBlockStmt stmt)
        {
            Previous = previous;
            Diagnostics = diagnostics;
            Variables = variables;
            Functions = functions;
            Stmt = stmt;
        }

        public BoundGlobalScope? Previous { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public ImmutableArray<VariableSymbol> Variables { get; }
        public ImmutableArray<FunctionSymbol> Functions { get; }
        public BoundBlockStmt Stmt { get; }
    }
}
