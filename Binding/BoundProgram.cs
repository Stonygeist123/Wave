using System.Collections.Immutable;
using Wave.Binding.BoundNodes;
using Wave.Symbols;

namespace Wave.Binding
{
    internal class BoundProgram
    {
        public BoundProgram(BoundBlockStmt stmt, ImmutableArray<Diagnostic> diagnostics, ImmutableDictionary<FunctionSymbol, BoundBlockStmt> fnBodies)
        {
            Stmt = stmt;
            Diagnostics = diagnostics;
            FnBodies = fnBodies;
        }

        public BoundBlockStmt Stmt { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public ImmutableDictionary<FunctionSymbol, BoundBlockStmt> FnBodies { get; }
    }
}