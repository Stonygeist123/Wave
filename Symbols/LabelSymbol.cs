namespace Wave.Symbols
{
    public sealed class LabelSymbol : Symbol
    {
        public LabelSymbol(string name)
            : base(name) { }

        public override SymbolKind Kind => SymbolKind.Label;
    }
}
