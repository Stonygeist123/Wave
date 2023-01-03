namespace Wave.Binding.BoundNodes
{
    public enum BoundUnOpKind
    {
        Plus,
        Minus,
        Bang,
        Inv
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

    public abstract class BoundExpr : BoundNode
    {
        public abstract Type Type { get; }
    }

    public sealed class BoundLiteral : BoundExpr
    {
        public override Type Type => Value.GetType();
        public override BoundNodeKind Kind => BoundNodeKind.LiteralExpr;
        public object Value { get; }
        public BoundLiteral(object value) => Value = value;
    }

    public sealed class BoundUnary : BoundExpr
    {
        public override Type Type => Op.ResultType;
        public override BoundNodeKind Kind => BoundNodeKind.UnaryExpr;
        public BoundUnOperator Op { get; }
        public BoundExpr Operand { get; }
        public BoundUnary(BoundUnOperator op, BoundExpr operand)
        {
            Op = op;
            Operand = operand;
        }
    }

    public sealed class BoundBinary : BoundExpr
    {
        public override Type Type => Op.ResultType;
        public override BoundNodeKind Kind => BoundNodeKind.BinaryExpr;
        public BoundExpr Left { get; }
        public BoundBinOperator Op { get; }
        public BoundExpr Right { get; private set; }

        public BoundBinary(BoundExpr left, BoundBinOperator op, BoundExpr right)
        {
            Left = left;
            Op = op;
            Right = right;
        }
    }

    public sealed class BoundName : BoundExpr
    {
        public override Type Type => Variable.Type;
        public override BoundNodeKind Kind => BoundNodeKind.NameExpr;
        public VariableSymbol Variable { get; }
        public BoundName(VariableSymbol variable) => Variable = variable;
    }

    public sealed class BoundAssignment : BoundExpr
    {
        public override Type Type => Value.Type;
        public override BoundNodeKind Kind => BoundNodeKind.AssignmentExpr;
        public VariableSymbol Variable { get; }
        public BoundExpr Value { get; }
        public BoundAssignment(VariableSymbol variable, BoundExpr value)
        {
            Variable = variable;
            Value = value;
        }
    }
}
