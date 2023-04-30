using System.Collections.Immutable;
using Wave.Source.Binding.BoundNodes;

namespace Wave.Symbols
{
    public class ClassSymbol : Symbol
    {
        public ClassSymbol(string name, KeyValuePair<CtorSymbol, BoundBlockStmt>? ctor, ImmutableDictionary<FunctionSymbol, BoundBlockStmt> fns, ImmutableDictionary<FieldSymbol, BoundExpr> fields)
            : base(name)
        {
            Ctor = ctor;
            Fns = fns;
            Fields = fields;
        }

        public override SymbolKind Kind => SymbolKind.Variable;
        public KeyValuePair<CtorSymbol, BoundBlockStmt>? Ctor { get; }
        public ImmutableDictionary<FunctionSymbol, BoundBlockStmt> Fns { get; }
        public ImmutableDictionary<FieldSymbol, BoundExpr> Fields { get; }
        public static bool operator ==(ClassSymbol c, FunctionSymbol other) => c.Name == other.Name;
        public static bool operator !=(ClassSymbol c, FunctionSymbol other) => c.Name != other.Name;
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || (obj is not null && (ClassSymbol)obj == this);
        public override int GetHashCode() => Name.GetHashCode();
    }
}
