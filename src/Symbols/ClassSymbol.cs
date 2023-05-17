using System.Collections.Immutable;
using Wave.Source.Binding.BoundNodes;

namespace Wave.Symbols
{
    public class ClassSymbol : Symbol
    {
        public ClassSymbol(string name, KeyValuePair<CtorSymbol, BoundBlockStmt>? ctor, ImmutableDictionary<MethodSymbol, BoundBlockStmt> fns, Dictionary<FieldSymbol, BoundExpr> fields, NamespaceSymbol? namespaceSymbol)
            : base(name)
        {
            Ctor = ctor;
            Fns = fns;
            Fields = fields;
            NamespaceSymbol = namespaceSymbol;
        }

        public override SymbolKind Kind => SymbolKind.Class;
        public KeyValuePair<CtorSymbol, BoundBlockStmt>? Ctor { get; }
        public ImmutableDictionary<MethodSymbol, BoundBlockStmt> Fns { get; }
        public Dictionary<FieldSymbol, BoundExpr> Fields { get; }
        public NamespaceSymbol? NamespaceSymbol { get; }
        public static bool operator ==(ClassSymbol c, ClassSymbol other) => c.Name == other.Name && c.NamespaceSymbol == other.NamespaceSymbol;
        public static bool operator !=(ClassSymbol c, ClassSymbol other) => c.Name != other.Name && c.NamespaceSymbol != other.NamespaceSymbol;
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || (obj is not null && (ClassSymbol)obj == this);
        public override int GetHashCode() => Name.GetHashCode();
    }
}
