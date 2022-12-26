using Wave.Nodes;

namespace Wave
{
    internal class Evaluator
    {
        private readonly ExprNode _root;
        public Evaluator(ExprNode root) => _root = root;

        public int Evaluate() => EvaluateExpr(_root);
        private int EvaluateExpr(ExprNode expr)
        {
            if (expr is NumberExpr n)
                return (int?)n.Token.Value ?? 0;
            if (expr is GroupingExpr g)
                return EvaluateExpr(g.Expr);
            else if (expr is BinaryExpr b)
            {
                int left = EvaluateExpr(b.Left);
                int right = EvaluateExpr(b.Right);

                return (b.Op.Kind) switch
                {
                    SyntaxKind.Plus => left + right,
                    SyntaxKind.Minus => left - right,
                    SyntaxKind.Star => left * right,
                    SyntaxKind.Slash => left / right,
                    SyntaxKind.Mod => left % right,
                    _ => throw new Exception($"Unexpected binary operator \"{b.Op.Kind}\".")
                };
            }

            throw new Exception($"Unexpected expression.");
        }
    }
}
