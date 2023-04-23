using System.Collections.Immutable;
using Wave.Source.Binding.BoundNodes;

namespace Wave.Symbols
{
    public class ClassSymbol : Symbol
    {
        public ClassSymbol(string name, ImmutableDictionary<FunctionSymbol, BoundBlockStmt> fns, ImmutableDictionary<FieldSymbol, BoundExpr> fields)
            : base(name)
        {
            Fns = fns;
            Fields = fields;
        }

        public override SymbolKind Kind => SymbolKind.Variable;
        public ImmutableDictionary<FunctionSymbol, BoundBlockStmt> Fns { get; }
        public ImmutableDictionary<FieldSymbol, BoundExpr> Fields { get; }
        public static bool operator ==(ClassSymbol c, FunctionSymbol other) => c.Name == other.Name;
        public static bool operator !=(ClassSymbol c, FunctionSymbol other) => c.Name != other.Name;
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || (obj is not null && (ClassSymbol)obj == this);
        public override int GetHashCode() => Name.GetHashCode();
    }
}
