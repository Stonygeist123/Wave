using Wave.Binding.BoundNodes;

namespace Wave.Binding
{
    public sealed class BoundUnOperator
    {
        public BoundUnOperator(SyntaxKind syntaxKind, BoundUnOpKind kind, Type operandType)
            : this(syntaxKind, kind, operandType, operandType)
        {
        }

        public BoundUnOperator(SyntaxKind syntaxKind, BoundUnOpKind kind, Type operandType, Type resultType)
        {
            SyntaxKind = syntaxKind;
            Kind = kind;
            OperandType = operandType;
            ResultType = resultType;
        }

        public SyntaxKind SyntaxKind { get; }
        public BoundUnOpKind Kind { get; }
        public Type OperandType { get; }
        public Type ResultType { get; }

        private static readonly BoundUnOperator[] _operators = {
            new(SyntaxKind.Plus, BoundUnOpKind.Plus, typeof(int)),
            new(SyntaxKind.Minus, BoundUnOpKind.Minus, typeof(int)),
            new(SyntaxKind.Inv, BoundUnOpKind.Inv, typeof(int)),
            new(SyntaxKind.Bang, BoundUnOpKind.Bang, typeof(bool))
        };

        public static BoundUnOperator? Bind(SyntaxKind kind, Type operandType)
        {
            foreach (BoundUnOperator op in _operators)
                if (op.SyntaxKind == kind && op.OperandType == operandType)
                    return op;

            return null;
        }
    }

    public sealed class BoundBinOperator
    {
        public BoundBinOperator(SyntaxKind syntaxKind, BoundBinOpKind kind, Type type)
            : this(syntaxKind, kind, type, type, type)
        {
        }

        public BoundBinOperator(SyntaxKind syntaxKind, BoundBinOpKind kind, Type operandType, Type resultType)
            : this(syntaxKind, kind, operandType, operandType, resultType)
        {
        }

        public BoundBinOperator(SyntaxKind syntaxKind, BoundBinOpKind kind, Type leftType, Type rightType, Type resultType)
        {
            LeftType = leftType;
            SyntaxKind = syntaxKind;
            Kind = kind;
            RightType = rightType;
            ResultType = resultType;
        }

        public Type LeftType { get; }
        public SyntaxKind SyntaxKind { get; }
        public BoundBinOpKind Kind { get; private set; }
        public Type RightType { get; }
        public Type ResultType { get; }

