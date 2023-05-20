using System.Collections.Immutable;
using Wave.Source.Syntax.Nodes;

namespace Wave.Symbols
{
    public class FunctionSymbol : Symbol
    {
        public FunctionSymbol(string name, ImmutableArray<ParameterSymbol> parameters, TypeSymbol type, FnDeclStmt? decl = null, bool isStd = false)
            : base(name)
        {
            Parameters = parameters;
            Type = type;
            Decl = decl;
            IsStd = isStd;
        }

        public override SymbolKind Kind => SymbolKind.Function;
        public ImmutableArray<ParameterSymbol> Parameters { get; }
        public TypeSymbol Type { get; }
        public FnDeclStmt? Decl { get; }
        public bool IsStd { get; }

        public static bool operator ==(FunctionSymbol fn, FunctionSymbol other) => fn.Name == other.Name && fn.Type.IsArray == fn.Type.IsArray && fn.Parameters.Select(p => p.Type).SequenceEqual(other.Parameters.Select(p => p.Type));
        public static bool operator !=(FunctionSymbol fn, FunctionSymbol other) => !(fn == other);
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || (obj is not null && (FunctionSymbol)obj == this);
        public override int GetHashCode() => Name.GetHashCode() & Parameters.GetHashCode();
    }

    public sealed class MethodSymbol : FunctionSymbol
    {
        public MethodSymbol(string name, ImmutableArray<ParameterSymbol> parameters, TypeSymbol type, string className, Accessibility accessibility, FnDeclStmt? decl = null, bool isStatic = false)
            : base(name, parameters, type, decl)
        {
            ClassName = className;
            Accessibility = accessibility;
            IsStatic = isStatic;
        }

        public override SymbolKind Kind => SymbolKind.Method;
        public string ClassName { get; }
        public Accessibility Accessibility { get; }
        public bool IsStatic { get; }
        public static bool operator ==(MethodSymbol fn, MethodSymbol other) => fn.Name == other.Name && fn.Type.IsArray == fn.Type.IsArray && fn.Parameters.Select(p => p.Type).SequenceEqual(other.Parameters.Select(p => p.Type)) && fn.ClassName == other.ClassName;
        public static bool operator !=(MethodSymbol fn, MethodSymbol other) => !(fn == other);
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || (obj is not null && (FunctionSymbol)obj == this);
        public override int GetHashCode() => Name.GetHashCode() ^ Parameters.GetHashCode();
    }
}
