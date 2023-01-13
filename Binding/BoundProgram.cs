using System.Collections.Immutable;
using Wave.Binding.BoundNodes;
using Wave.Symbols;

namespace Wave.Binding
{
    internal class BoundProgram
    {
        public BoundProgram(BoundGlobalScope globalScope, ImmutableArray<Diagnostic> diagnostics, ImmutableDictionary<FunctionSymbol, BoundBlockStmt> fnBodies)
        {
            GlobalScope = globalScope;
            Diagnostics = diagnostics;
            FnBodies = fnBodies;
        }

        public BoundGlobalScope GlobalScope { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public ImmutableDictionary<FunctionSymbol, BoundBlockStmt> FnBodies { get; }
    }
}