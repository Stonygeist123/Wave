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
        private readonly Dictionary<FieldSymbol, object?> _fields = new();
        private readonly Stack<Dictionary<VariableSymbol, object?>> _locals = new();
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
            foreach (ClassSymbol c in program.Classes)
            {
                foreach ((FieldSymbol f, BoundExpr expr) in c.Fields)
                    _fields.Add(f, EvaluateExpr(expr)!);
            }
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
                        if (!(v.Variable.Kind == SymbolKind.GlobalVariable
                              ? _globals
                              : _locals.Peek()).TryAdd(v.Variable, value))
                        {
                            (v.Variable.Kind == SymbolKind.GlobalVariable
                              ? _globals
                              : _locals.Peek()).Remove(v.Variable);
                            (v.Variable.Kind == SymbolKind.GlobalVariable
                              ? _globals
                              : _locals.Peek()).Add(v.Variable, value);
                        }

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

                    if (v is string s)
                        return u.Op.Kind switch
                        {
                            BoundUnOpKind.Plus => s.Length,
                            _ => throw new Exception($"Unexpected unary operator \"{u.Op}\".")
                        };
                    else
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
                    else if (b.Left.Type.IsADT)
                    {
                        object v = b.Op.Kind switch
                        {
                            BoundBinOpKind.EqEq => left == right,
                            BoundBinOpKind.NotEq => left != right,
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
                    if (c.Function.IsStd)
                    {
                        // IO
                        if ((NamespaceSymbol_Std)c.NamespaceSymbol! == StdLib.IO)
                            return StdLib.IO.Fns[c.Function](c.Args.Select(a => EvaluateExpr(a)!).ToArray());
                        // Math
                        if ((NamespaceSymbol_Std)c.NamespaceSymbol! == StdLib.Math)
                            return StdLib.Math.Fns[c.Function](c.Args.Select(a => EvaluateExpr(a)!).ToArray());
                    }
                    else
                    {
                        Dictionary<VariableSymbol, object?> locals = new();
                        for (int i = 0; i < c.Args.Length; ++i)
                        {
                            ParameterSymbol parameter = c.Function.Parameters[i];
                            locals.Add(parameter, EvaluateExpr(c.Args[i]));
                        }

                        _locals.Push(locals);
                        object? res = EvaluateStmt((c.NamespaceSymbol?.Fns ?? _functions)[c.Function]);
                        _locals.Pop();
                        return res;
                    }

                    throw new Exception($"Unexpected function \"{c.Function.Name}\"");
                }
                case BoundArray a:
                    return a.Elements.Select(EvaluateExpr).ToArray();
                case BoundEnumIndexing ei:
                {
                    object? index = EvaluateExpr(ei.Index)!;
                    return ei.ADT.Members.Keys.ElementAt(ei.ADT.Members.Values.ToList().FindIndex(v => EvaluateExpr(v) == index));
                }
                case BoundIndexing i:
                {
                    int index = (int)EvaluateExpr(i.Index)!;
                    try
                    {
                        object v = EvaluateExpr(i.Expr)!;
                        return i.Expr.Type.IsArray ? Enumerable.Cast<object>((Array)v).ElementAt(index) : (i.Expr.Type == TypeSymbol.String ? ((string)v)[index].ToString() : null);
                    }
                    catch (IndexOutOfRangeException)
                    {
                        RuntimeException($"Index Out Of Range: An element with the index \"{index}\" does not exist that array.");
                        break;
                    }
                }
                case BoundInstance i:
                {
                    ClassSymbol c = i.NamespaceSymbol?.Classes.SingleOrDefault(c => c.Name == i.Name) ?? _classes[i.Name];
                    IEnumerable<KeyValuePair<string, BoundBlockStmt>> fns = c.Fns.Select(fn => new KeyValuePair<string, BoundBlockStmt>(fn.Key.Name, fn.Value));
                    IEnumerable<KeyValuePair<string, object?>> fields = c.Fields.Select(f => new KeyValuePair<string, object?>(f.Key.Name, EvaluateExpr(f.Value)));
                    ClassInstance instance = new(i.Name,
                        fns.Any() ? fns.ToDictionary(x => x.Key, x => x.Value) : new(),
                        fields.Any() ? fields.ToDictionary(x => x.Key, x => x.Value) : new());
                    if (c.Ctor is not null)
                    {
                        Dictionary<VariableSymbol, object?> locals = new();
                        for (int j = 0; j < i.Args.Length; ++j)
                        {
                            ParameterSymbol parameter = c.Ctor.Value.Key.Parameters[j];
                            locals.Add(parameter, EvaluateExpr(i.Args[j]));
                        }

                        _locals.Push(locals);
                        ClassInstance? before = _currentInstance;
                        _currentInstance = instance;
                        EvaluateStmt(c.Ctor.Value.Value);
                        _currentInstance = before;
                        _locals.Pop();
                    }

                    return instance;
                }
                case BoundEnumGet eg:
                    return EvaluateExpr(eg.EnumSymbol.Members[eg.Member]);
                case BoundGet g:
                {
                    if (!g.HasInstance)
                        return _fields[g.Field];

                    if (g.NamespaceSymbol is not null)
                        return EvaluateExpr(g.NamespaceSymbol.Classes.Single(c => c == g.Field.ClassSymbol).Fields[g.Field]);

                    if (g.Id is not null)
                    {
                        if (!g.HasInstance)
                            return _fields[g.Field];

                        ClassInstance instance = g.Id.Kind == SymbolKind.GlobalVariable
                            ? (ClassInstance)_globals.Single(v => v.Key.Name == g.Id.Name).Value!
                            : (ClassInstance)_locals.Peek().Single(v => v.Key.Name == g.Id.Name).Value!;
                        return instance.Fields[g.Field.Name];
                    }

                    return _currentInstance!.Fields[g.Field.Name];
                }
                case BoundMethod m:
                {
                    if (m.Function.IsStatic && _currentInstance is null)
                    {
                        Dictionary<VariableSymbol, object?> locals = new();
                        for (int i = 0; i < m.Args.Length; ++i)
                        {
                            ParameterSymbol parameter = m.Function.Parameters[i];
                            locals.Add(parameter, EvaluateExpr(m.Args[i]));
                        }

                        _locals.Push(locals);
                        object? res = EvaluateStmt(_classes[m.Function.ClassName!].Fns.Single(fn => fn.Key.Name == m.Function.Name && fn.Key.Parameters.Select(p => p.Type).SequenceEqual(m.Function.Parameters.Select(p => p.Type))).Value);
                        _locals.Pop();
                        return res;
                    }
                    else
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

                        ClassInstance? before = _currentInstance;
                        _currentInstance = instance;
                        object? res = EvaluateStmt(instance.Fns[m.Function.Name]);
                        _currentInstance = before;

                        _locals.Pop();
                        return res;
                    }
                }
                case BoundSet s:
                {
                    object? v = EvaluateExpr(s.Value);
                    if (!s.HasInstance)
                        return _fields[s.Field] = v;

                    if (s.Id is null)
                        return _currentInstance!.Fields[s.Field.Name] = v;

                    ClassInstance oldV = (ClassInstance)(s.Id.Kind == SymbolKind.GlobalVariable
                        ? _globals
                        : _locals.Peek())[s.Id]!;
                    oldV.Fields[s.Field.Name] = v;
                    return (s.Id.Kind == SymbolKind.GlobalVariable
                            ? _globals
                            : _locals.Peek())[s.Id] = oldV;
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
