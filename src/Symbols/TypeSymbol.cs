namespace Wave.Symbols
{
    public sealed class TypeSymbol : Symbol
    {
        public static readonly TypeSymbol Int = new("int");
        public static readonly TypeSymbol Float = new("float");
        public static readonly TypeSymbol Bool = new("bool");
        public static readonly TypeSymbol String = new("string");
        public static readonly TypeSymbol Unknown = new("unknown");
        public static readonly TypeSymbol Void = new("void");
        public TypeSymbol(string name, bool isArray = false)
            : base(name) => IsArray = isArray;

        public override SymbolKind Kind => SymbolKind.Type;
        public bool IsArray { get; }
        public static bool operator ==(TypeSymbol a, TypeSymbol b) => a.Name == b.Name;
        public static bool operator !=(TypeSymbol a, TypeSymbol b) => a.Name != b.Name;
        public override int GetHashCode() => base.GetHashCode();
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            else if (obj is null)
                return false;
            else
                return false;
        }

        public override string ToString() => $"{Name}{(IsArray ? "[]" : "")}";
    }
}
