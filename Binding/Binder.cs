using System.Collections.Immutable;
using Wave.Binding.BoundNodes;
using Wave.Lowering;
using Wave.Nodes;
using Wave.Symbols;
using Wave.Syntax.Nodes;

namespace Wave.Binding
{
    internal class Binder
    {
        private readonly FunctionSymbol? _fn;
        private readonly DiagnosticBag _diagnostics = new();
        private BoundScope _scope;
        public DiagnosticBag Diagnostics => _diagnostics;
        private readonly Stack<(LabelSymbol BreakLabel, LabelSymbol ContinueLabel)> _loopStack = new();
        private int _labelCounter = 0;
        public Binder(BoundScope? parent, FunctionSymbol? fn)
        {
            _scope = new(parent);
            _fn = fn;

            if (fn is not null)
                foreach (ParameterSymbol param in fn.Parameters)
                    _scope.TryDeclareVar(param);
        }

        public static BoundGlobalScope BindGlobalScope(BoundGlobalScope? previous, CompilationUnit syntax)
        {
            BoundScope parentScope = CreateParentScope(previous);
            Binder binder = new(parentScope, null);
            foreach (FnDeclStmt fnDecl in syntax.Members.OfType<FnDeclStmt>())
                binder.BindFnDecl(fnDecl);

            ImmutableArray<BoundStmt>.Builder stmts = ImmutableArray.CreateBuilder<BoundStmt>();
            foreach (GlobalStmt globalStmt in syntax.Members.OfType<GlobalStmt>())
                stmts.Add(binder.BindStmt(globalStmt.Stmt));

            ImmutableArray<FunctionSymbol> functions = binder._scope.GetDeclaredFns();
            ImmutableArray<VariableSymbol> variables = binder._scope.GetDeclaredVars();
            ImmutableArray<Diagnostic> diagnostics = binder.Diagnostics.ToImmutableArray();
            return new(previous, diagnostics, variables, functions, new BoundBlockStmt(stmts.ToImmutable()));
        }

        public static BoundProgram BindProgram(BoundGlobalScope globalScope)
        {
            BoundScope parentScope = CreateParentScope(globalScope);
            ImmutableDictionary<FunctionSymbol, BoundBlockStmt>.Builder fnBodies = ImmutableDictionary.CreateBuilder<FunctionSymbol, BoundBlockStmt>();
            DiagnosticBag diagnostics = new();

            BoundGlobalScope? scope = globalScope;
            while (scope is not null)
            {
                foreach (FunctionSymbol fn in scope.Functions)
                {
                    Binder binder = new(parentScope, fn);
                    BoundBlockStmt body = Lowerer.Lower(binder.BindStmt(fn.Decl!.Body));
                    fnBodies.Add(fn, body);
                    diagnostics.AddRange(binder.Diagnostics);
                }

                scope = scope.Previous;
            }

            return new(Lowerer.Lower(new BoundBlockStmt(ImmutableArray.Create<BoundStmt>(globalScope.Stmt))), diagnostics.ToImmutableArray(), fnBodies.ToImmutable());
        }

        private void BindFnDecl(FnDeclStmt decl)
        {
            ImmutableArray<ParameterSymbol>.Builder parameters = ImmutableArray.CreateBuilder<ParameterSymbol>();
            if (decl.Parameters is not null)
            {
                HashSet<string> seenParameters = new();

                foreach (ParameterNode param in decl.Parameters.Parameters)
                {
                    string pName = param.Id.Lexeme;
                    if (!seenParameters.Add(pName))
                    {
                        _diagnostics.Report(param.Id.Span, $"Parameter \"{pName}\" was already declared.");
                    }
                    else
                    {
                        TypeSymbol? pType = BindTypeClause(param.Type);
                        if (pType is null)
                            _diagnostics.Report(param.Type.Id.Span, $"Expected type.");
                        else
                            parameters.Add(new ParameterSymbol(pName, pType));
                    }
                }
            }

            TypeSymbol type = BindTypeClause(decl.TypeClause) ?? TypeSymbol.Void;
            Token name = decl.Name;
            FunctionSymbol fn = new(name.Lexeme, parameters.ToImmutable(), type, decl);
            if (!_scope.TryDeclareFn(fn))
            {
                _diagnostics.Report(name.Span, $"Function \"{name.Lexeme}\" was already declared.");
            }
        }

