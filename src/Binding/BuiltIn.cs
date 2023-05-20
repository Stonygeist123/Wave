using System.Collections.Immutable;
using System.Reflection;
using Wave.Symbols;

namespace Wave.Source.Binding
{
    public static class StdLib
    {
        public static readonly NamespaceSymbol_Std Namespace = new("std", new(), new(), new(), GetAll().Cast<NamespaceSymbol>().ToList());
        private static readonly Random rnd = new();
        public static readonly NamespaceSymbol_Std IO = new("io", new(), new() {
            { new("print", ImmutableArray.Create(new ParameterSymbol("text", TypeSymbol.String)), TypeSymbol.Void, null, true), (object[] text) => { Console.WriteLine(text[0]); return null; } },
            { new("print", ImmutableArray.Create(new ParameterSymbol("text", TypeSymbol.Int)), TypeSymbol.Void, null, true), (object[] text) => { Console.WriteLine(text[0]); return null; } },
            { new("print", ImmutableArray.Create(new ParameterSymbol("text", TypeSymbol.Float)), TypeSymbol.Void, null, true), (object[] text) => { Console.WriteLine(text[0]); return null; } },
            { new("print", ImmutableArray.Create(new ParameterSymbol("text", TypeSymbol.Bool)), TypeSymbol.Void, null, true), (object[] text) => { Console.WriteLine(text[0]); return null; } },
            { new("print", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Void, null, true), (object[] text) => { Console.WriteLine(); return null; } },
            { new("input", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.String, null, true), (object[] _) => { return Console.ReadLine(); } },
            { new("clear", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Void, null, true), (object[] _) => { Console.Clear(); return null; } }
        }, new(), new());

        public static readonly NamespaceSymbol_Std Math = new("math", new(), new() {
            { new("sin", ImmutableArray.Create(new ParameterSymbol("n", TypeSymbol.Float)), TypeSymbol.Float, null, true), (object[] n) => { return System.Math.Sin((double)n[0]); } },
            { new("cos", ImmutableArray.Create(new ParameterSymbol("n", TypeSymbol.Float)), TypeSymbol.Float, null, true), (object[] n) => { return System.Math.Cos((double)n[0]); } },
            { new("tan", ImmutableArray.Create(new ParameterSymbol("n", TypeSymbol.Float)), TypeSymbol.Float, null, true), (object[] n) => { return System.Math.Tan((double)n[0]); } },
            { new("asin", ImmutableArray.Create(new ParameterSymbol("n", TypeSymbol.Float)), TypeSymbol.Float, null, true), (object[] n) => { return System.Math.Asin((double) n[0]); } },
            { new("acos", ImmutableArray.Create(new ParameterSymbol("n", TypeSymbol.Float)), TypeSymbol.Float, null, true), (object[] n) => { return System.Math.Acos((double) n[0]); } },
            { new("atan", ImmutableArray.Create(new ParameterSymbol("n", TypeSymbol.Float)), TypeSymbol.Float, null, true), (object[] n) => { return System.Math.Atan((double) n[0]); } },
            { new("random", ImmutableArray.Create(new ParameterSymbol("max", TypeSymbol.Int)), TypeSymbol.Int, null, true), (object[] max) => { return rnd.Next((int)max[0]); } },
            { new("range", ImmutableArray.Create(new ParameterSymbol("lowerBound", TypeSymbol.Int), new ParameterSymbol("upperBound", TypeSymbol.Int)), new(TypeSymbol.Int.Name, true), null, true), (object[] bounds) => { return Enumerable.Range((int)bounds[0], (int)bounds[1]).ToArray(); } },
            { new("range", ImmutableArray.Create(new ParameterSymbol("upperBound", TypeSymbol.Int)), new(TypeSymbol.Int.Name, true), null, true), (object[] bounds) => { return Enumerable.Range(0, (int)bounds[1]).ToArray(); } }
        }, new(), new());
        public static IEnumerable<NamespaceSymbol_Std> GetAll()
            => typeof(StdLib).GetFields(BindingFlags.Public | BindingFlags.Static)
                                        .Where(f => f.FieldType == typeof(NamespaceSymbol_Std))
                                        .Select(f => (NamespaceSymbol_Std)f.GetValue(null)!);
    }
}
