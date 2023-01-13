using Wave.Symbols;

namespace Wave.Binding
{
    internal sealed class Conversion
    {
        public static Conversion Identity = new(true, true, true);
        public static Conversion Implicit = new(true, false, true);
        public static Conversion Explicit = new(true, false, false);
        public static Conversion None = new(false, false, false);
        private Conversion(bool exists, bool isIdentity, bool isImplicit)
        {
            Exists = exists;
            IsIdentity = isIdentity;
            IsImplicit = isImplicit;
        }

        public bool Exists { get; }
        public bool IsIdentity { get; }
        public bool IsImplicit { get; }
        public bool IsExplicit => Exists && !IsImplicit;

        public static Conversion Classify(TypeSymbol from, TypeSymbol to)
        {
            if (from == to)
                return Conversion.Identity;

            if (from == TypeSymbol.Bool)
            {
                if (to == TypeSymbol.String)
                    return Conversion.Explicit;
            }
            else if (from == TypeSymbol.Int)
            {
                if (to == TypeSymbol.String)
                    return Conversion.Explicit;
                else if (to == TypeSymbol.Float)
                    return Conversion.Implicit;
            }
            else if (from == TypeSymbol.Float)
            {
                if (to == TypeSymbol.String)
                    return Conversion.Explicit;
                else if (to == TypeSymbol.Int)
                    return Conversion.Explicit;
            }
            else if (from == TypeSymbol.String)
            {
                if (to == TypeSymbol.Int)
                    return Conversion.Explicit;
                else if (to == TypeSymbol.Float)
                    return Conversion.Explicit;
            }

            return Conversion.None;
        }
    }
}
