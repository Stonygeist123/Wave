using Wave.Source.Binding.BoundNodes;

namespace Wave.Symbols
{
    public class NamespaceSymbol : Symbol
    {
        public NamespaceSymbol(string name, List<ClassSymbol> classes, Dictionary<FunctionSymbol, BoundBlockStmt> fns, List<ADTSymbol> adts, List<NamespaceSymbol> namespaces, NamespaceSymbol? parent)
            : base(name)
        {
            Classes = classes;
            Fns = fns;
            ADTs = adts;
            Namespaces = namespaces;
            Parent = parent;
        }

        public override SymbolKind Kind => SymbolKind.Class;
        public List<ClassSymbol> Classes { get; }
        public Dictionary<FunctionSymbol, BoundBlockStmt> Fns { get; }
        public List<ADTSymbol> ADTs { get; }
        public List<NamespaceSymbol> Namespaces { get; }
        public NamespaceSymbol? Parent { get; }
        public static bool operator ==(NamespaceSymbol? c, NamespaceSymbol? other) => c?.Name == other?.Name && c?.Parent == other?.Parent;
        public static bool operator !=(NamespaceSymbol? c, NamespaceSymbol? other) => c?.Name != other?.Name && c?.Parent != other?.Parent;
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || (obj is not null && (NamespaceSymbol)obj == this);
        public override int GetHashCode() => Name.GetHashCode();
    }
}