        private static BoundScope CreateParentScope(BoundGlobalScope? previous)
        {
            Stack<BoundGlobalScope> stack = new();
            while (previous is not null)
            {
                stack.Push(previous);
                previous = previous.Previous;
            }

            BoundScope parent = GetRootScope();
            while (stack.Count > 0)
            {
                previous = stack.Pop();
                BoundScope scope = new(parent);
                foreach (FunctionSymbol f in previous.Functions)
                    scope.TryDeclareFn(f);

                foreach (VariableSymbol v in previous.Variables)
                    scope.TryDeclareVar(v);

                parent = scope;
            }

            return parent;
        }

        private static BoundScope GetRootScope()
        {
            BoundScope res = new(null);
            foreach (FunctionSymbol fn in BuiltInFunctions.GetAll())
                res.TryDeclareFn(fn);

            return res;
        }

        public BoundStmt BindStmt(StmtNode stmt)
        {
            return stmt switch
            {
                ExpressionStmt e => BindExpressionStmt(e),
                BlockStmt b => BindBlockStmt(b),
                VarStmt v => BindVarStmt(v),
                IfStmt i => BindIfStmt(i),
                WhileStmt w => BindWhileStmt(w),
                DoWhileStmt d => BindDoWhileStmt(d),
                ForStmt f => BindForStmt(f),
                BreakStmt b => BindBreakStmt(b),
                ContinueStmt c => BindContinueStmt(c),
                RetStmt r => BindRetStmt(r),
                _ => throw new Exception("Unexpected syntax."),
            };
        }

        private BoundExpressionStmt BindExpressionStmt(ExpressionStmt e) => new(BindExpr(e.Expr, canBeVoid: true));
        private BoundBlockStmt BindBlockStmt(BlockStmt b)
        {
            ImmutableArray<BoundStmt>.Builder stmts = ImmutableArray.CreateBuilder<BoundStmt>();
            _scope = new(_scope);
            foreach (StmtNode stmt in b.Stmts)
                stmts.Add(BindStmt(stmt));

            _scope = _scope.Parent!;
            return new(stmts.ToImmutable());
        }

        private BoundVarStmt BindVarStmt(VarStmt v)
        {
            bool isMut = v.MutKeyword is not null;
            BoundExpr value = BindExpr(v.Value);
            TypeSymbol type = BindTypeClause(v.TypeClause) ?? value.Type;
            value = BindConversion(value, type, v.Value.Span);
            return new(DeclareVar(v.Name, type, isMut), value);
        }

        private TypeSymbol? BindTypeClause(TypeClause? typeClause)
        {
            if (typeClause is null)
                return null;

            if (LookupType(typeClause.Id.Lexeme) is TypeSymbol t)
                return t;

            _diagnostics.Report(typeClause.Id.Span, $"Undefined type \"{typeClause.Id.Lexeme}\".");
            return null;
        }

        private BoundIfStmt BindIfStmt(IfStmt i)
        {
            BoundExpr condition = BindExpr(i.Condition, TypeSymbol.Bool);
            if (condition.Type != TypeSymbol.Bool)
                _diagnostics.Report(i.Condition.Span, $"Condition needs to be a bool.");

            BoundStmt thenBranch = BindStmt(i.ThenBranch);
            BoundStmt? elseClause = i.ElseClause is not null ? BindStmt(i.ElseClause.Stmt) : null;
            return new(condition, thenBranch, elseClause);
        }

