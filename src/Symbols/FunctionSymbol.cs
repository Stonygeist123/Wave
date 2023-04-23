using System.Collections.Immutable;
using Wave.Source.Syntax.Nodes;

namespace Wave.Symbols
{
    public sealed class FunctionSymbol : Symbol
    {
        public FunctionSymbol(string name, ImmutableArray<ParameterSymbol> parameters, TypeSymbol type, FnDeclStmt? decl = null, string? className = null)
            : base(name)
        {
            Parameters = parameters;
            Type = type;
            Decl = decl;
            ClassName = className;
        }

        public override SymbolKind Kind => SymbolKind.Function;
        public ImmutableArray<ParameterSymbol> Parameters { get; }
        public TypeSymbol Type { get; }
        public FnDeclStmt? Decl { get; }
        public string? ClassName { get; }
        public static bool operator ==(FunctionSymbol fn, FunctionSymbol other) => fn.Name == other.Name && fn.Type.IsArray == fn.Type.IsArray && fn.Parameters.Select(p => p.Type).SequenceEqual(other.Parameters.Select(p => p.Type)) && fn.ClassName == other.ClassName;
        public static bool operator !=(FunctionSymbol fn, FunctionSymbol other) => !(fn == other);
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || (obj is not null && (FunctionSymbol)obj == this);
        public override int GetHashCode() => Name.GetHashCode() ^ Parameters.GetHashCode();
    }
}
