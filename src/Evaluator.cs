using System.Globalization;
using Wave.IO;
using Wave.Source.Binding;
using Wave.Source.Binding.BoundNodes;
using Wave.Symbols;

namespace Wave
{
    public class Evaluator
    {
        private readonly BoundProgram _program;
        private readonly Dictionary<VariableSymbol, object?> _globals;
        private ClassInstance? _currentInstance = null;
        private readonly Dictionary<FunctionSymbol, BoundBlockStmt> _functions = new();
        private readonly Dictionary<string, ClassSymbol> _classes;
        private readonly Stack<Dictionary<VariableSymbol, object?>> _locals = new();
        private readonly Random rnd = new();
        private object? _lastValue = null;
        public Evaluator(BoundProgram program, Dictionary<VariableSymbol, object?> variables)
        {
            _program = program;
            _globals = variables;
            _locals.Push(new Dictionary<VariableSymbol, object?>());
            BoundProgram? current = program;
            KeyValuePair<FunctionSymbol, BoundBlockStmt>? evalFn = current.Functions.Any(fn => fn.Key.Name == "$eval") ? current.Functions.Single(fn => fn.Key.Name == "$eval") : null;
            if (evalFn.HasValue)
                _functions.Add(evalFn.Value.Key, evalFn.Value.Value);

            while (current is not null)
            {
                foreach ((FunctionSymbol fn, BoundBlockStmt body) in current.Functions.Where(fn => fn.Key.Name != "$eval"))
                    _functions.Add(fn, body);
                current = current.Previous;
            }

            _classes = program.Classes.ToDictionary(c => c.Name, c => c);
        }

        public object? Evaluate()
        {
            FunctionSymbol? fn = _program.MainFn ?? _program.ScriptFn;
            if (fn is null)
                return null;
            return EvaluateStmt(_functions[fn]);
        }

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
                        return _lastValue = r.Value is null ? null : EvaluateExpr(r.Value);
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
                    if (u.Operand.Type.IsArray)
                    {
                        return u.Op.Kind switch
                        {
                            BoundUnOpKind.Plus => ((Array)v).Length,
                            _ => throw new Exception($"Unexpected unary operator \"{u.Op}\".")
                        };
                    }

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

