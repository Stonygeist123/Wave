namespace Wave
{
    internal class Lexer
    {
        private readonly string _text;
        private int _position = 0;
        private readonly List<string> _diagnostics = new();
        private char Current
        {
            get
            {
                if (_position >= _text.Length)
                    return '\0';
                return _text[_position];
            }
        }

        public IEnumerable<string> Diagnostics => _diagnostics;

        public Lexer(string text) => _text = text;
        public Token GetToken()
        {
            if (_position >= _text.Length)
                return new(SyntaxKind.Eof, _position, "\0");

            switch (Current)
            {
                case '+':
                    return new(SyntaxKind.Plus, _position++, "+");
                case '-':
                    return new(SyntaxKind.Minus, _position++, "-");
                case '*':
                    return new(SyntaxKind.Star, _position++, "*");
                case '/':
                    return new(SyntaxKind.Slash, _position++, "/");
                case '%':
                    return new(SyntaxKind.Mod, _position++, "%");
                case '(':
                    return new(SyntaxKind.LParen, _position++, "(");
                case ')':
                    return new(SyntaxKind.RParen, _position++, ")");
                default:

                    if (char.IsDigit(Current))
                    {
                        int start = _position;
                        while (char.IsDigit(Current))
                            Advance();

                        string lexeme = _text[start.._position];
                        if (!int.TryParse(lexeme, out int res))
                            _diagnostics.Add($"Invalid number \"{lexeme}\".");

                        return new(SyntaxKind.Number, start, lexeme, res);
                    }
                    else if (char.IsWhiteSpace(Current))
                    {
                        int start = _position;
                        while (char.IsWhiteSpace(Current))
                            Advance();

                        string lexeme = _text[start.._position];
                        return new(SyntaxKind.Space, start, lexeme);
                    }

                    _diagnostics.Add($"Unexpected character: '{Current}'.");
                    return new(SyntaxKind.Bad, _position++, Current.ToString());
            }
        }

        private void Advance() => ++_position;
    }
}
