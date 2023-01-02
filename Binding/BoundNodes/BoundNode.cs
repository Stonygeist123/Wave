namespace Wave.Binding.BoundNodes
{
    public enum BoundNodeKind
    {
        // Expr
        LiteralExpr,
        UnaryExpr,
        BinaryExpr,
        NameExpr,

        // Stmt
        ExpressionStmt,
        BlockStmt,
        VarStmt,
        ForStmt
    }

    public enum BoundUnOpKind
    {
        Plus,
        Minus,
        Bang
    }

    public enum BoundBinOpKind
    {
        Plus,
        Minus,
        Star,
        Slash,
        Power,
        Mod,
        And,
        Or,
        Xor,
        LogicAnd,
        LogicOr,
        EqEq,
        NotEq,
        Greater,
        Less,
        GreaterEq,
        LessEq
    }

    public abstract class BoundNode
    {
        public abstract BoundNodeKind Kind { get; }
    }
}
