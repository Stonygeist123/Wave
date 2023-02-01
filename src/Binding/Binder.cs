using System.Collections.Immutable;
using Wave.Lowering;
using Wave.Source.Binding;
using Wave.Source.Binding.BoundNodes;
using Wave.Source.Syntax;
using Wave.Source.Syntax.Nodes;
using Wave.Symbols;
using static Wave.Source.Binding.ControlFlowGraph;

namespace Wave.src.Binding.BoundNodes
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
                    _scope.TryDeclareVar(param, true);
        }

        public static BoundGlobalScope BindGlobalScope(BoundGlobalScope? previous, ImmutableArray<SyntaxTree> syntaxTrees)
        {
            BoundScope parentScope = CreateParentScope(previous);
            Binder binder = new(parentScope, null);
            IEnumerable<FnDeclStmt> fnDecls = syntaxTrees.SelectMany(s => s.Root.Members).OfType<FnDeclStmt>();
            foreach (FnDeclStmt fnDecl in fnDecls)
                binder.BindFnDecl(fnDecl);

            ImmutableArray<BoundStmt>.Builder stmts = ImmutableArray.CreateBuilder<BoundStmt>();
            IEnumerable<GlobalStmt> globalStmts = syntaxTrees.SelectMany(s => s.Root.Members).OfType<GlobalStmt>();
            foreach (GlobalStmt globalStmt in globalStmts)
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

                    if (fn.Type != TypeSymbol.Void && !AllPathsReturn(body) && fn.Decl!.Body.Kind != SyntaxKind.ExpressionStmt)
                        binder._diagnostics.Report(fn.Decl.Name.Location, $"All code paths must return a value.");

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
                        _diagnostics.Report(param.Id.Location, $"Parameter \"{pName}\" was already declared.");
                    else
                    {
                        TypeSymbol? pType = BindTypeClause(param.Type);
                        if (pType is null)
                            _diagnostics.Report(param.Type.Id.Location, $"Expected type.", "Grammar: \": <type>\".");
                        else
                            parameters.Add(new ParameterSymbol(pName, pType));
                    }
                }
            }

            BoundExpr? boundExpr = null;
            if (decl!.Body.Kind == SyntaxKind.ExpressionStmt)
            {
                _scope = new(_scope);
                foreach (ParameterSymbol parameter in parameters.ToImmutable())
                    _scope.TryDeclareVar(parameter);

                boundExpr = BindExpr(((ExpressionStmt)decl!.Body).Expr);
                _scope = _scope.Parent!;
            }

            TypeSymbol fnType = BindTypeClause(decl.TypeClause) ?? boundExpr?.Type ?? TypeSymbol.Void;
            if (decl.TypeClause is null && fnType == TypeSymbol.Void)
            {
                Binder binder = new(null, new(decl.Name.Lexeme, parameters.ToImmutable(), fnType));
                ControlFlowGraph graph = CreateGraph(Lowerer.Lower(binder.BindStmt(decl.Body!)));
                BasicBlockBranch? branch = graph.Start.Outgoing.FirstOrDefault(b => b.To.Stmts.Any(s => s.Kind == BoundNodeKind.RetStmt && ((BoundRetStmt)s).Value is not null));
                if (branch is not null)
                    fnType = ((BoundRetStmt)branch.To.Stmts.First(s => s.Kind == BoundNodeKind.RetStmt)).Value!.Type;
            }

            if (boundExpr is not null && !Conversion.Classify(boundExpr.Type, fnType).IsImplicit)
                _diagnostics.Report(decl!.Body.Location, $"Expected a value with type of \"{fnType}\" - got \"{boundExpr.Type}\".");

            Token name = decl.Name;
            FunctionSymbol fn = new(name.Lexeme, parameters.ToImmutable(), fnType, decl);
            if (!_scope.TryDeclareFn(fn))
                _diagnostics.Report(name.Location, $"Function \"{name.Lexeme}\" was already declared.");
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
            value = BindConversion(value, type, v.Value.Location);
            return new(DeclareVar(v.Name, type, isMut), value);
        }

        private TypeSymbol? BindTypeClause(TypeClause? typeClause)
        {
            if (typeClause is null)
                return null;

            if (LookupType(typeClause.Id.Lexeme) is TypeSymbol t)
                return t;

            _diagnostics.Report(typeClause.Id.Location, $"Undefined type \"{typeClause.Id.Lexeme}\".");
            return null;
        }

        private BoundIfStmt BindIfStmt(IfStmt i)
        {
            BoundExpr condition = BindExpr(i.Condition, TypeSymbol.Bool);
            if (condition.Type != TypeSymbol.Bool)
                _diagnostics.Report(i.Condition.Location, $"Condition needs to be a bool.");

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
            breakLabel = new($"Break_{_labelCounter}");
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
                _diagnostics.Report(b.Location, "Can only break inside of a loop.");
                return new BoundErrorStmt();
            }

            return new BoundGotoStmt(_loopStack.Peek().BreakLabel);
        }

        private BoundStmt BindContinueStmt(ContinueStmt c)
        {
            if (!_loopStack.Any())
            {
                _diagnostics.Report(c.Location, "Can only continue inside of a loop.");
                return new BoundErrorStmt();
            }

            return new BoundGotoStmt(_loopStack.Peek().ContinueLabel);
        }

        private BoundStmt BindRetStmt(RetStmt r)
        {
            BoundExpr? value = r.Value is null ? null : BindExprInternal(r.Value);
            if (_fn is null)
                _diagnostics.Report(r.Location, "Cannot return outside of a function.");
            else
            {
                if (_fn.Type == TypeSymbol.Void)
                {
                    if (value is not null)
                        _diagnostics.Report(r.Value!.Location, "Cannot return from a void function.");
                }
                else
                {
                    if (value is null)
                        _diagnostics.Report(r.Location, $"Expected a value of type \"{_fn.Type}\" to be returned - got none.");
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
                _diagnostics.Report(expr.Location, $"Expression must have a value.");
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
                _diagnostics.Report(u.Op.Location, $"Unary operator \"{u.Op.Lexeme}\" is not defined for type \"{operand.Type}\".");
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
                _diagnostics.Report(b.Op.Location, $"Binary operator \"{b.Op.Lexeme}\" is not defined for types \"{left.Type}\" and \"{right.Type}\".");
                return new BoundError();
            }

            return new BoundBinary(left, op, right);
        }

        static string FindBestMatch(string stringToCompare, IEnumerable<string> strs)
        {

            HashSet<string> strCompareHash = stringToCompare.Split(' ').ToHashSet();

            int maxIntersectCount = 0;
            string bestMatch = string.Empty;

            foreach (string str in strs)
            {
                HashSet<string> strHash = str.Split(' ').ToHashSet();

                int intersectCount = strCompareHash.Intersect(strCompareHash).Count();
                if (intersectCount > maxIntersectCount)
                {
                    maxIntersectCount = intersectCount;
                    bestMatch = str;
                }
            }

            return bestMatch;
        }

        private BoundExpr BindNameExpr(NameExpr n)
        {
            string name = n.Identifier.Lexeme;
            if (n.Identifier.IsMissing)
                return new BoundError();

            if (_fn is null)
            {
                if (!_scope.TryLookupVar(name, out VariableSymbol? variable))
                {
                    string bestMatch = FindBestMatch(name, _scope.GetDeclaredVars().Select(v => v.Name));
                    _diagnostics.Report(n.Identifier.Location, $"Could not find \"{name}\".", bestMatch != string.Empty ? $"Did you mean \"{bestMatch}\"." : null);
                    return new BoundError();
                }

                return new BoundName(variable!);
            }
            else
            {
                if (!_scope.TryLookupVar(name, out VariableSymbol? variable))
                {
                    string bestMatch = FindBestMatch(name, _scope.GetDeclaredVars().Select(v => v.Name));
                    _diagnostics.Report(n.Identifier.Location, $"Could not find \"{name}\".", bestMatch != string.Empty ? $"Did you mean \"{bestMatch}\"." : null);
                    return new BoundError();
                }

                return new BoundName(variable!);
            }
        }

        private BoundExpr BindAssignmentExpr(AssignmentExpr a)
        {
            Token id = a.Identifier;
            if (!_scope.TryLookupVar(id.Lexeme, out VariableSymbol? variable))
            {
                string bestMatch = FindBestMatch(id.Lexeme, _scope.GetDeclaredVars().Select(v => v.Name));
                _diagnostics.Report(id.Location, $"Could not find \"{id.Lexeme}\".", bestMatch != string.Empty ? $"Did you mean \"{bestMatch}\"." : null);
                return new BoundError();
            }

            if (!variable!.IsMut)
                _diagnostics.Report(a.Location, $"Cannot assign to \"{id.Lexeme}\" - it is a constant.");

            return new BoundAssignment(variable, BindConversion(BindExpr(a.Value), variable.Type, a.Location));
        }

        private BoundExpr BindCallExpr(CallExpr c)
        {
            if (c.Args.Count == 1 && LookupType(c.Callee.Lexeme) is TypeSymbol type)
                return BindConversion(c.Args[0], type, true);

            string name = c.Callee.Lexeme;
            if (!_scope.TryLookupFn(name, out FunctionSymbol? fn) && fn is null)
            {
                string bestMatch = FindBestMatch(name, _scope.GetDeclaredFns().Select(f => f.Name));
                _diagnostics.Report(c.Callee.Location, $"Could not find function \"{name}\".", bestMatch != string.Empty ? $"Did you mean \"{bestMatch}\"." : null);
                return new BoundError();
            }

            SourceText source = c.SyntaxTree.Source;
            if (c.Args.Count > 0 && fn!.Parameters.Length == 0)
            {
                ImmutableArray<Node> nodes = c.Args.GetWithSeps();
                _diagnostics.Report(new(source, TextSpan.From(nodes.First().Span.Start, nodes.Last().Location.Span.End)), $"Wrong number of arguments; expected none - got \"{c.Args.Count}\".");
                return new BoundError();
            }

            if (c.Args.Count > fn!.Parameters.Length)
            {
                ImmutableArray<Node> nodes = c.Args.GetWithSeps();
                _diagnostics.Report(new(source, TextSpan.From(nodes.First(a => a.Span.Start != nodes[fn.Parameters.Length - 1].Span.Start).Span.Start, c.Args.Last().Location.Span.End)),
                    $"Wrong number of arguments; expected \"{fn.Parameters.Length}\" - got \"{c.Args.Count}\".",
                    $"\"{name}({c.Location.Source[c.Args.First().Location.StartColumn..c.Args[fn!.Parameters.Length - 1].Location.EndColumn]})\".");

                return new BoundError();
            }

            if (c.Args.Count == 0 && fn.Parameters.Length > 0)
            {
                _diagnostics.Report(new(source, TextSpan.From(c.LParen.Span.Start, c.RParen.Span.End)), $"Wrong number of arguments; expected \"{fn.Parameters.Length}\" - got none.");
                return new BoundError();
            }

            if (c.Args.Count < fn.Parameters.Length)
            {
                _diagnostics.Report(new(source, TextSpan.From(c.Args.First().Span.Start, c.Args.Last().Span.End)), $"Wrong number of arguments; expected \"{fn.Parameters.Length}\" - got \"{c.Args.Count}\".");
                return new BoundError();
            }

            ImmutableArray<BoundExpr>.Builder args = ImmutableArray.CreateBuilder<BoundExpr>();
            for (int i = 0; i < fn.Parameters.Length; ++i)
            {
                ParameterSymbol param = fn.Parameters[i];
                ExprNode rawArg = c.Args[i];
                BoundExpr arg = BindExpr(rawArg);
                args.Add(BindConversion(arg, param.Type, new(rawArg.Location.Source, rawArg.Span), false, $"Parameter \"{param.Name}\" has a type of \"{param.Type}\" - got a value with type of \"{arg.Type}\" as argument.", null,
                    $"{source[..rawArg.Span.Start]}{param.Type}({rawArg.Location.Text}){source[rawArg.Span.End..]}"));
            }

            return new BoundCall(fn, args.ToImmutable());
        }

        private BoundExpr BindConversion(ExprNode expr, TypeSymbol type, bool allowExplicit = false, string? errorImplicit = null, string? errorExists = null) => BindConversion(BindExpr(expr), type, expr.Location, allowExplicit, errorImplicit, errorExists);
        private BoundExpr BindConversion(BoundExpr expr, TypeSymbol type, TextLocation location, bool allowExplicit = false, string? errorImplicit = null, string? errorExists = null, string? errorImplSuggestion = null)
        {
            Conversion conversion = Conversion.Classify(expr.Type, type);
            if (!conversion.Exists)
            {
                if (expr.Type != TypeSymbol.Unknown && type != TypeSymbol.Unknown)
                    _diagnostics.Report(location, errorExists ?? $"No conversion from \"{expr.Type}\" to \"{type}\" possible.");
                return new BoundError();
            }

            if (!allowExplicit && conversion.IsExplicit)
                _diagnostics.Report(location, errorImplicit ?? $"No implicit conversion from \"{expr.Type}\" to \"{type}\" possible; though an explicit cast is.", errorImplSuggestion ?? $"\"{type.Name}({location.Text})\".");

            if (conversion.IsIdentity)
                return expr;
            return new BoundConversion(type, expr);
        }

        private VariableSymbol DeclareVar(Token id, TypeSymbol type, bool isMut = false)
        {
            VariableSymbol variable = _fn is null ? new GlobalVariableSymbol(id.Lexeme, type, isMut) : new LocalVariableSymbol(id.Lexeme, type, isMut);
            if (!id.IsMissing && !_scope.TryDeclareVar(variable) || _scope.TryLookupFn(variable.Name, out _))
                _diagnostics.Report(id.Location, $"\"{id.Lexeme}\" already exists.");

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
