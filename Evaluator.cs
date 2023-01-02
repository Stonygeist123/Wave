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
                            BoundUnOpKind.Inv => ~(int)v,
                            _ => throw new Exception($"Unexpected unary operator \"{u.Op}\".")
                        };
                    }
                case BoundBinary b:
                    {
                        object left = EvaluateExpr(b.Left);
                        object right = EvaluateExpr(b.Right);

                        if (left is double lf)
                        {
                            if (right is double rf)
                                return b.Op.Kind switch
                                {
                                    BoundBinOpKind.Plus => lf + rf,
                                    BoundBinOpKind.Minus => lf - rf,
                                    BoundBinOpKind.Star => lf * rf,
                                    BoundBinOpKind.Slash => lf / rf,
                                    BoundBinOpKind.Power => Math.Pow(lf, rf),
                                    BoundBinOpKind.EqEq => Equals(lf, rf),
                                    BoundBinOpKind.NotEq => !Equals(lf, rf),
                                    BoundBinOpKind.Greater => lf > rf,
                                    BoundBinOpKind.Less => lf < rf,
                                    BoundBinOpKind.GreaterEq => lf >= rf,
                                    BoundBinOpKind.LessEq => lf <= rf,
                                    _ => throw new Exception($"Unexpected binary operator \"{b.Op}\".")
                                };
                            else if (right is int ri) return b.Op.Kind switch
                            {
                                BoundBinOpKind.Plus => lf + ri,
                                BoundBinOpKind.Minus => lf - ri,
                                BoundBinOpKind.Star => lf * ri,
                                BoundBinOpKind.Slash => lf / ri,
                                BoundBinOpKind.Power => Math.Pow(lf, ri),
                                BoundBinOpKind.EqEq => Equals(lf, ri),
                                BoundBinOpKind.NotEq => !Equals(lf, ri),
                                BoundBinOpKind.Greater => lf > ri,
                                BoundBinOpKind.Less => lf < ri,
                                BoundBinOpKind.GreaterEq => lf >= ri,
                                BoundBinOpKind.LessEq => lf <= ri,
                                _ => throw new Exception($"Unexpected binary operator \"{b.Op}\".")
                            };
                        }
                        else if (left is int li)
                        {

                            if (right is double rf)
                                return b.Op.Kind switch
                                {
                                    BoundBinOpKind.Plus => li + rf,
                                    BoundBinOpKind.Minus => li - rf,
                                    BoundBinOpKind.Star => li * rf,
                                    BoundBinOpKind.Slash => li / rf,
                                    BoundBinOpKind.Power => Math.Pow(li, rf),
                                    BoundBinOpKind.EqEq => Equals(li, rf),
                                    BoundBinOpKind.NotEq => !Equals(li, rf),
                                    BoundBinOpKind.Greater => li > rf,
                                    BoundBinOpKind.Less => li < rf,
                                    BoundBinOpKind.GreaterEq => li >= rf,
                                    BoundBinOpKind.LessEq => li <= rf,
                                    _ => throw new Exception($"Unexpected binary operator \"{b.Op}\".")
                                };
                            else if (right is int ri) return b.Op.Kind switch
                            {
                                BoundBinOpKind.Plus => li + ri,
                                BoundBinOpKind.Minus => li - ri,
                                BoundBinOpKind.Star => li * ri,
                                BoundBinOpKind.Slash => li / ri,
                                BoundBinOpKind.Power => (int)Math.Pow(li, ri),
                                BoundBinOpKind.Mod => (int)left % ri,
                                BoundBinOpKind.And => (int)left & ri,
                                BoundBinOpKind.Or => (int)left | ri,
                                BoundBinOpKind.Xor => (int)left ^ ri,
                                BoundBinOpKind.EqEq => Equals(li, ri),
                                BoundBinOpKind.NotEq => !Equals(li, ri),
                                BoundBinOpKind.Greater => li > ri,
                                BoundBinOpKind.Less => li < ri,
                                BoundBinOpKind.GreaterEq => li >= ri,
                                BoundBinOpKind.LessEq => li <= ri,
                                _ => throw new Exception($"Unexpected binary operator \"{b.Op}\".")
                            };
                        }
                        else if (left is bool lb)
                            if (right is bool rb)
                                return b.Op.Kind switch
                                {
                                    BoundBinOpKind.LogicAnd => lb && rb,
                                    BoundBinOpKind.LogicOr => lb || rb,
                                    BoundBinOpKind.EqEq => Equals(lb, rb),
                                    BoundBinOpKind.NotEq => !Equals(lb, rb),
                                    _ => throw new Exception($"Unexpected binary operator \"{b.Op}\".")
                                };

                        break;
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
