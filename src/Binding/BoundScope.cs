using System.Collections.Immutable;
using Wave.Source.Binding.BoundNodes;
using Wave.Symbols;

namespace Wave.Source.Binding
{
    public sealed class BoundScope
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
            if (TryLookupFn(fn.Name, out FunctionSymbol[]? foundFn) && foundFn.Any(f => f == fn))
                return false;

            _functions.Add(fn.Name, fn);
            return true;
        }

        public bool TryLookupFn(string name, out FunctionSymbol[] functions)
        {
            ImmutableArray<FunctionSymbol> fns = GetFunctions();
            functions = fns.Any(fn => fn.Name == name) ? fns.Where(f => f.Name == name).ToArray() : Array.Empty<FunctionSymbol>();
            return functions.Any() || Parent is not null && Parent.TryLookupFn(name, out functions);
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

    public sealed class BoundGlobalScope
    {
        public BoundGlobalScope(BoundGlobalScope? previous, FunctionSymbol? mainFn, FunctionSymbol? scriptFn, BoundBlockStmt stmt, ImmutableArray<VariableSymbol> variables, ImmutableArray<FunctionSymbol> functions, ImmutableArray<Diagnostic> diagnostics)
        {
            Previous = previous;
            MainFn = mainFn;
            ScriptFn = scriptFn;
            Stmt = stmt;
            Variables = variables;
            Functions = functions;
            Diagnostics = diagnostics;
        }

        public BoundGlobalScope? Previous { get; }
        public FunctionSymbol? MainFn { get; }
        public FunctionSymbol? ScriptFn { get; }
        public BoundBlockStmt Stmt { get; }
        public ImmutableArray<VariableSymbol> Variables { get; }
        public ImmutableArray<FunctionSymbol> Functions { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
    }
}
