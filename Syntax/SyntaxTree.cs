using Wave.Nodes;

namespace Wave
{
    public class SyntaxTree
    {
        public SyntaxTree(SourceText source, IEnumerable<Diagnostic> diagnostics, ExprNode root, Token eofToken)
        {
            Diagnostics = diagnostics.ToArray();
            Source = source;
            Root = root;
            EofToken = eofToken;
        }

        public IReadOnlyList<Diagnostic> Diagnostics { get; }
        public SourceText Source { get; }
        public ExprNode Root { get; }
        public Token EofToken { get; }

        public static SyntaxTree Parse(string text)
        {
            SourceText source = SourceText.From(text);
            Parser parser = new(source);
            return parser.Parse();
        }

        public static SyntaxTree Parse(SourceText source)
        {
            Parser parser = new(source);
            return parser.Parse();
        }
    }
}