        private BoundWhileStmt BindWhileStmt(WhileStmt w)
        {
            BoundExpr condition = BindExpr(w.Condition, TypeSymbol.Bool);
            BoundStmt stmt = BoundLoopStmt(w.Stmt, out LabelSymbol bodyLabel, out LabelSymbol breakLabel, out LabelSymbol continueLabel);
            return new(condition, stmt, bodyLabel, breakLabel, continueLabel);
        }

        private BoundDoWhileStmt BindDoWhileStmt(DoWhileStmt d)
        {
            BoundStmt stmt = BoundLoopStmt(d.Stmt, out LabelSymbol bodyLabel, out LabelSymbol breakLabel, out LabelSymbol continueLabel);
            BoundExpr condition = BindExpr(d.Condition, TypeSymbol.Bool);
            return new(stmt, condition, bodyLabel, breakLabel, continueLabel);
        }

        private BoundForStmt BindForStmt(ForStmt f)
        {
            BoundExpr lowerBound = BindExpr(f.LowerBound, TypeSymbol.Int);
            BoundExpr upperBound = BindExpr(f.UpperBound, TypeSymbol.Int);

            _scope = new(_scope);
            VariableSymbol variable = DeclareVar(f.Id, TypeSymbol.Int);
            BoundStmt stmt = BoundLoopStmt(f.Stmt, out LabelSymbol bodyLabel, out LabelSymbol breakLabel, out LabelSymbol continueLabel);
            _scope = _scope.Parent!;
            return new(variable, lowerBound, upperBound, stmt, bodyLabel, breakLabel, continueLabel);
        }

        private BoundStmt BoundLoopStmt(StmtNode stmt, out LabelSymbol bodyLabel, out LabelSymbol breakLabel, out LabelSymbol continueLabel)
        {
            bodyLabel = new($"LoopBody_{++_labelCounter}");
            breakLabel = new($"Break_{++_labelCounter}");
            continueLabel = new($"Continue_{_labelCounter}");


            _loopStack.Push((bodyLabel, continueLabel));
            BoundStmt boundStmt = BindStmt(stmt);
            _loopStack.Pop();
            return boundStmt;
        }

        private BoundStmt BindBreakStmt(BreakStmt b)
        {
            if (!_loopStack.Any())
            {
                _diagnostics.Report(b.Span, "Can only break inside of a loop.");
                return new BoundErrorStmt();
            }

            return new BoundGotoStmt(_loopStack.Peek().BreakLabel);
        }

        private BoundStmt BindContinueStmt(ContinueStmt c)
        {
            if (!_loopStack.Any())
            {
                _diagnostics.Report(c.Span, "Can only continue inside of a loop.");
                return new BoundErrorStmt();
            }

            return new BoundGotoStmt(_loopStack.Peek().ContinueLabel);
        }

        private BoundStmt BindRetStmt(RetStmt r)
        {
            BoundExpr? value = r.Value is null ? null : BindExprInternal(r.Value);
            if (_fn is null)
                _diagnostics.Report(r.Span, "Cannot return outside of a function.");
            else
            {
                if (_fn.Type == TypeSymbol.Void)
                {
                    if (value is not null)
                        _diagnostics.Report(r.Value!.Span, "Cannot return from a void function.");
                }
                else
                {
                    if (value is null)
                        _diagnostics.Report(r.Span, $"Expected a value of type \"{_fn.Type}\" to be returned.");
                    else
                        value = BindConversion(r.Value!, _fn.Type, false, $"Return type must match the type of function \"{_fn.Name}\"; expected \"{_fn.Type}\" - got \"{value.Type}\".");
                }
            }

            return new BoundRetStmt(value);
        }

