using Wave.Binding;
using Wave.Binding.BoundNodes;

namespace Wave
{
    internal class Evaluator
    {
        private readonly BoundStmt _root;
        private readonly Dictionary<VariableSymbol, object?> _variables;
        private object? _lastValue = null;
        public Evaluator(BoundStmt root, Dictionary<VariableSymbol, object?> variables)
        {
            _root = root;
            _variables = variables;
        }

        public object? Evaluate()
        {
            EvaluateStmt(_root);
            return _lastValue;
        }

        private void EvaluateStmt(BoundStmt stmt)
        {
            switch (stmt)
            {
                case BoundExpressionStmt e:
                    {
                        _lastValue = EvaluateExpr(e.Expr);
                        break;
                    }
                case BoundBlockStmt b:
                    {
                        foreach (BoundStmt s in b.Stmts)
                            EvaluateStmt(s);
                        break;
                    }
                case BoundVarStmt v:
                    {
                        object value = EvaluateExpr(v.Value);
                        _variables[v.Variable] = value;
                        _lastValue = value;
                        break;
                    }
                case BoundIfStmt i:
                    {
                        bool condition = (bool)EvaluateExpr(i.Condition);
                        if (condition)
                            EvaluateStmt(i.ThenBranch);
                        else if (i.ElseClause is not null)
                            EvaluateStmt(i.ElseClause);
                        break;
                    }
                case BoundWhileStmt w:
                    {
                        bool condition = (bool)EvaluateExpr(w.Condition);
                        while (condition)
                            EvaluateStmt(w.Stmt);
                        break;
                    }
                case BoundForStmt f:
                    {
                        int lowerBound = (int)EvaluateExpr(f.LowerBound);
                        int upperBound = (int)EvaluateExpr(f.UpperBound);

                        for (int i = lowerBound; i <= upperBound; ++i)
                        {
                            _variables[f.Variable] = i;
                            EvaluateStmt(f.Stmt);
                        }

                        break;
                    }
            }
        }

        private object EvaluateExpr(BoundExpr expr)
        {
            switch (expr)
            {
                case BoundLiteral l:
                    return l.Value ?? 0;
                case BoundUnary u:
                    {
                        object v = EvaluateExpr(u.Operand);
                        return u.Op.Kind switch
                        {
                            BoundUnOpKind.Plus => (int)v,
                            BoundUnOpKind.Minus => -(int)v,
                            BoundUnOpKind.Bang => !(bool)v,
                            _ => throw new Exception($"Unexpected unary operator \"{u.Op}\".")
                        };
                    }
                case BoundBinary b:
                    {
                        object left = EvaluateExpr(b.Left);
                        object right = EvaluateExpr(b.Right);

                        return b.Op.Kind switch
                        {
                            BoundBinOpKind.Plus => (int)left + (int)right,
                            BoundBinOpKind.Minus => (int)left - (int)right,
                            BoundBinOpKind.Star => (int)left * (int)right,
                            BoundBinOpKind.Slash => (int)left / (int)right,
                            BoundBinOpKind.Power => (int)Math.Pow((int)left, (int)right),
                            BoundBinOpKind.Mod => (int)left % (int)right,
                            BoundBinOpKind.And => (int)left & (int)right,
                            BoundBinOpKind.Or => (int)left | (int)right,
                            BoundBinOpKind.Xor => (int)left ^ (int)right,
                            BoundBinOpKind.LogicAnd => (bool)left && (bool)right,
                            BoundBinOpKind.LogicOr => (bool)left || (bool)right,
                            BoundBinOpKind.EqEq => Equals(left, right),
                            BoundBinOpKind.NotEq => !Equals(left, right),
                            BoundBinOpKind.Greater => (int)left > (int)right,
                            BoundBinOpKind.Less => (int)left < (int)right,
                            BoundBinOpKind.GreaterEq => (int)left >= (int)right,
                            BoundBinOpKind.LessEq => (int)left <= (int)right,
                            _ => throw new Exception($"Unexpected binary operator \"{b.Op}\".")
                        };
                    }
                case BoundName n:
                    return _variables[n.Variable] ?? 0;
                case BoundAssignment a:
                    return _variables[a.Variable] = EvaluateExpr(a.Value);
            }

            throw new Exception($"Unexpected expression.");
        }

        private static new bool Equals(object left, object right)
        {
            if (left == null || right == null)
                return false;

            return left.Equals(right);
        }
    }
}
