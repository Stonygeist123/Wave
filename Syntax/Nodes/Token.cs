using Wave.Nodes;

namespace Wave.Syntax.Nodes
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
        Inv,
        LParen,
        RParen,
        LBrace,
        RBrace,
        Bang,
        LogicAnd,
        LogicOr,
        EqEq,
        NotEq,
        Greater,
        Less,
        GreaterEq,
        LessEq,
        Eq,
        Comma,
        Semicolon,
        Colon,
        Arrow,
        Bad,
        Eof,

        // Literals
        Int,
        Float,
        Identifier,
        String,

        // Keywords
        True,
        False,
        Var,
        Mut,
        If,
        Else,
        While,
        Do,
        For,
        Fn,

        // Nodes
        CompilationUnit,
        FnDecl,
        GlobalStmt,
        ElseClause,
        TypeClause,
        ParameterSyntax,

        // Expr
        LiteralExpr,
        BinaryExpr,
        UnaryExpr,
        GroupingExpr,
        NameExpr,
        AssignmentExpr,
        CallExpr,

        // Stmt
        ExpressionStmt,
        BlockStmt,
        VarStmt,
        IfStmt,
        ForStmt
    }

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
