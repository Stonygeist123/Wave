using Wave.Binding.BoundNodes;
using Wave.Symbols;
using Wave.Syntax.Nodes;

namespace Wave.Binding
{
    public sealed class BoundUnOperator
    {
        public BoundUnOperator(SyntaxKind syntaxKind, BoundUnOpKind kind, TypeSymbol operandType)
            : this(syntaxKind, kind, operandType, operandType)
        {
        }

        public BoundUnOperator(SyntaxKind syntaxKind, BoundUnOpKind kind, TypeSymbol operandType, TypeSymbol resultType)
        {
            SyntaxKind = syntaxKind;
            Kind = kind;
            OperandType = operandType;
            ResultType = resultType;
        }

        public SyntaxKind SyntaxKind { get; }
        public BoundUnOpKind Kind { get; }
        public TypeSymbol OperandType { get; }
        public TypeSymbol ResultType { get; }

        private static readonly BoundUnOperator[] _operators = {
            new(SyntaxKind.Plus, BoundUnOpKind.Plus, TypeSymbol.Int),
            new(SyntaxKind.Minus, BoundUnOpKind.Minus, TypeSymbol.Int),
            new(SyntaxKind.Inv, BoundUnOpKind.Inv, TypeSymbol.Int),
            new(SyntaxKind.Bang, BoundUnOpKind.Bang, TypeSymbol.Bool)
        };

        public static BoundUnOperator? Bind(SyntaxKind kind, TypeSymbol operandType)
        {
            foreach (BoundUnOperator op in _operators)
                if (op.SyntaxKind == kind && op.OperandType == operandType)
                    return op;

            return null;
        }
    }

    public sealed class BoundBinOperator
    {
        public BoundBinOperator(SyntaxKind syntaxKind, BoundBinOpKind kind, TypeSymbol type)
            : this(syntaxKind, kind, type, type, type)
        {
        }

        public BoundBinOperator(SyntaxKind syntaxKind, BoundBinOpKind kind, TypeSymbol operandType, TypeSymbol resultType)
            : this(syntaxKind, kind, operandType, operandType, resultType)
        {
        }

        public BoundBinOperator(SyntaxKind syntaxKind, BoundBinOpKind kind, TypeSymbol leftType, TypeSymbol rightType, TypeSymbol resultType)
        {
            LeftType = leftType;
            SyntaxKind = syntaxKind;
            Kind = kind;
            RightType = rightType;
            ResultType = resultType;
        }

        public TypeSymbol LeftType { get; }
        public SyntaxKind SyntaxKind { get; }
        public BoundBinOpKind Kind { get; private set; }
        public TypeSymbol RightType { get; }
        public TypeSymbol ResultType { get; }

