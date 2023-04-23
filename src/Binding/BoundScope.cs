using System.Collections.Immutable;
using Wave.Source.Binding.BoundNodes;
using Wave.Symbols;

namespace Wave.Source.Binding
{
    public sealed class BoundScope
    {
        private readonly Dictionary<string, VariableSymbol> _variables = new();
        private readonly Dictionary<string, FunctionSymbol> _functions = new();
        private readonly Dictionary<string, ClassSymbol> _classes = new();
        public BoundScope(BoundScope? parent) => Parent = parent;
        public bool TryLookupVar(string name, out VariableSymbol? variable) => _variables.TryGetValue(name, out variable) || Parent is not null && Parent.TryLookupVar(name, out variable);
        public bool TryDeclareVar(VariableSymbol variable, bool force = false)
        {
            if (TryLookupVar(variable.Name, out _) && !force)
                return false;

            _variables.Add(variable.Name, variable);
            return true;
        }

        public bool TryDeclareFn(FunctionSymbol fn)
        {
            if (TryLookupFn(fn.Name, out FunctionSymbol[] foundFns) && foundFns.Any(f => f == fn))
                return false;

            _functions.Add(fn.GetHashCode().ToString(), fn);
            return true;
        }

        public bool TryLookupFn(string name, out FunctionSymbol[] functions)
        {
            ImmutableArray<FunctionSymbol> fns = GetFunctions();
            functions = fns.Any(fn => fn.Name == name) ? fns.Where(f => f.Name == name).ToArray() : Array.Empty<FunctionSymbol>();
            return functions.Any() || Parent is not null && Parent.TryLookupFn(name, out functions);
        }

        public bool TryLookupClass(string name, out ClassSymbol? c) => _classes.TryGetValue(name, out c) || Parent is not null && Parent.TryLookupClass(name, out c);
        public bool TryDeclareClass(ClassSymbol c)
        {
            if (TryLookupClass(c.Name, out ClassSymbol? foundClass) && foundClass is not null)
                return false;

            _classes.Add(c.Name, c);
            return true;
        }

        public ImmutableArray<VariableSymbol> GetDeclaredVars() => _variables.Values.ToImmutableArray();
        public ImmutableArray<FunctionSymbol> GetDeclaredFns() => _functions.Values.ToImmutableArray();
        public ImmutableArray<ClassSymbol> GetDeclaredClasses() => _classes.Values.ToImmutableArray();
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

            return vars.ToImmutable();
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

            return fns.ToImmutable();
        }

        public ImmutableArray<ClassSymbol> GetClasses()
        {
            ImmutableArray<ClassSymbol>.Builder classes = ImmutableArray.CreateBuilder<ClassSymbol>();
            BoundScope? scope = this;
            while (scope is not null)
            {
                foreach (ClassSymbol c in scope.GetDeclaredClasses())
                    classes.Add(c);
                scope = scope.Parent;
            }

            return classes.ToImmutable();
        }

        public BoundScope? Parent { get; }
    }

    public sealed class BoundGlobalScope
    {
        public BoundGlobalScope(BoundGlobalScope? previous, FunctionSymbol? mainFn, FunctionSymbol? scriptFn, BoundBlockStmt stmt, ImmutableArray<VariableSymbol> variables, ImmutableArray<FunctionSymbol> functions, ImmutableArray<ClassSymbol> classes, ImmutableArray<Diagnostic> diagnostics)
        {
            Previous = previous;
            MainFn = mainFn;
            ScriptFn = scriptFn;
            Stmt = stmt;
            Variables = variables;
            Functions = functions;
            Classes = classes;
            Diagnostics = diagnostics;
        }

        public BoundGlobalScope? Previous { get; }
        public FunctionSymbol? MainFn { get; }
        public FunctionSymbol? ScriptFn { get; }
        public BoundBlockStmt Stmt { get; }
        public ImmutableArray<VariableSymbol> Variables { get; }
        public ImmutableArray<FunctionSymbol> Functions { get; }
        public ImmutableArray<ClassSymbol> Classes { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
    }
}
