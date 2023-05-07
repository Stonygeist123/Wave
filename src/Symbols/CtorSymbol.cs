using System.Collections.Immutable;
using Wave.Source.Syntax.Nodes;

namespace Wave.Symbols
{
    public sealed class CtorSymbol : Symbol
    {
        public CtorSymbol(ImmutableArray<ParameterSymbol> parameters, string className, Accessibility accessibility, CtorDeclStmt? decl = null)
            : base("")
        {
            Parameters = parameters;
            Decl = decl;
            ClassName = className;
            Accessibility = accessibility;
        }

        public override SymbolKind Kind => SymbolKind.Function;
        public ImmutableArray<ParameterSymbol> Parameters { get; }
        public CtorDeclStmt? Decl { get; }
        public string? ClassName { get; }
        public Accessibility Accessibility { get; }
        public static bool operator ==(CtorSymbol fn, CtorSymbol other) => fn.Name == other.Name && fn.Parameters.Select(p => p.Type).SequenceEqual(other.Parameters.Select(p => p.Type)) && fn.ClassName == other.ClassName;
        public static bool operator !=(CtorSymbol fn, CtorSymbol other) => !(fn == other);
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || (obj is not null && (CtorSymbol)obj == this);
        public override int GetHashCode() => Name.GetHashCode() ^ Parameters.GetHashCode();
    }
}
