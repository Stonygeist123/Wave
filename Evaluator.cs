using Wave.Binding;
using Wave.Binding.BoundNodes;

namespace Wave
{
    internal class Evaluator
    {
        private readonly BoundBlockStmt _root;
        private readonly Dictionary<VariableSymbol, object?> _variables;
        private object? _lastValue = null;
        public Evaluator(BoundBlockStmt root, Dictionary<VariableSymbol, object?> variables)
        {
            _root = root;
            _variables = variables;
        }

        public object? Evaluate()
        {
            Dictionary<LabelSymbol, int> labelToIndex = new();
            for (int i = 0; i < _root.Stmts.Length; ++i)
                if (_root.Stmts[i] is BoundLabelStmt l)
                    labelToIndex.Add(l.Label, i + 1);

            int index = 0;
            while (index < _root.Stmts.Length)
            {
                BoundStmt s = _root.Stmts[index];

                switch (s)
                {
                    case BoundExpressionStmt e:
                        {
                            _lastValue = EvaluateExpr(e.Expr);
                            ++index;
                            break;
                        }
                    case BoundVarStmt v:
                        {
                            object value = EvaluateExpr(v.Value);
                            _variables[v.Variable] = value;
                            _lastValue = value;
                            ++index;
                            break;
                        }
                    case BoundLabelStmt:
                        {
                            ++index;
                            break;
                        }
                    case BoundGotoStmt g:
                        {
                            index = labelToIndex[g.Label];
                            break;
                        }
                    case BoundCondGotoStmt cg:
                        {
                            bool condition = (bool)EvaluateExpr(cg.Condition);
                            if (condition == cg.JumpIfTrue)
                                index = labelToIndex[cg.Label];
                            else
                                ++index;
                            break;
                        }
                }
            }

            return _lastValue;
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
