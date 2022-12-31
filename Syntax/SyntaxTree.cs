using System.Collections.Immutable;
using Wave.Nodes;

namespace Wave
{
    public class SyntaxTree
    {
        private SyntaxTree(SourceText source)
        {
            Parser parser = new(source);
            CompilationUnit root = parser.ParseCompilationUnit();
            ImmutableArray<Diagnostic> diagnostics = parser.Diagnostics.ToImmutableArray();

            Diagnostics = diagnostics;
            Source = source;
            Root = root;
        }

        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public SourceText Source { get; }
        public CompilationUnit Root { get; }

        public static SyntaxTree Parse(string text) => Parse(SourceText.From(text));
        public static SyntaxTree Parse(SourceText source) => new(source);
    }
}
