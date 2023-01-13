using System.Collections.Immutable;
using Wave.Nodes;

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
        Label
    }

    public abstract class Symbol
    {
        internal Symbol(string name) => Name = name;
        public abstract SymbolKind Kind { get; }
        public string Name { get; }
        public override string ToString() => Name;
    }

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

    public sealed class LabelSymbol : Symbol
    {
        public LabelSymbol(string name)
            : base(name) { }

        public override SymbolKind Kind => SymbolKind.Label;
    }
}