                    if (b.Left.Type.IsArray)
                    {
                        List<object?> arr = ((object?[])left).ToList();
                        switch (b.Op.Kind)
                        {
                            case BoundBinOpKind.Plus:
                                arr.Add(right);
                                break;
                            default:
                                throw new Exception($"Unexpected binary operator \"{b.Op}\".");
                        };
                        return arr.ToArray();
                    }
                    else if (b.Right.Type.IsArray)
                    {
                        object?[] v = b.Op.Kind switch
                        {
                            BoundBinOpKind.Plus => ((object?[])right).Prepend(left).ToArray(),
                            _ => throw new Exception($"Unexpected binary operator \"{b.Op}\".")
                        };
                        return v;
                    }
                    else if (left is double lf)
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
                        else if (right is int ri)
                            return b.Op.Kind switch
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
                        else if (right is string rs)
                            return b.Op.Kind switch
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
                        else if (right is int ri)
                            return b.Op.Kind switch
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
                        else if (right is string rs)
                            return b.Op.Kind switch
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
                    return (n.Variable.Kind == SymbolKind.GlobalVariable
                            ? _globals
                            : _locals.Peek()).Single(v => v.Key.Name == n.Variable.Name).Value;
                case BoundAssignment a:
                    return (a.Variable.Kind == SymbolKind.GlobalVariable
                            ? _globals
                            : _locals.Peek())[a.Variable] = EvaluateExpr(a.Value);
                case BoundArrayAssignment a:
                {
                    object?[] oldV = (object?[])(a.Variable.Kind == SymbolKind.GlobalVariable
                           ? _globals
                           : _locals.Peek())[a.Variable]!;
                    int index = (int)EvaluateExpr(a.Index)!;
                    object? v = EvaluateExpr(a.Value);
                    oldV[index] = v;
                    (a.Variable.Kind == SymbolKind.GlobalVariable
                           ? _globals
                           : _locals.Peek())[a.Variable] = oldV;

                    return v;
                }
                case BoundCall c:
                {
                    if (c.Function == BuiltInFunctions.Input)
                        return Console.ReadLine()!;
                    else if (c.Function == BuiltInFunctions.PrintS || c.Function == BuiltInFunctions.PrintI || c.Function == BuiltInFunctions.PrintF || c.Function == BuiltInFunctions.PrintB)
                    {
                        Console.WriteLine(EvaluateExpr(c.Args[0]).Stringify());
                        return null;
                    }
                    else if (c.Function == BuiltInFunctions.PrintEmpty)
                    {
                        Console.WriteLine();
                        return null;
                    }
                    else if (c.Function == BuiltInFunctions.Clear)
                    {
                        Console.Clear();
                        return null;
                    }
                    else if (c.Function == BuiltInFunctions.Random)
                        return rnd.Next((int?)EvaluateExpr(c.Args[0]) ?? 1);
                    else if (c.Function == BuiltInFunctions.Range)
                        return Enumerable.Range((int?)EvaluateExpr(c.Args[0]) ?? 0, (int?)EvaluateExpr(c.Args[1]) ?? 1).ToArray();
                    else
                    {
                        Dictionary<VariableSymbol, object?> locals = new();
                        for (int i = 0; i < c.Args.Length; ++i)
                        {
                            ParameterSymbol parameter = c.Function.Parameters[i];
                            locals.Add(parameter, EvaluateExpr(c.Args[i]));
                        }

                        _locals.Push(locals);
                        object? res = EvaluateStmt(_functions[c.Function]);
                        _locals.Pop();
                        return res;
                    }

                    throw new Exception($"Unexpected function \"{c.Function.Name}\"");
                }
                case BoundArray a:
                    return a.Elements.Select(EvaluateExpr).ToArray();
                case BoundIndexing i:
                {
                    int index = (int)EvaluateExpr(i.Index)!;
                    try
                    {
                        return EvaluateExpr(i.Array) is not Array array ? null : Enumerable.Cast<object>(array).ElementAt(index);
                    }
                    catch (IndexOutOfRangeException)
                    {
                        RuntimeException($"Index Out Of Range: An element with the index \"{index}\" does not exist that array.");
                        break;
                    }
                }
                case BoundInstance i:
                {
                    ClassSymbol c = _classes[i.Name];
                    return new ClassInstance(i.Name,
                        c.Fns.Select(fn => new KeyValuePair<string, BoundBlockStmt>(fn.Key.Name, fn.Value)).ToDictionary(x => x.Key, x => x.Value),
                        c.Fields.Select(f => new KeyValuePair<string, object?>(f.Key.Name, EvaluateExpr(f.Value))).ToDictionary(x => x.Key, x => x.Value));
                }
                case BoundGet g:
                {
                    if (g.Id is not null)
                    {
                        ClassInstance instance;
                        if (g.Id.Kind == SymbolKind.GlobalVariable)
                            instance = (ClassInstance)_globals.Single(v => v.Key.Name == g.Id.Name).Value!;
                        else
                            instance = (ClassInstance)_locals.Peek().Single(v => v.Key.Name == g.Id.Name).Value!;
                        return instance.Fields[g.Field.Name];
                    }

                    return _currentInstance!.Fields[g.Field.Name];
                }
                case BoundMethod m:
                {
                    ClassInstance instance = m.Id is null ? _currentInstance! : (ClassInstance)(m.Id.Kind == SymbolKind.GlobalVariable
                            ? _globals
                            : _locals.Peek())[m.Id]!;
                    Dictionary<VariableSymbol, object?> locals = new();
                    for (int i = 0; i < m.Args.Length; ++i)
                    {
                        ParameterSymbol parameter = m.Function.Parameters[i];
                        locals.Add(parameter, EvaluateExpr(m.Args[i]));
                    }

                    _locals.Push(locals);
                    _currentInstance = instance;
                    object? res = EvaluateStmt(instance.Fns[m.Function.Name]);
                    _locals.Pop();
                    _currentInstance = null;
                    return res;
                }
                case BoundSet s:
                {
                    if (s.Id is not null)
                    {
                        ClassInstance oldV = (ClassInstance)(s.Id.Kind == SymbolKind.GlobalVariable
                            ? _globals
                            : _locals.Peek())[s.Id]!;
                        oldV.Fields[s.Field.Name] = EvaluateExpr(s.Value);
                        return (s.Id.Kind == SymbolKind.GlobalVariable
                                ? _globals
                                : _locals.Peek())[s.Id] = oldV;
                    }

                    return _currentInstance!.Fields[s.Field.Name] = EvaluateExpr(s.Value);
                }
                case BoundConversion c:
                {
                    object v = EvaluateExpr(c.Expr)!;
                    try
                    {
                        if (c.Type == TypeSymbol.Bool)
                            return Convert.ToBoolean(v);
                        else if (c.Type == TypeSymbol.Int)
                            return Convert.ToInt32(v);
                        else if (c.Type == TypeSymbol.Float)
                            return Convert.ToDouble(v, CultureInfo.InvariantCulture);
                        else if (c.Type == TypeSymbol.String)
                            return v.Stringify();
                        else
                            throw new Exception($"Unexpected type \"{c.Type}\".");
                    }
                    catch (InvalidCastException)
                    {
                        RuntimeException($"Invalid Cast: Could not cast \"{v.Stringify}\" to a type of \"{c.Type}\".");
                        break;
                    }
                }
            }

            throw new Exception($"Unexpected expression.");
        }

        private static void RuntimeException(string message)
        {
            Console.Out.SetForeground(ConsoleColor.DarkRed);
            Console.Out.WriteLine(message);
            Console.Out.ResetColor();
            Environment.Exit(1);
            throw new();
        }

        private static new bool Equals(object left, object right)
        {
            if (left == null || right == null)
                return false;

            return left.Equals(right);
        }
    }

    public class RuntimeException : Exception
    {
        public RuntimeException(string title, string message, TextLocation location) : base(message)
        {
            Title = title;
            Location = location;
        }

        public string Title { get; }
        public TextLocation Location { get; }
        public void Print()
        {
            Console.Out.WriteLine($"{Location.FileName}:{Location.StartLine}{Location.Span.Start}\n{Title}: {Message}");
        }
    }
}