        private static readonly BoundBinOperator[] _operators = {
            new(SyntaxKind.Plus, BoundBinOpKind.Plus, TypeSymbol.Int),
            new(SyntaxKind.Minus, BoundBinOpKind.Minus, TypeSymbol.Int),
            new(SyntaxKind.Star, BoundBinOpKind.Star, TypeSymbol.Int),
            new(SyntaxKind.Slash, BoundBinOpKind.Slash, TypeSymbol.Int),
            new(SyntaxKind.Power, BoundBinOpKind.Power, TypeSymbol.Int),
            new(SyntaxKind.Mod, BoundBinOpKind.Mod, TypeSymbol.Int),
            new(SyntaxKind.And, BoundBinOpKind.And, TypeSymbol.Int),
            new(SyntaxKind.Or, BoundBinOpKind.Or, TypeSymbol.Int),
            new(SyntaxKind.Xor, BoundBinOpKind.Xor, TypeSymbol.Int),
            new(SyntaxKind.EqEq, BoundBinOpKind.EqEq, TypeSymbol.Int, TypeSymbol.Bool),
            new(SyntaxKind.NotEq, BoundBinOpKind.NotEq, TypeSymbol.Int, TypeSymbol.Bool),
            new(SyntaxKind.Greater, BoundBinOpKind.Greater, TypeSymbol.Int, TypeSymbol.Bool),
            new(SyntaxKind.Less, BoundBinOpKind.Less, TypeSymbol.Int, TypeSymbol.Bool),
            new(SyntaxKind.GreaterEq, BoundBinOpKind.GreaterEq, TypeSymbol.Int, TypeSymbol.Bool),
            new(SyntaxKind.LessEq, BoundBinOpKind.LessEq, TypeSymbol.Int, TypeSymbol.Bool),

            new(SyntaxKind.Plus, BoundBinOpKind.Plus, TypeSymbol.Int, TypeSymbol.String, TypeSymbol.String),

            new(SyntaxKind.Plus, BoundBinOpKind.Plus, TypeSymbol.Float),
            new(SyntaxKind.Minus, BoundBinOpKind.Minus, TypeSymbol.Float),
            new(SyntaxKind.Star, BoundBinOpKind.Star, TypeSymbol.Float),
            new(SyntaxKind.Slash, BoundBinOpKind.Slash, TypeSymbol.Float),
            new(SyntaxKind.Power, BoundBinOpKind.Power, TypeSymbol.Float),
            new(SyntaxKind.EqEq, BoundBinOpKind.EqEq, TypeSymbol.Float, TypeSymbol.Bool),
            new(SyntaxKind.NotEq, BoundBinOpKind.NotEq, TypeSymbol.Float, TypeSymbol.Bool),
            new(SyntaxKind.Greater, BoundBinOpKind.Greater, TypeSymbol.Float, TypeSymbol.Bool),
            new(SyntaxKind.Less, BoundBinOpKind.Less, TypeSymbol.Float, TypeSymbol.Bool),
            new(SyntaxKind.GreaterEq, BoundBinOpKind.GreaterEq, TypeSymbol.Float, TypeSymbol.Bool),
            new(SyntaxKind.LessEq, BoundBinOpKind.LessEq, TypeSymbol.Float, TypeSymbol.Bool),

            new(SyntaxKind.Plus, BoundBinOpKind.Plus, TypeSymbol.Float, TypeSymbol.String, TypeSymbol.String),

            new(SyntaxKind.Plus, BoundBinOpKind.Plus, TypeSymbol.Float, TypeSymbol.Int, TypeSymbol.Float),
            new(SyntaxKind.Minus, BoundBinOpKind.Minus, TypeSymbol.Float, TypeSymbol.Int, TypeSymbol.Float),
            new(SyntaxKind.Star, BoundBinOpKind.Star, TypeSymbol.Float, TypeSymbol.Int, TypeSymbol.Float),
            new(SyntaxKind.Slash, BoundBinOpKind.Slash, TypeSymbol.Float, TypeSymbol.Int, TypeSymbol.Float),
            new(SyntaxKind.Power, BoundBinOpKind.Power, TypeSymbol.Float, TypeSymbol.Int, TypeSymbol.Float),
            new(SyntaxKind.EqEq, BoundBinOpKind.EqEq, TypeSymbol.Float, TypeSymbol.Int, TypeSymbol.Bool),
            new(SyntaxKind.NotEq, BoundBinOpKind.NotEq, TypeSymbol.Float, TypeSymbol.Int, TypeSymbol.Bool),
            new(SyntaxKind.Greater, BoundBinOpKind.Greater, TypeSymbol.Float, TypeSymbol.Int, TypeSymbol.Bool),
            new(SyntaxKind.Less, BoundBinOpKind.Less, TypeSymbol.Float, TypeSymbol.Int, TypeSymbol.Bool),
            new(SyntaxKind.GreaterEq, BoundBinOpKind.GreaterEq, TypeSymbol.Float, TypeSymbol.Int, TypeSymbol.Bool),
            new(SyntaxKind.LessEq, BoundBinOpKind.LessEq, TypeSymbol.Float, TypeSymbol.Int, TypeSymbol.Bool),

            new(SyntaxKind.Plus, BoundBinOpKind.Plus, TypeSymbol.Int, TypeSymbol.Float, TypeSymbol.Float),
            new(SyntaxKind.Minus, BoundBinOpKind.Minus,  TypeSymbol.Int,TypeSymbol.Float, TypeSymbol.Float),
            new(SyntaxKind.Star, BoundBinOpKind.Star, TypeSymbol.Int, TypeSymbol.Float, TypeSymbol.Float),
            new(SyntaxKind.Slash, BoundBinOpKind.Slash, TypeSymbol.Int, TypeSymbol.Float, TypeSymbol.Float),
            new(SyntaxKind.Power, BoundBinOpKind.Power, TypeSymbol.Int, TypeSymbol.Float, TypeSymbol.Float),
            new(SyntaxKind.EqEq, BoundBinOpKind.EqEq,  TypeSymbol.Int,TypeSymbol.Float, TypeSymbol.Bool),
            new(SyntaxKind.NotEq, BoundBinOpKind.NotEq, TypeSymbol.Int, TypeSymbol.Float, TypeSymbol.Bool),
            new(SyntaxKind.Greater, BoundBinOpKind.Greater, TypeSymbol.Int, TypeSymbol.Float, TypeSymbol.Bool),
            new(SyntaxKind.Less, BoundBinOpKind.Less, TypeSymbol.Int, TypeSymbol.Float, TypeSymbol.Bool),
            new(SyntaxKind.GreaterEq, BoundBinOpKind.GreaterEq, TypeSymbol.Int, TypeSymbol.Float, TypeSymbol.Bool),
            new(SyntaxKind.LessEq, BoundBinOpKind.LessEq, TypeSymbol.Int, TypeSymbol.Float, TypeSymbol.Bool),

            new(SyntaxKind.LogicAnd, BoundBinOpKind.LogicAnd, TypeSymbol.Bool),
            new(SyntaxKind.LogicOr, BoundBinOpKind.LogicOr, TypeSymbol.Bool),
            new(SyntaxKind.EqEq, BoundBinOpKind.EqEq, TypeSymbol.Bool),
            new(SyntaxKind.NotEq, BoundBinOpKind.NotEq, TypeSymbol.Bool),

            new(SyntaxKind.Plus, BoundBinOpKind.Plus, TypeSymbol.String),
            new(SyntaxKind.EqEq, BoundBinOpKind.EqEq, TypeSymbol.String),
            new(SyntaxKind.NotEq, BoundBinOpKind.NotEq, TypeSymbol.String),

            new(SyntaxKind.Plus, BoundBinOpKind.Plus, TypeSymbol.String, TypeSymbol.Float, TypeSymbol.String),
            new(SyntaxKind.Plus, BoundBinOpKind.Plus, TypeSymbol.String, TypeSymbol.Int, TypeSymbol.String),
            new(SyntaxKind.Minus, BoundBinOpKind.Minus, TypeSymbol.String, TypeSymbol.Int, TypeSymbol.String),
            new(SyntaxKind.Star, BoundBinOpKind.Star, TypeSymbol.String, TypeSymbol.Int, TypeSymbol.String),
        };


        public static BoundBinOperator? Bind(SyntaxKind kind, TypeSymbol leftType, TypeSymbol rightType)
        {
            foreach (BoundBinOperator op in _operators)
                if (op.SyntaxKind == kind && op.LeftType == leftType && op.RightType == rightType)
                    return op;

            return null;
        }
    }
}
