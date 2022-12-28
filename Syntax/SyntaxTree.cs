using Wave.Nodes;

namespace Wave
{
    internal class SyntaxTree
    {
        public SyntaxTree(IEnumerable<string> diagnostics, ExprNode root, Token eofToken)
        {
            Diagnostics = diagnostics.ToArray();
            Root = root;
            EofToken = eofToken;
        }

        public IReadOnlyList<string> Diagnostics { get; }
        public ExprNode Root { get; }
        public Token EofToken { get; }

        public static SyntaxTree Parse(string text)
        {
            Parser parser = new(text);
            return parser.Parse();
        }
    }
}
