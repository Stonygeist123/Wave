using System.Collections.Immutable;
using Wave.Source.Binding.BoundNodes;

namespace Wave.Symbols
{
    public class ADTSymbol : Symbol
    {
        public ADTSymbol(string name, ImmutableDictionary<string, BoundExpr> members)
            : base(name) => Members = members;
        public override SymbolKind Kind => SymbolKind.ADT;
        public ImmutableDictionary<string, BoundExpr> Members { get; }
        public static bool operator ==(ADTSymbol c, ADTSymbol other) => c.Members.SequenceEqual(other.Members);
        public static bool operator !=(ADTSymbol c, ADTSymbol other) => !c.Members.SequenceEqual(other.Members);
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || (obj is not null && (ADTSymbol)obj == this);
        public override int GetHashCode() => Name.GetHashCode();
    }
}
