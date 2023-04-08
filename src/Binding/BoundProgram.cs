using System.Collections.Immutable;
using Wave.Source.Binding.BoundNodes;
using Wave.Symbols;

namespace Wave.Source.Binding
{
    public class BoundProgram
    {
        public BoundProgram(BoundProgram? previous, FunctionSymbol? mainFn, FunctionSymbol? scriptFn, ImmutableArray<Diagnostic> diagnostics, ImmutableDictionary<FunctionSymbol, BoundBlockStmt> functions)
        {
            Previous = previous;
            MainFn = mainFn;
            ScriptFn = scriptFn;
            Diagnostics = diagnostics;
            Functions = functions;
        }

        public BoundProgram? Previous { get; }
        public FunctionSymbol? MainFn { get; }
        public FunctionSymbol? ScriptFn { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public ImmutableDictionary<FunctionSymbol, BoundBlockStmt> Functions { get; }
    }
}