using System.Text;
using Wave.Source.Syntax.Nodes;

namespace Wave.Source.Syntax
{
    public class Lexer
    {
        private readonly SourceText _source;
        private int _position = 0;
        private readonly DiagnosticBag _diagnostics = new();
        private readonly SyntaxTree _syntaxTree;

        private char Current => _position >= _source.Length ? '\0' : _source[_position];
        private SyntaxKind _kind;
        private int _start;
        private object? _value;

        public DiagnosticBag Diagnostics => _diagnostics;
        public Lexer(SyntaxTree syntaxTree)
        {
            _source = syntaxTree.Source;
            _syntaxTree = syntaxTree;
        }

        public Token GetToken()
        {
            _start = _position;
            _kind = SyntaxKind.Bad;
            _value = null;

            switch (Current)
            {
                case '\0':
                    _kind = SyntaxKind.Eof;
                    break;
                case '+':
                    ++_position;
                    _kind = SyntaxKind.Plus;
                    break;
                case '-':
                    ++_position;
                    if (Current == '>')
                    {
                        ++_position;
                        _kind = SyntaxKind.Arrow;
                    }
                    else
                        _kind = SyntaxKind.Minus;
                    break;
                case '*':
                    ++_position;
                    if (Current == '*')
                    {
                        ++_position;
                        _kind = SyntaxKind.Power;
                    }
                    else
                        _kind = SyntaxKind.Star;
                    break;
                case '/':
                    ++_position;
                    if (Current == '/')
                    {
                        while (Current != '\n' && Current != '\0')
                            ++_position;

                        _kind = SyntaxKind.Comment;
                    }
                    else
                        _kind = SyntaxKind.Slash;
                    break;
                case '%':
                    ++_position;
                    _kind = SyntaxKind.Mod;
                    break;
                case '(':
                    ++_position;
                    _kind = SyntaxKind.LParen;
                    break;
                case ')':
                    ++_position;
                    _kind = SyntaxKind.RParen;
                    break;
                case '{':
                    ++_position;
                    _kind = SyntaxKind.LBrace;
                    break;
                case '}':
                    ++_position;
                    _kind = SyntaxKind.RBrace;
                    break;
                case '&':
                    ++_position;
                    if (Current == '&')
                    {
                        ++_position;
                        _kind = SyntaxKind.LogicAnd;
                    }
                    else
                        _kind = SyntaxKind.And;
                    break;
                case '^':
                    ++_position;
                    _kind = SyntaxKind.Xor;
                    break;
                case '~':
                    ++_position;
                    _kind = SyntaxKind.Inv;
                    break;
                case '|':
                    ++_position;
                    if (Current == '|')
                    {
                        ++_position;
                        _kind = SyntaxKind.LogicOr;
                    }
                    else
                        _kind = SyntaxKind.Or;
                    break;
                case '!':
                    ++_position;
                    if (Current == '=')
                    {
                        ++_position;
                        _kind = SyntaxKind.NotEq;
                    }
                    else
                        _kind = SyntaxKind.Bang;
                    break;
                case '=':
                    ++_position;
                    if (Current == '=')
                    {
                        ++_position;
                        _kind = SyntaxKind.EqEq;
                    }
                    else
                        _kind = SyntaxKind.Eq;
                    break;
                case '>':
                    ++_position;
                    if (Current == '=')
                    {
                        ++_position;
                        _kind = SyntaxKind.GreaterEq;
                    }
                    else
                        _kind = SyntaxKind.Greater;
                    break;
                case '<':
                    ++_position;
                    if (Current == '=')
                    {
                        ++_position;
                        _kind = SyntaxKind.LessEq;
                    }
                    else
                        _kind = SyntaxKind.Less;
                    break;
                case '.':
                    if (_position + 1 >= _source.Length || !char.IsDigit(_source[_position + 1]))
                        goto default;
                    else
                        LexNumber();
                    break;
                case ',':
                    ++_position;
                    _kind = SyntaxKind.Comma;
                    break;
                case ';':
                    ++_position;
                    _kind = SyntaxKind.Semicolon;
                    break;
                case ':':
                    ++_position;
                    _kind = SyntaxKind.Colon;
                    break;
                case '"':
                    LexString();
                    break;
                case '\n':
                case '\r':
                    if (Current == '\r' && _source[_position + 1] == '\n')
                        _position += 2;
                    else
                        _position++;

                    _kind = SyntaxKind.Space;
                    break;
                case ' ':
                case '\t':
                    LexWhitespace();
                    break;
                default:
                    if (char.IsDigit(Current))
                        LexNumber();
                    else if (char.IsLetter(Current) || Current == '_')
                    {
                        while (char.IsLetterOrDigit(Current) || Current == '_')
                            ++_position;

                        _kind = SyntaxFacts.GetKeyWordKind(_source[_start.._position]);
                    }
                    else
                    {
                        _diagnostics.Report(new(_source, new(_position, 1)), $"Unexpected character: '{Current}'.");
                        ++_position;
                    }
                    break;
            }

            string text = _kind.GetLexeme() ?? _source[_start.._position];
            return new(_syntaxTree, _kind, _start, text, _value);
        }

        private void LexWhitespace()
        {
            _kind = SyntaxKind.Space;
            bool done = false;
            while (!done)
            {
                switch (Current)
                {
                    case '\0':
                    case '\r':
                    case '\n':
                        done = true;
                        break;
                    default:
                        if (char.IsWhiteSpace(Current))
                            ++_position;
                        else
                            done = true;
                        break;
                }
            }
        }

        private void LexString()
        {
            ++_position;
            StringBuilder sb = new();
            bool done = false;
            while (!done)
            {
                switch (Current)
                {
                    case '\0':
                    case '\r':
                    case '\n':
                        _diagnostics.Report(new(_source, new(_start, _position - _start)), $"Unterminated string literal.");
                        done = true;
                        break;
                    case '"':
                        ++_position;
                        if (Current == '"')
                        {
                            sb.Append('"');
                            ++_position;
                        }
                        else
                            done = true;
                        break;
                    default:
                        sb.Append(Current);
                        ++_position;
                        break;
                }
            }

            _kind = SyntaxKind.String;
            _value = sb.ToString();
        }

        private void LexNumber()
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

                if (char.IsDigit(cur))
                {
                    ++_position;
                    n = int.Parse(cur.ToString());
                }
                else if (cur == '_')
                {
                    ++_position;
                    continue;
                }
                else if (cur == '.')
                {
                    ++_position;
                    if (isFloat)
                        _diagnostics.Report(new(_source, new(_start, _position - _start)), "Invalid floating-point number with two dots.");

                    isFloat = true;
                    fVal = iVal;
                    continue;
                }
                else if (cur == 'e')
                {
                    ++_position;
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

                    ++_position;
                    if (cur == '_')
                        continue;

                    if (cur == '-')
                    {
                        if (negPower)
                            _diagnostics.Report(new(_source, new(_start, _position - _start)), "Invalid number literal with multiple negatives.");

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

            _kind = isFloat ? SyntaxKind.Float : SyntaxKind.Int;
            if (isFloat)
                _value = fVal;
            else
                _value = iVal;
        }
    }
}
