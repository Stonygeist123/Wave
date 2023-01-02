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
        Power,
        Mod,
        And,
        Or,
        Xor,
        LParen,
        RParen,
        LBrace,
        RBrace,
        Bang,
        LogicAnd,
        LogicOr,
        EqEq,
        NotEq,
        Eq,
        Semicolon,
        Bad,
        Eof,

        // Literals
        Int,
        Float,
        Identifier,

        // Keywords
        True,
        False,
        Var,
        Mut,

        // Nodes
        CompilationUnit,

        // Expr
        LiteralExpr,
        BinaryExpr,
        UnaryExpr,
        GroupingExpr,
        NameExpr,

        // Stmt
        ExpressionStmt,
        BlockStmt,
        VarStmt
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
        public new TextSpan Span => new(Position, Lexeme.Length);
    }
}