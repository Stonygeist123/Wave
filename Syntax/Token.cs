﻿using Wave.Nodes;

namespace Wave
{
    public enum SyntaxKind
    {
        // Token
        Space,
        Plus,
        Minus,
        Star,
        Slash,
        Mod,
        LParen,
        RParen,
        Bang,
        LogicAnd,
        LogicOr,
        EqEq,
        NotEq,
        Eq,
        Bad,
        Eof,

        // Literals
        Int,
        Float,
        Identifier,

        // Keywords
        True,
        False,

        // Expr
        LiteralExpr,
        BinaryExpr,
        UnaryExpr,
        GroupingExpr,
        NameExpr
    }

    public class Token : Node
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
        public TextSpan Span => new(Position, Lexeme.Length);

        public override IEnumerable<Node> GetChildren() => Enumerable.Empty<Node>();
    }
}
