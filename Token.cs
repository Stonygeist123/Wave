using Wave.Nodes;

namespace Wave
{
    public enum SyntaxKind
    {
        // Token
        Number,
        Space,
        Plus,
        Minus,
        Star,
        Slash,
        Mod,
        LParen,
        RParen,
        Bad,
        Eof,

        // Expr
        NumberExpr,
        BinaryExpr,
        UnaryExpr,
        GroupingExpr
    }

    internal class Token : Node
    {
        public Token(SyntaxKind kind, int position, string lexeme, object? value = null)
        {
            Kind = kind;
            Position = position;
            Lexeme = lexeme;
            Value = value;
        }

        public override SyntaxKind Kind { get; }
        public int Position { get; }
        public string Lexeme { get; }
        public object? Value { get; }

        public override IEnumerable<Node> GetChildren() => Enumerable.Empty<Node>();
    }
}
