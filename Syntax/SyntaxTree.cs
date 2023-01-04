using System.Collections.Immutable;
using Wave.Nodes;

namespace Wave
{
    public class SyntaxTree
    {
        private SyntaxTree(SourceText source)
        {
            Parser parser = new(source);
            CompilationUnit root = parser.ParseCompilationUnit(); ;
            Diagnostics = parser.Diagnostics.ToImmutableArray();
            Source = source;
            Root = root;
        }

        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public SourceText Source { get; }
        public CompilationUnit Root { get; }

        public static SyntaxTree Parse(string text) => Parse(SourceText.From(text));
        public static SyntaxTree Parse(SourceText source) => new(source);

        internal static List<Token> ParseTokens(string line)
        {
            Lexer lexer = new(SourceText.From(line));
            List<Token> tokens = new();
            while (true)
            {
                Token token = lexer.GetToken();
                if (token.Kind == SyntaxKind.Eof)
                    break;

                tokens.Add(token);
            }

            return tokens;
        }
    }
}
