using System.Collections.Immutable;
using System.Reflection;

namespace Wave.Binding
{
    public static class BuiltInFunctions
    {
        public static readonly FunctionSymbol Print = new("print", ImmutableArray.Create(new ParameterSymbol("text", TypeSymbol.String)), TypeSymbol.Void);
        public static readonly FunctionSymbol Input = new("input", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.String);
        internal static IEnumerable<FunctionSymbol> GetAll()
            => typeof(BuiltInFunctions).GetFields(BindingFlags.Public | BindingFlags.Static)
                                        .Where(f => f.FieldType == typeof(FunctionSymbol))
                                        .Select(f => (FunctionSymbol)f.GetValue(null)!);
    }
}
