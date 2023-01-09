using System.Collections.Immutable;
using Wave.Nodes;
using Wave.Syntax.Nodes;

namespace Wave
{
    public class SyntaxTree
    {
        private SyntaxTree(SourceText source)
        {
            Parser parser = new(source);
            CompilationUnit root = parser.ParseCompilationUnit();
            Diagnostics = parser.Diagnostics.ToImmutableArray();
            Source = source;
            Root = root;
        }

        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public SourceText Source { get; }
        public CompilationUnit Root { get; }

        public static SyntaxTree Parse(string text) => Parse(SourceText.From(text));
        public static SyntaxTree Parse(SourceText source) => new(source);
        internal static ImmutableArray<Token> ParseTokens(string line) => ParseTokens(line, out _);
        internal static ImmutableArray<Token> ParseTokens(SourceText source) => ParseTokens(source, out _);
        internal static ImmutableArray<Token> ParseTokens(string line, out ImmutableArray<Diagnostic> diagnostics) => ParseTokens(SourceText.From(line), out diagnostics);
        internal static ImmutableArray<Token> ParseTokens(SourceText source, out ImmutableArray<Diagnostic> diagnostics)
        {

            Lexer lexer = new(source);
            List<Token> result = new();
            while (true)
            {
                Token token = lexer.GetToken();
                result.Add(token);
                if (token.Kind == SyntaxKind.Eof)
                    break;
            }

            diagnostics = lexer.Diagnostics.ToImmutableArray();
            return result.ToImmutableArray();
        }
    }
}
