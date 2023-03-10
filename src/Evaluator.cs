using System.Collections.Immutable;
using System.Globalization;
using Wave.Source.Binding;
using Wave.Source.Binding.BoundNodes;
using Wave.Symbols;

namespace Wave
{
    internal class Evaluator
    {
        private readonly ImmutableDictionary<FunctionSymbol, BoundBlockStmt> _fnBodies;
        private readonly BoundBlockStmt _root;
        private readonly Dictionary<VariableSymbol, object?> _globals;
        private readonly Stack<Dictionary<VariableSymbol, object?>> _locals = new();
        private readonly Random rnd = new();
        private object? _lastValue = null;
        public Evaluator(ImmutableDictionary<FunctionSymbol, BoundBlockStmt> fnBodies, BoundBlockStmt root, Dictionary<VariableSymbol, object?> variables)
        {
            _fnBodies = fnBodies;
            _root = root;
            _globals = variables;
            _locals.Push(new Dictionary<VariableSymbol, object?>());
        }

        public object? Evaluate() => EvaluateStmt(_root);
        private object? EvaluateStmt(BoundBlockStmt stmt)
        {
            Dictionary<LabelSymbol, int> labelToIndex = new();
            for (int i = 0; i < stmt.Stmts.Length; ++i)
                if (stmt.Stmts[i] is BoundLabelStmt l)
                    labelToIndex.Add(l.Label, i + 1);

            int index = 0;
            while (index < stmt.Stmts.Length)
            {
                BoundStmt s = stmt.Stmts[index];
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
                            object? value = EvaluateExpr(v.Value);
                            (v.Variable.Kind == SymbolKind.GlobalVariable
                            ? _globals
                            : _locals.Peek()).Add(v.Variable, value);

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
                            bool condition = (bool)EvaluateExpr(cg.Condition)!;
                            if (condition == cg.JumpIfTrue)
                                index = labelToIndex[cg.Label];
                            else
                                ++index;
                            break;
                        }
                    case BoundRetStmt r:
                        {
                            return _lastValue = r.Value is null ? null : EvaluateExpr(r.Value);
                        }
                }
            }

            return _lastValue;
        }

        private object? EvaluateExpr(BoundExpr expr)
        {
            switch (expr)
            {
                case BoundLiteral l:
                    return l.Value;
                case BoundUnary u:
                    {
                        object v = EvaluateExpr(u.Operand)!;
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
                        object left = EvaluateExpr(b.Left)!;
                        object right = EvaluateExpr(b.Right)!;

                        if (left is not null && right is not null)
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
                                else if (right is string rs) return b.Op.Kind switch
                                {
                                    BoundBinOpKind.Plus => lf.ToString(CultureInfo.InvariantCulture) + rs,
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
                                else if (right is string rs) return b.Op.Kind switch
                                {
                                    BoundBinOpKind.Plus => li + rs,
                                    _ => throw new Exception($"Unexpected binary operator \"{b.Op}\".")
                                };
                            }
                            else if (left is bool lb)
                            {
                                if (right is bool rb)
                                    return b.Op.Kind switch
                                    {
                                        BoundBinOpKind.LogicAnd => lb && rb,
                                        BoundBinOpKind.LogicOr => lb || rb,
                                        BoundBinOpKind.EqEq => Equals(lb, rb),
                                        BoundBinOpKind.NotEq => !Equals(lb, rb),
                                        _ => throw new Exception($"Unexpected binary operator \"{b.Op}\".")
                                    };
                            }
                            else if (left is string ls)
                            {
                                if (right is string rs)
                                    return b.Op.Kind switch
                                    {
                                        BoundBinOpKind.Plus => ls + rs,
                                        BoundBinOpKind.EqEq => ls == rs,
                                        BoundBinOpKind.NotEq => ls != rs,
                                        _ => throw new Exception($"Unexpected binary operator \"{b.Op}\".")
                                    };
                                else if (right is int ri)
                                    return b.Op.Kind switch
                                    {
                                        BoundBinOpKind.Plus => ls + ri,
                                        BoundBinOpKind.Minus => ri < 0 ? ls + Math.Abs(ri) : ri > ls.Length ? (ri > ls.Length * 2 ? "" : ls[(ri - ls.Length)..]) : ls[..^ri],
                                        BoundBinOpKind.Star => string.Concat(Enumerable.Repeat(ls, ri)),
                                        _ => throw new Exception($"Unexpected binary operator \"{b.Op}\".")
                                    };
                                else if (right is double rf)
                                    return b.Op.Kind switch
                                    {
                                        BoundBinOpKind.Plus => ls + rf.ToString(CultureInfo.InvariantCulture),
                                        _ => throw new Exception($"Unexpected binary operator \"{b.Op}\".")
                                    };
                            }

                        break;
                    }
                case BoundName n:
                    return n.Variable.Kind == SymbolKind.GlobalVariable
                            ? _globals[n.Variable]
                            : _locals.Peek()[n.Variable];
                case BoundAssignment a:
                    return (a.Variable.Kind == SymbolKind.GlobalVariable
                            ? _globals
                            : _locals.Peek())[a.Variable] = EvaluateExpr(a.Value);
                case BoundCall c:
                    {
                        if (c.Function == BuiltInFunctions.Input) return Console.ReadLine()!;
                        else if (c.Function == BuiltInFunctions.Print)
                        {
                            Console.WriteLine(EvaluateExpr(c.Args[0]));
                            return null;
                        }
                        else if (c.Function == BuiltInFunctions.Random)
                            return rnd.Next((int?)EvaluateExpr(c.Args[0]) ?? 1);
                        else
                        {
                            Dictionary<VariableSymbol, object?> locals = new();
                            for (int i = 0; i < c.Args.Length; ++i)
                            {
                                ParameterSymbol parameter = c.Function.Parameters[i];
                                locals.Add(parameter, EvaluateExpr(c.Args[i]));
                            }

                            _locals.Push(locals);
                            object? res = EvaluateStmt(_fnBodies[c.Function]);
                            _locals.Pop();
                            return res;
                        }

                        throw new Exception($"Unexpected function \"{c.Function.Name}\"");
                    }
                case BoundConversion c:
                    {
                        object? v = EvaluateExpr(c.Expr);
                        if (c.Type == TypeSymbol.Bool)
                            return Convert.ToBoolean(v);
                        if (c.Type == TypeSymbol.Int)
                            return Convert.ToInt32(v);
                        if (c.Type == TypeSymbol.Float)
                        {
                            return Convert.ToDouble(v, CultureInfo.InvariantCulture);
                        }
                        if (c.Type == TypeSymbol.String)
                            return Convert.ToString(v);
                        throw new Exception($"Unexpected type \"{c.Type}\".");
                    }
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