        private BoundExpr BindExpr(ExprNode expr, TypeSymbol type) => BindConversion(expr, type);
        private BoundExpr BindExpr(ExprNode expr, bool canBeVoid = false)
        {
            BoundExpr boundExpr = BindExprInternal(expr);
            if (!canBeVoid && boundExpr.Type == TypeSymbol.Void)
            {
                _diagnostics.Report(expr.Span, $"Expression must have a value.");
                return new BoundError();
            }

            return boundExpr;
        }

        private BoundExpr BindExprInternal(ExprNode expr)
        {
            return expr switch
            {
                LiteralExpr l => new BoundLiteral(l.Value),
                UnaryExpr u => BindUnaryExpr(u),
                BinaryExpr b => BindBinaryExpr(b),
                GroupingExpr b => BindExprInternal(b.Expr),
                NameExpr n => BindNameExpr(n),
                AssignmentExpr a => BindAssignmentExpr(a),
                CallExpr c => BindCallExpr(c),
                _ => throw new Exception("Unexpected syntax."),
            };
        }

        private BoundExpr BindUnaryExpr(UnaryExpr u)
        {
            BoundExpr operand = BindExprInternal(u.Operand);
            if (operand.Type == TypeSymbol.Unknown)
                return new BoundError();

            BoundUnOperator? op = BoundUnOperator.Bind(u.Op.Kind, operand.Type);
            if (op is null)
            {
                _diagnostics.Report(u.Op.Span, $"Unary operator \"{u.Op.Lexeme}\" is not defined for type \"{operand.Type}\".");
                return new BoundError();
            }

            return new BoundUnary(op, operand);
        }

        private BoundExpr BindBinaryExpr(BinaryExpr b)
        {
            BoundExpr left = BindExprInternal(b.Left);
            BoundExpr right = BindExprInternal(b.Right);
            BoundBinOperator? op = BoundBinOperator.Bind(b.Op.Kind, left.Type, right.Type);

            if (left.Type == TypeSymbol.Unknown || right.Type == TypeSymbol.Unknown)
                return new BoundError();

            if (op is null)
            {
                _diagnostics.Report(b.Op.Span, $"Binary operator \"{b.Op.Lexeme}\" is not defined for types \"{left.Type}\" and \"{right.Type}\".");
                return new BoundError();
            }

            return new BoundBinary(left, op, right);
        }

        private BoundExpr BindNameExpr(NameExpr n)
        {
            string name = n.Identifier.Lexeme;
            if (n.Identifier.IsMissing)
                return new BoundError();

            if (!_scope.TryLookupVar(name, out VariableSymbol? variable))
            {
                _diagnostics.Report(n.Identifier.Span, $"Could not find \"{name}\".");
                return new BoundError();
            }

            return new BoundName(variable!);
        }

        private BoundExpr BindAssignmentExpr(AssignmentExpr a)
        {
            string name = a.Identifier.Lexeme;
            if (!_scope.TryLookupVar(name, out VariableSymbol? variable))
            {
                _diagnostics.Report(a.Identifier.Span, $"Could not find \"{name}\".");
                return new BoundError();
            }

            if (!variable!.IsMut)
                _diagnostics.Report(a.EqToken.Span, $"Cannot assign to \"{name}\" - it is a constant.");

            return new BoundAssignment(variable, BindConversion(BindExpr(a.Value), variable.Type, a.Span));
        }

