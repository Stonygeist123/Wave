namespace Wave
{
    internal class Lexer
    {
        private readonly string _text;
        private int _position = 0;
        private readonly DiagnosticBag _diagnostics = new();
        private char Current => Peek();
        private char Next => Peek(1);

        public DiagnosticBag Diagnostics => _diagnostics;

        public Lexer(string text) => _text = text;
        public Token GetToken()
        {
            int start = _position;
            switch (Current)
            {
                case '\0':
                    return new(SyntaxKind.Eof, _position - 1, "\0");
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
                case '!':
                    if (Next == '=')
                    {
                        _position += 2;
                        return new(SyntaxKind.NotEq, start, "!=");
                    }
                    return new(SyntaxKind.Bang, _position++, "!");
                case '&':
                    if (Next == '&')
                    {
                        _position += 2;
                        return new(SyntaxKind.LogicAnd, start, "&&");
                    }
                    break;
                case '|':
                    if (Next == '|')
                    {
                        _position += 2;
                        return new(SyntaxKind.LogicOr, start, "||");
                    }
                    break;
                case '=':
                    if (Next == '=')
                    {
                        _position += 2;
                        return new(SyntaxKind.EqEq, start, "==");
                    }
                    return new Token(SyntaxKind.Eq, _position++, "=");
                default:
                    if (char.IsDigit(Current))
                    {
                        bool isPower = false, isFloat = false;
                        double fVal = 0, fractionSize = 1;
                        int iVal = 0;
                        while (true)
                        {
                            int n;
                            char cur = Current;
                            if (!char.IsDigit(cur) && cur != '.' && cur != 'e' && cur != '_' && cur != '-')
                                break;

                            Advance();
                            if (char.IsDigit(cur))
                                n = int.Parse(cur.ToString());
                            else if (cur == '_')
                                continue;
                            else if (cur == '.')
                            {
                                if (isFloat)
                                    _diagnostics.Report(new(start, _position - start), "Invalid floating-point number with two dots.");

                                isFloat = true;
                                fVal = iVal;
                                continue;
                            }
                            else if (cur == 'e')
                            {
                                isPower = true;
                                break;
                            }
                            else
                                break;

                            if (isFloat)
                                fVal += n * (fractionSize /= 10);
                            else
                            {
                                iVal *= 10;
                                iVal += n;
                            }
                        }

                        bool negPower = false;
                        if (isPower)
                        {
                            double power = 0;
                            while (true)
                            {
                                char cur = Current;
                                if (cur == '\0')
                                    break;

                                if (!char.IsDigit(cur) && cur != '-' && cur != '_')
                                    break;

                                Advance();
                                if (cur == '_')
                                    continue;

                                if (cur == '-')
                                {
                                    if (negPower)
                                        _diagnostics.Report(new(start, _position - start), "Invalid number literal with multiple negatives.");

                                    negPower = true;
                                    if (!isFloat)
                                        fVal = iVal;

                                    isFloat = true;
                                    continue;
                                }

                                power *= 10;
                                power += int.Parse(cur.ToString());
                            }


                            if (negPower)
                                power = -power;

                            if (isFloat)
                                fVal *= Math.Pow(10, power);
                            else
                            {
                                isFloat = true;
                                fVal = iVal;
                                fVal *= (int)Math.Pow(10, power);
                            }
                        }

                        return new(isFloat ? SyntaxKind.Float : SyntaxKind.Int, start, _text[start.._position], isFloat ? (int)fVal : iVal);
                    }
                    else if (char.IsWhiteSpace(Current))
                    {
                        while (char.IsWhiteSpace(Current))
                            Advance();

                        string lexeme = _text[start.._position];
                        return new(SyntaxKind.Space, start, lexeme);
                    }
                    else if (char.IsLetter(Current))
                    {
                        while (char.IsLetter(Current))
                            Advance();

                        string text = _text[start.._position];
                        SyntaxKind kind = SyntaxFacts.GetKeyWordKind(text);
                        return new(kind, start, text);
                    }
                    break;
            }

            _diagnostics.Report(new(_position, 1), $"Unexpected character: '{Current}'.");
            return new(SyntaxKind.Bad, _position++, Peek(-1).ToString());
        }

        private void Advance() => ++_position;
        private char Peek(int offset = 0)
        {
            if (_position + offset >= _text.Length)
                return '\0';
            return _text[_position + offset];
        }
    }
}
