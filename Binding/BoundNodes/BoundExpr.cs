using System.Collections.Immutable;
using Wave.Symbols;

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
        public abstract TypeSymbol Type { get; }
    }

    public sealed class BoundLiteral : BoundExpr
    {
        public override TypeSymbol Type { get; }
        public override BoundNodeKind Kind => BoundNodeKind.LiteralExpr;
        public object Value { get; }
        public BoundLiteral(object value)
        {
            Value = value;
            if (value is int)
                Type = TypeSymbol.Int;
            else if (value is double)
                Type = TypeSymbol.Float;
            else if (value is bool)
                Type = TypeSymbol.Bool;
            else if (value is string)
                Type = TypeSymbol.String;
            else
                throw new Exception($"Unexpected literal \"{value}\" of type \"{value.GetType()}\".");
        }
    }

    public sealed class BoundUnary : BoundExpr
    {
        public override TypeSymbol Type => Op.ResultType;
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
        public override TypeSymbol Type => Op.ResultType;
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
        public override TypeSymbol Type => Variable.Type;
        public override BoundNodeKind Kind => BoundNodeKind.NameExpr;
        public VariableSymbol Variable { get; }
        public BoundName(VariableSymbol variable) => Variable = variable;
    }

    public sealed class BoundAssignment : BoundExpr
    {
        public override TypeSymbol Type => Value.Type;
        public override BoundNodeKind Kind => BoundNodeKind.AssignmentExpr;
        public VariableSymbol Variable { get; }
        public BoundExpr Value { get; }
        public BoundAssignment(VariableSymbol variable, BoundExpr value)
        {
            Variable = variable;
            Value = value;
        }
    }

    public sealed class BoundCall : BoundExpr
    {
        public override TypeSymbol Type => Function.Type;
        public override BoundNodeKind Kind => BoundNodeKind.CallExpr;
        public FunctionSymbol Function { get; }
        public ImmutableArray<BoundExpr> Args { get; }
        public BoundCall(FunctionSymbol function, ImmutableArray<BoundExpr> args)
        {
            Function = function;
            Args = args;
        }
    }

    public sealed class BoundConversion : BoundExpr
    {
        public override TypeSymbol Type { get; }
        public override BoundNodeKind Kind => BoundNodeKind.ConversionExpr;
        public BoundExpr Expr { get; }

        public BoundConversion(TypeSymbol type, BoundExpr expr)
        {
            Type = type;
            Expr = expr;
        }
    }

    public sealed class BoundError : BoundExpr
    {
        public override TypeSymbol Type => TypeSymbol.Unknown;
        public override BoundNodeKind Kind => BoundNodeKind.ErrorExpr;
    }
}
