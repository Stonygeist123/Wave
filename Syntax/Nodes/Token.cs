using Wave.Nodes;

namespace Wave.Syntax.Nodes
{
    public class Token : Node
    {
        public Token(SyntaxKind kind, int position, string? lexeme, object? value = null)
        {
            Kind = kind;
            Position = position;
            Lexeme = lexeme ?? string.Empty;
            Value = value;
        }

        public override SyntaxKind Kind { get; }
        public int Position { get; }
        public string Lexeme { get; }
        public object? Value { get; }
        public override TextSpan Span => new(Position, Lexeme.Length);
        public bool IsMissing => string.IsNullOrEmpty(Lexeme);
    }
}
