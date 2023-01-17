namespace Wave.Symbols
{
    public sealed class TypeSymbol : Symbol
    {
        public static readonly TypeSymbol Int = new("int");
        public static readonly TypeSymbol Float = new("float");
        public static readonly TypeSymbol Bool = new("bool");
        public static readonly TypeSymbol String = new("string");
        public static readonly TypeSymbol Unknown = new("unknown");
        public static readonly TypeSymbol Any = new("any");
        public static readonly TypeSymbol Void = new("void");
        public TypeSymbol(string name)
            : base(name) { }

        public override SymbolKind Kind => SymbolKind.Type;
    }
}
