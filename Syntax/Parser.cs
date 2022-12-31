﻿using System.Collections.Immutable;
using Wave.Nodes;

namespace Wave
{
    internal class Parser
    {
        private readonly ImmutableArray<Token> _tokens;
        private int _position = 0;
        private readonly DiagnosticBag _diagnostics = new();
        private readonly SourceText _source;

        public DiagnosticBag Diagnostics => _diagnostics;

        public Parser(SourceText source)
        {
            Lexer lexer = new(source);
            List<Token> tokens = new();
            while (true)
            {
                Token t = lexer.GetToken();
                if (t.Kind != SyntaxKind.Bad && t.Kind != SyntaxKind.Space)
                    tokens.Add(t);

                if (t.Kind == SyntaxKind.Eof)
                    break;
            }

            _tokens = tokens.ToImmutableArray();
            _diagnostics.AddRange(lexer.Diagnostics);
            _source = source;
        }

        public SyntaxTree Parse()
        {
            ExprNode expr = ParseExpr();
            Token eofToken = Match(SyntaxKind.Eof);
            return new(_source, _diagnostics, expr, eofToken);
        }

        public ExprNode ParseExpr() => ParseAssignmentExpr();
        private ExprNode ParseAssignmentExpr()
        {
            if (Current.Kind == SyntaxKind.Identifier && Peek(1).Kind == SyntaxKind.Eq)
            {
                Token id = Advance();
                Advance();
                ExprNode right = ParseAssignmentExpr();
                return new AssignmentExpr(id, right);
            }

            return ParseBinExpr();
        }

        public ExprNode ParseBinExpr(ushort parentPrecedence = 0)
        {
            ExprNode left;
            ushort unOpPrec = Current.Kind.GetUnOpPrecedence();
            if (unOpPrec != 0 && unOpPrec >= parentPrecedence)
            {
                Token op = Advance();
                left = new UnaryExpr(op, ParseBinExpr(unOpPrec));
            }
            else
                left = ParsePrimaryExpr();

            while (true)
            {
                ushort precendence = Current.Kind.GetBinOpPrecedence();
                if (precendence <= parentPrecedence)
                    break;

                Token op = Advance();
                ExprNode right = ParseBinExpr(precendence);
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
                case SyntaxKind.Identifier:
                    {
                        Token id = Advance();
                        return new NameExpr(id);
                    }
                case SyntaxKind.True:
                case SyntaxKind.False:
                    return new LiteralExpr(Current, Advance().Kind == SyntaxKind.True);
                case SyntaxKind.Float:
                    return new LiteralExpr(Advance());
                default:
                    Token number = Match(SyntaxKind.Int);
                    return new LiteralExpr(number);
            }
        }

        private Token Match(SyntaxKind kind)
        {
            if (Current.Kind == kind)
                return Advance();

            _diagnostics.Report(Current.Span, $"Unexpected token: \"{Current.Kind}\" - expected \"{kind}\".");
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
