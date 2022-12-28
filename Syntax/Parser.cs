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

        public ExprNode ParseExpr(ushort parentPrecedence = 0)
        {
            ExprNode left;
            ushort unOpPrec = Current.Kind.GetUnOpPrecedence();
            if (unOpPrec != 0 && unOpPrec >= parentPrecedence)
            {
                Token op = Advance();
                left = new UnaryExpr(op, ParseExpr(unOpPrec));
            }
            else
                left = ParsePrimaryExpr();

            while (true)
            {
                ushort precendence = Current.Kind.GetBinOpPrecedence();
                if (precendence <= parentPrecedence)
                    break;

                Token op = Advance();
                ExprNode right = ParseExpr(precendence);
                left = new BinaryExpr(left, op, right);
            }

            return left;
        }


        private ExprNode ParsePrimaryExpr()
        {
            switch (Current.Kind)
            {
                case SyntaxKind.LParen:
                    {
                        Token lParen = Advance();
                        ExprNode expr = ParseExpr();
                        Token rParen = Match(SyntaxKind.RParen);
                        return new GroupingExpr(lParen, expr, rParen);
                    }
                case SyntaxKind.True:
                case SyntaxKind.False:
                    return new LiteralExpr(Current, Advance().Kind == SyntaxKind.True);
            }

            Token number = Match(SyntaxKind.Int);
            return new LiteralExpr(number);
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
