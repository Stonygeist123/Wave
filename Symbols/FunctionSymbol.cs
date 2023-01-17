using System.Collections.Immutable;
using Wave.Nodes;

namespace Wave.Symbols
{
    public sealed class FunctionSymbol : Symbol
    {
        public FunctionSymbol(string name, ImmutableArray<ParameterSymbol> parameters, TypeSymbol type, FnDeclStmt? decl = null)
            : base(name)
        {
            Parameters = parameters;
            Type = type;
            Decl = decl;
        }

        public override SymbolKind Kind => SymbolKind.Function;
        public ImmutableArray<ParameterSymbol> Parameters { get; }
        public TypeSymbol Type { get; }
        public FnDeclStmt? Decl { get; }
    }
}