        private static readonly BoundBinOperator[] _operators = {
            new(SyntaxKind.Plus, BoundBinOpKind.Plus, typeof(int)),
            new(SyntaxKind.Minus, BoundBinOpKind.Minus, typeof(int)),
            new(SyntaxKind.Star, BoundBinOpKind.Star, typeof(int)),
            new(SyntaxKind.Slash, BoundBinOpKind.Slash, typeof(int)),
            new(SyntaxKind.Power, BoundBinOpKind.Power, typeof(int)),
            new(SyntaxKind.Mod, BoundBinOpKind.Mod, typeof(int)),
            new(SyntaxKind.And, BoundBinOpKind.And, typeof(int)),
            new(SyntaxKind.Or, BoundBinOpKind.Or, typeof(int)),
            new(SyntaxKind.Xor, BoundBinOpKind.Xor, typeof(int)),
            new(SyntaxKind.EqEq, BoundBinOpKind.EqEq, typeof(int), typeof(bool)),
            new(SyntaxKind.NotEq, BoundBinOpKind.NotEq, typeof(int), typeof(bool)),
            new(SyntaxKind.Greater, BoundBinOpKind.Greater, typeof(int), typeof(bool)),
            new(SyntaxKind.Less, BoundBinOpKind.Less, typeof(int), typeof(bool)),
            new(SyntaxKind.GreaterEq, BoundBinOpKind.GreaterEq, typeof(int), typeof(bool)),
            new(SyntaxKind.LessEq, BoundBinOpKind.LessEq, typeof(int), typeof(bool)),

            new(SyntaxKind.Plus, BoundBinOpKind.Plus, typeof(double)),
            new(SyntaxKind.Minus, BoundBinOpKind.Minus, typeof(double)),
            new(SyntaxKind.Star, BoundBinOpKind.Star, typeof(double)),
            new(SyntaxKind.Slash, BoundBinOpKind.Slash, typeof(double)),
            new(SyntaxKind.Power, BoundBinOpKind.Power, typeof(double)),
            new(SyntaxKind.EqEq, BoundBinOpKind.EqEq, typeof(double), typeof(bool)),
            new(SyntaxKind.NotEq, BoundBinOpKind.NotEq, typeof(double), typeof(bool)),
            new(SyntaxKind.Greater, BoundBinOpKind.Greater, typeof(double), typeof(bool)),
            new(SyntaxKind.Less, BoundBinOpKind.Less, typeof(double), typeof(bool)),
            new(SyntaxKind.GreaterEq, BoundBinOpKind.GreaterEq, typeof(double), typeof(bool)),
            new(SyntaxKind.LessEq, BoundBinOpKind.LessEq, typeof(double), typeof(bool)),

            new(SyntaxKind.Plus, BoundBinOpKind.Plus, typeof(double), typeof(int), typeof(double)),
            new(SyntaxKind.Minus, BoundBinOpKind.Minus, typeof(double), typeof(int), typeof(double)),
            new(SyntaxKind.Star, BoundBinOpKind.Star, typeof(double), typeof(int), typeof(double)),
            new(SyntaxKind.Slash, BoundBinOpKind.Slash, typeof(double), typeof(int), typeof(double)),
            new(SyntaxKind.Power, BoundBinOpKind.Power, typeof(double), typeof(int), typeof(double)),
            new(SyntaxKind.EqEq, BoundBinOpKind.EqEq, typeof(double), typeof(int), typeof(bool)),
            new(SyntaxKind.NotEq, BoundBinOpKind.NotEq, typeof(double), typeof(int), typeof(bool)),
            new(SyntaxKind.Greater, BoundBinOpKind.Greater, typeof(double), typeof(int), typeof(bool)),
            new(SyntaxKind.Less, BoundBinOpKind.Less, typeof(double), typeof(int), typeof(bool)),
            new(SyntaxKind.GreaterEq, BoundBinOpKind.GreaterEq, typeof(double), typeof(int), typeof(bool)),
            new(SyntaxKind.LessEq, BoundBinOpKind.LessEq, typeof(double), typeof(int), typeof(bool)),

            new(SyntaxKind.Plus, BoundBinOpKind.Plus, typeof(int), typeof(double), typeof(double)),
            new(SyntaxKind.Minus, BoundBinOpKind.Minus,  typeof(int),typeof(double), typeof(double)),
            new(SyntaxKind.Star, BoundBinOpKind.Star, typeof(int), typeof(double), typeof(double)),
            new(SyntaxKind.Slash, BoundBinOpKind.Slash, typeof(int), typeof(double), typeof(double)),
            new(SyntaxKind.Power, BoundBinOpKind.Power, typeof(int), typeof(double), typeof(double)),
            new(SyntaxKind.EqEq, BoundBinOpKind.EqEq,  typeof(int),typeof(double), typeof(bool)),
            new(SyntaxKind.NotEq, BoundBinOpKind.NotEq, typeof(int), typeof(double), typeof(bool)),
            new(SyntaxKind.Greater, BoundBinOpKind.Greater, typeof(int), typeof(double), typeof(bool)),
            new(SyntaxKind.Less, BoundBinOpKind.Less, typeof(int), typeof(double), typeof(bool)),
            new(SyntaxKind.GreaterEq, BoundBinOpKind.GreaterEq, typeof(int), typeof(double), typeof(bool)),
            new(SyntaxKind.LessEq, BoundBinOpKind.LessEq, typeof(int), typeof(double), typeof(bool)),

            new(SyntaxKind.LogicAnd, BoundBinOpKind.LogicAnd, typeof(bool)),
            new(SyntaxKind.LogicOr, BoundBinOpKind.LogicOr, typeof(bool)),
            new(SyntaxKind.EqEq, BoundBinOpKind.EqEq, typeof(bool)),
            new(SyntaxKind.NotEq, BoundBinOpKind.NotEq, typeof(bool))
        };


        public static BoundBinOperator? Bind(SyntaxKind kind, Type leftType, Type rightType)
        {
            foreach (BoundBinOperator op in _operators)
                if (op.SyntaxKind == kind && op.LeftType == leftType && op.RightType == rightType)
                    return op;

            return null;
        }
    }
}
