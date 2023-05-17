namespace Wave.Symbols
{
    public enum SymbolKind
    {
        Variable,
        GlobalVariable,
        LocalVariable,
        Type,
        Parameter,
        Function,
        Method,
        Class,
        ADT,
        Label
    }

    public abstract class Symbol
    {
        public Symbol(string name) => Name = name;
        public abstract SymbolKind Kind { get; }
        public string Name { get; set; }
        public void WriteTo(TextWriter writer) => SymbolPrinter.WriteTo(this, writer);
        public override string ToString()
        {
            using StringWriter writer = new();
            WriteTo(writer);
            return writer.ToString();
        }
    }
}
