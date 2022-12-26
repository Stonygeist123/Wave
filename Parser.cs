using Wave.Nodes;

namespace Wave
{
    internal class Parser
    {
        private readonly Token[] _tokens;
        private int _position = 0;
        private readonly List<string> _diagnostics = new();
        public IEnumerable<string> Diagnostics => _diagnostics;

        public Parser(string text)
        {
            Lexer lexer = new(text);
            List<Token> tokens = new();
            while (true)
            {
                Token t = lexer.GetToken();
                if (t.Kind != SyntaxKind.Bad && t.Kind != SyntaxKind.Space)
                    tokens.Add(t);

                if (t.Kind == SyntaxKind.Eof)
                    break;
            }

            _tokens = tokens.ToArray();
            _diagnostics.AddRange(lexer.Diagnostics);
        }

        public SyntaxTree Parse()
        {
            ExprNode expr = ParseExpr();
            Token eofToken = Match(SyntaxKind.Eof);
            return new(_diagnostics, expr, eofToken);
        }

        public ExprNode ParseExpr() => ParseTerm();
        public ExprNode ParseTerm()
        {
            ExprNode left = ParseFactor();
            while (Current.Kind == SyntaxKind.Plus || Current.Kind == SyntaxKind.Minus)
            {
                Token op = Advance();
                ExprNode right = ParseFactor();
                left = new BinaryExpr(left, op, right);
            }

            return left;
        }

        public ExprNode ParseFactor()
        {
            ExprNode left = ParsePrimaryExpr();
            while (Current.Kind == SyntaxKind.Star || Current.Kind == SyntaxKind.Slash || Current.Kind == SyntaxKind.Mod)
            {
                Token op = Advance();
                ExprNode right = ParsePrimaryExpr();
                left = new BinaryExpr(left, op, right);
            }

            return left;
        }

        private ExprNode ParsePrimaryExpr()
        {
            if (Current.Kind == SyntaxKind.LParen)
            {
                Token lParen = Advance();
                ExprNode expr = ParseExpr();
                Token rParen = Match(SyntaxKind.RParen);
                return new GroupingExpr(lParen, expr, rParen);
            }

            Token number = Match(SyntaxKind.Number);
            return new NumberExpr(number);
        }

        private Token Match(SyntaxKind kind)
        {
            if (Current.Kind == kind)
                return Advance();

            _diagnostics.Add($"Unexpected token: \"{Current.Kind}\" - expected \"{kind}\".");
            return Current;
        }

        private Token Advance()
        {
            _position++;
            return Peek(-1);
        }

        private Token Peek(int offset) => _position + offset >= _tokens.Length ? _tokens.Last() : _tokens[_position + offset];
        private Token Current => Peek(0);
    }
}
