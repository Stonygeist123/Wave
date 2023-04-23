namespace Wave.Symbols
{
    public abstract class VariableSymbol : Symbol
    {
        public VariableSymbol(string name, TypeSymbol type, bool isMut)
            : base(name)
        {
            Type = type;
            IsMut = isMut;
        }

        public override SymbolKind Kind => SymbolKind.Variable;
        public TypeSymbol Type { get; }
        public bool IsMut { get; }
    }

    public sealed class GlobalVariableSymbol : VariableSymbol
    {
        public GlobalVariableSymbol(string name, TypeSymbol type, bool isMut)
            : base(name, type, isMut) { }
        public override SymbolKind Kind => SymbolKind.GlobalVariable;
    }

    public class LocalVariableSymbol : VariableSymbol
    {
        public LocalVariableSymbol(string name, TypeSymbol type, bool isMut)
            : base(name, type, isMut) { }
        public override SymbolKind Kind => SymbolKind.LocalVariable;
    }

    public sealed class ParameterSymbol : LocalVariableSymbol
    {
        public ParameterSymbol(string name, TypeSymbol type)
            : base(name, type, false) { }
        public override SymbolKind Kind => SymbolKind.Parameter;
    }

    public enum Accessibility
    {
        Pub,
        Priv
    }

    public sealed class FieldSymbol : LocalVariableSymbol
    {
        public FieldSymbol(string name, TypeSymbol type, Accessibility accessibility, bool isMut)
            : base(name, type, isMut) => Accessibility = accessibility;
        public override SymbolKind Kind => SymbolKind.Parameter;
        public Accessibility Accessibility { get; }
    }
}