        private BoundExpr BindCallExpr(CallExpr c)
        {
            if (c.Args.Count == 1 && LookupType(c.Callee.Lexeme) is TypeSymbol type)
                return BindConversion(c.Args[0], type, true);

            string name = c.Callee.Lexeme;
            if (!_scope.TryLookupFn(name, out FunctionSymbol? fn) && fn is null)
            {
                _diagnostics.Report(c.Callee.Span, $"Could not find function \"{name}\".");
                return new BoundError();
            }

            if (c.Args.Count > 0 && fn!.Parameters.Length == 0)
            {
                ImmutableArray<Node> nodes = c.Args.GetWithSeps();
                _diagnostics.Report(TextSpan.From(nodes.First().Span.Start, nodes.Last().Span.End), $"Wrong number of arguments; expected none - got \"{c.Args.Count}\".");
                return new BoundError();
            }

            if (c.Args.Count > fn!.Parameters.Length)
            {
                ImmutableArray<Node> nodes = c.Args.GetWithSeps();
                _diagnostics.Report(TextSpan.From(nodes.First(a => a.Span.Start != nodes[fn.Parameters.Length - 1].Span.Start).Span.Start, c.Args.Last().Span.End), $"Wrong number of arguments; expected \"{fn.Parameters.Length}\" - got \"{c.Args.Count}\".");
                return new BoundError();
            }

            if (c.Args.Count == 0 && fn.Parameters.Length > 0)
            {
                _diagnostics.Report(TextSpan.From(c.LParen.Span.Start, c.RParen.Span.End), $"Wrong number of arguments; expected \"{fn.Parameters.Length}\" - got none.");
                return new BoundError();
            }

            if (c.Args.Count < fn.Parameters.Length)
            {
                _diagnostics.Report(TextSpan.From(c.Args.First().Span.Start, c.Args.Last().Span.End), $"Wrong number of arguments; expected \"{fn.Parameters.Length}\" - got \"{c.Args.Count}\".");
                return new BoundError();
            }

            ImmutableArray<BoundExpr>.Builder args = ImmutableArray.CreateBuilder<BoundExpr>();
            for (int i = 0; i < fn.Parameters.Length; ++i)
            {
                ParameterSymbol param = fn.Parameters[i];
                BoundExpr arg = BindExpr(c.Args[i]);
                args.Add(BindConversion(c.Args[i], param.Type, false, $"Parameter \"{param.Name}\" has a type of \"{param.Type}\" - got a value with type of \"{arg.Type}\" as argument."));
            }

            return new BoundCall(fn, args.ToImmutable());
        }

        private BoundExpr BindConversion(ExprNode expr, TypeSymbol type, bool allowExplicit = false, string? errorImplicit = null, string? errorExists = null) => BindConversion(BindExpr(expr), type, expr.Span, allowExplicit, errorImplicit, errorExists);
        private BoundExpr BindConversion(BoundExpr expr, TypeSymbol type, TextSpan span, bool allowExplicit = false, string? errorImplicit = null, string? errorExists = null)
        {
            Conversion conversion = Conversion.Classify(expr.Type, type);
            if (!conversion.Exists)
            {
                if (expr.Type != TypeSymbol.Unknown && type != TypeSymbol.Unknown)
                    _diagnostics.Report(span, errorExists is null ? $"No conversion from \"{expr.Type}\" to \"{type}\" possible." : errorExists);
                return new BoundError();
            }

            if (!allowExplicit && conversion.IsExplicit)
                _diagnostics.Report(span, errorImplicit is null ? $"No implicit conversion from \"{expr.Type}\" to \"{type}\" possible; though an explicit cast is." : errorImplicit);

            if (conversion.IsIdentity)
                return expr;
            return new BoundConversion(type, expr);
        }

        private VariableSymbol DeclareVar(Token id, TypeSymbol type, bool isMut = false)
        {
            VariableSymbol variable = _fn is null ? new GlobalVariableSymbol(id.Lexeme, type, isMut) : new LocalVariableSymbol(id.Lexeme, type, isMut);
            if (!id.IsMissing && !_scope.TryDeclareVar(variable) || _scope.TryLookupFn(variable.Name, out _))
                _diagnostics.Report(id.Span, $"\"{id.Lexeme}\" already exists.");

            return variable;
        }

        private static TypeSymbol? LookupType(string name) => name switch
        {
            "bool" => TypeSymbol.Bool,
            "int" => TypeSymbol.Int,
            "float" => TypeSymbol.Float,
            "string" => TypeSymbol.String,
            _ => null,
        };
    }
}
