using System.Collections.Immutable;
using Wave.Source.Binding.BoundNodes;
using Wave.Symbols;

namespace Wave.Source.Binding
{
    internal class BoundProgram
    {
        public BoundProgram(BoundBlockStmt stmt, ImmutableArray<Diagnostic> diagnostics, ImmutableDictionary<FunctionSymbol, BoundBlockStmt> fnBodies)
        {
            Stmt = stmt;
            Diagnostics = diagnostics;
            Functions = fnBodies;
        }

        public BoundBlockStmt Stmt { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public ImmutableDictionary<FunctionSymbol, BoundBlockStmt> Functions { get; }
    }
}