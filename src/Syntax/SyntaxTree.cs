using System.Collections.Immutable;
using Wave.Source.Syntax.Nodes;

namespace Wave.Source.Syntax
{
    public class SyntaxTree
    {
        private SyntaxTree(SourceText source, ParseHandler handler)
        {
            Source = source;
            handler(this, out CompilationUnit root, out ImmutableArray<Diagnostic> diagnostics);
            Diagnostics = diagnostics;
            Root = root;
        }

        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public SourceText Source { get; }
        public CompilationUnit Root { get; }

        private delegate void ParseHandler(SyntaxTree syntaxTree, out CompilationUnit root, out ImmutableArray<Diagnostic> diagnostics);
        public static SyntaxTree Load(string fileName)
        {
            string text = File.ReadAllText(fileName);
            return Parse(SourceText.From(text, fileName));
        }

        private static void Parse(SyntaxTree syntaxTree, out CompilationUnit root, out ImmutableArray<Diagnostic> diagnostics)
        {
            Parser parser = new(syntaxTree);
            root = parser.ParseCompilationUnit();
            diagnostics = parser.Diagnostics.ToImmutableArray();
        }

        public static SyntaxTree Parse(string text) => Parse(SourceText.From(text));
        public static SyntaxTree Parse(SourceText source) => new(source, Parse);
        internal static ImmutableArray<Token> ParseTokens(string line) => ParseTokens(line, out _);
        internal static ImmutableArray<Token> ParseTokens(SourceText source) => ParseTokens(source, out _);
        internal static ImmutableArray<Token> ParseTokens(string line, out ImmutableArray<Diagnostic> diagnostics) => ParseTokens(SourceText.From(line), out diagnostics);
        internal static ImmutableArray<Token> ParseTokens(SourceText source, out ImmutableArray<Diagnostic> diagnostics)
        {
            List<Token> tokens = new();
            void ParseTokens(SyntaxTree syntaxTree, out CompilationUnit root, out ImmutableArray<Diagnostic> diagnostics)
            {
                Lexer lexer = new(syntaxTree);
                while (true)
                {
                    Token token = lexer.GetToken();
                    if (token.Kind == SyntaxKind.Eof)
                    {
                        root = new(syntaxTree, ImmutableArray<MemberNode>.Empty, token);
                        break;
                    }

                    tokens.Add(token);
                }

                diagnostics = lexer.Diagnostics.ToImmutableArray();
            }

            SyntaxTree syntaxTree = new(source, ParseTokens);
            diagnostics = syntaxTree.Diagnostics;
            return tokens.ToImmutableArray();
        }
    }
}
