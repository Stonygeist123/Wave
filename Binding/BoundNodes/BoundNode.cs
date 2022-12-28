namespace Wave.Binding.BoundNodes
{
    public enum BoundNodeKind
    {
        LiteralExpr,
        UnaryExpr,
        BinaryExpr
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
        Mod,
        LogicAnd,
        LogicOr,
        EqEq,
        NotEq
    }

    public abstract class BoundNode
    {
        public abstract BoundNodeKind Kind { get; }
    }
}
