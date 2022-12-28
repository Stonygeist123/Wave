using Wave.Binding.BoundNodes;
using Wave.Nodes;

namespace Wave.Binding
{
    internal class Binder
    {
        private readonly List<string> _diagnostics = new();
        public IEnumerable<string> Diagnostics => _diagnostics;

        public BoundExpr BindExpr(ExprNode expr)
        {
            return expr switch
            {
                LiteralExpr l => new BoundLiteral(l.Value),
                UnaryExpr u => BindUnaryExpr(u),
                BinaryExpr b => BindBinaryExpr(b),
                GroupingExpr b => BindExpr(b.Expr),
                _ => throw new Exception("Unexpected syntax."),
            };
        }

        private BoundExpr BindUnaryExpr(UnaryExpr u)
        {
            BoundExpr operand = BindExpr(u.Operand);
            BoundUnOperator? op = BoundUnOperator.Bind(u.Op.Kind, operand.Type);
            if (op is not null)
                return new BoundUnary(op, operand);

            _diagnostics.Add($"Unary operator \"{u.Op.Kind}\" is not defined for type \"{operand.Type}\".");
            return operand;
        }

        private BoundExpr BindBinaryExpr(BinaryExpr b)
        {
            BoundExpr left = BindExpr(b.Left);
            BoundExpr right = BindExpr(b.Right);
            BoundBinOperator? op = BoundBinOperator.Bind(b.Op.Kind, left.Type, right.Type);

            if (op is not null)
                return new BoundBinary(left, op, right);

            _diagnostics.Add($"Binary operator \"{b.Op.Kind}\" is not defined for types \"{left.Type}\" and \"{right.Type}\".");
            return left;
        }

        private static BoundUnOpKind? BindUnOpKind(SyntaxKind kind, Type operandType)
        {
            if (operandType == typeof(int))
                return kind switch
                {
                    SyntaxKind.Plus => BoundUnOpKind.Plus,
                    SyntaxKind.Minus => BoundUnOpKind.Minus,
                    _ => null,
                };
            else if (operandType == typeof(bool))
                return kind switch
                {
                    SyntaxKind.Bang => BoundUnOpKind.Bang,
                    _ => null,
                };

            return null;
        }

        private static BoundBinOpKind? BindBinOpKind(SyntaxKind kind, Type leftType, Type rightType)
        {
            if (leftType == typeof(int) || rightType == typeof(int))
                return kind switch
                {
                    SyntaxKind.Plus => BoundBinOpKind.Plus,
                    SyntaxKind.Minus => BoundBinOpKind.Minus,
                    SyntaxKind.Star => BoundBinOpKind.Star,
                    SyntaxKind.Slash => BoundBinOpKind.Slash,
                    SyntaxKind.Mod => BoundBinOpKind.Mod,
                    _ => null,
                };
            else if (leftType == typeof(bool) && rightType == typeof(bool))
                return kind switch
                {
                    SyntaxKind.LogicAnd => BoundBinOpKind.LogicAnd,
                    SyntaxKind.LogicOr => BoundBinOpKind.LogicOr,
                    _ => null,
                };

            return null;
        }
    }
}
