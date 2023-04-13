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
    public class Binder
    {
        private readonly bool _isScript;
        private readonly FunctionSymbol? _fn;
        private readonly DiagnosticBag _diagnostics = new();
        private BoundScope _scope;
        public DiagnosticBag Diagnostics => _diagnostics;
        private readonly Stack<(LabelSymbol BreakLabel, LabelSymbol ContinueLabel)> _loopStack = new();
        private int _labelCounter = 0;
        public Binder(bool isScript, BoundScope? parent, FunctionSymbol? fn)
        {
            _scope = new(parent);
            _isScript = isScript;
            _fn = fn;
            if (fn is not null)
                foreach (ParameterSymbol param in fn.Parameters)
                    _scope.TryDeclareVar(param, true);
        }

        public static BoundGlobalScope BindGlobalScope(bool isScript, BoundGlobalScope? previous, ImmutableArray<SyntaxTree> syntaxTrees)
        {
            BoundScope parentScope = CreateParentScope(previous);
            Binder binder = new(isScript, parentScope, null);
            IEnumerable<FnDeclStmt> fnDecls = syntaxTrees.SelectMany(s => s.Root.Members).OfType<FnDeclStmt>();
            foreach (FnDeclStmt fnDecl in fnDecls)
                binder.BindFnDecl(fnDecl);

            GlobalStmt?[] firstGlobalStmts = syntaxTrees.Select(st => st.Root.Members.OfType<GlobalStmt>().FirstOrDefault()).Where(g => g is not null).ToArray();
            if (firstGlobalStmts.Length > 1)
            {
                foreach (GlobalStmt? globalStmt in firstGlobalStmts)
                    if (globalStmt is not null)
                        binder.Diagnostics.Report(globalStmt.Location, "At most one file can have global statements.");
            }

            IEnumerable<GlobalStmt> globalStmts = syntaxTrees.SelectMany(s => s.Root.Members).OfType<GlobalStmt>();
            ImmutableArray<BoundStmt>.Builder stmts = ImmutableArray.CreateBuilder<BoundStmt>();
            foreach (GlobalStmt globalStmt in globalStmts)
                stmts.Add(binder.BindGlobalStmt(globalStmt.Stmt));

            ImmutableArray<FunctionSymbol> functions = binder._scope.GetDeclaredFns();
            FunctionSymbol? mainFn = null, scriptFn = null;
            if (isScript)
            {
                if (globalStmts.Any())
                    scriptFn = new FunctionSymbol("$eval", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Void);
            }
            else
            {
                mainFn = functions.FirstOrDefault(fn => fn.Name == "main");
                if (mainFn is not null && (mainFn.Type != TypeSymbol.Void || mainFn.Parameters.Any()))
                    binder.Diagnostics.Report(mainFn.Decl!.Name.Location, "The main function must neither have a return type except void nor parameters.", $"\"fn main {mainFn.Decl!.Body.Location.Text.Split(Environment.NewLine).First()}\"");

                if (globalStmts.Any())
                {
                    if (mainFn is null)
                        mainFn = new FunctionSymbol("main", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Void);
                    else
                    {
                        binder.Diagnostics.Report(mainFn.Decl!.Name.Location, "Cannot declare a main function when global statements are used.");
                        foreach (GlobalStmt? globalStmt in firstGlobalStmts)
                            if (globalStmt is not null)
                                binder.Diagnostics.Report(globalStmt.Location, "Cannot use global statements when a main function is declared.");
                    }
                }
            }

            ImmutableArray<Diagnostic> diagnostics = binder.Diagnostics.ToImmutableArray();
            FunctionSymbol? globalStmtFn = mainFn ?? scriptFn;

            ImmutableArray<VariableSymbol> variables = binder._scope.GetDeclaredVars();
            if (previous is not null)
                diagnostics.InsertRange(0, previous.Diagnostics);

            return new(previous, mainFn, scriptFn, new BoundBlockStmt(stmts.ToImmutable()), variables, functions, diagnostics);
        }

        public static BoundProgram BindProgram(bool isScript, BoundProgram? previous, BoundGlobalScope globalScope)
        {
            BoundScope parentScope = CreateParentScope(globalScope);
            ImmutableDictionary<FunctionSymbol, BoundBlockStmt>.Builder fnBodies = ImmutableDictionary.CreateBuilder<FunctionSymbol, BoundBlockStmt>();
            DiagnosticBag diagnostics = new();
            BoundGlobalScope? scope = globalScope;
            foreach (FunctionSymbol fn in scope.Functions)
            {
                Binder binder = new(isScript, parentScope, fn);
                BoundBlockStmt body = Lowerer.Lower(binder.BindGlobalStmt(fn.Decl!.Body));
                if (fn.Type != TypeSymbol.Void && !AllPathsReturn(body) && fn.Decl!.Body.Kind != SyntaxKind.ExpressionStmt)
                    binder._diagnostics.Report(fn.Decl.Name.Location, $"All code paths must return a value.");

                fnBodies.Add(fn, body);
                diagnostics.AddRange(binder.Diagnostics);
            }

            if (globalScope.MainFn is not null && globalScope.Stmt.Stmts.Any())
                fnBodies.Add(globalScope.MainFn, Lowerer.Lower(globalScope.Stmt));
            else if (globalScope.ScriptFn is not null)
            {
                ImmutableArray<BoundStmt> stmts = globalScope.Stmt.Stmts;
                if (stmts.Length == 1 && stmts.First() is BoundExpressionStmt es && es.Expr.Type != TypeSymbol.Void)
                    stmts = stmts.SetItem(0, new BoundRetStmt(es.Expr));
                fnBodies.Add(globalScope.ScriptFn, Lowerer.Lower(new BoundBlockStmt(stmts)));
            }

            return new(previous, globalScope.MainFn, globalScope.ScriptFn, diagnostics.ToImmutableArray(), fnBodies.ToImmutable());
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
                            _diagnostics.Report(param.Type.Id.Location, $"Expected type.", "\": <type>\".");
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

                boundExpr = BindExpr(((ExpressionStmt)decl!.Body).Expr, true);
                _scope = _scope.Parent!;
            }

            TypeSymbol fnType = decl.TypeClause?.Id.Lexeme == "void" ? TypeSymbol.Void : BindTypeClause(decl.TypeClause) ?? boundExpr?.Type ?? TypeSymbol.Void;
            if (decl.TypeClause is null && fnType == TypeSymbol.Void)
            {
                Binder binder = new(_isScript, null, new(decl.Name.Lexeme, parameters.ToImmutable(), fnType));
                ControlFlowGraph graph = CreateGraph(Lowerer.Lower(binder.BindStmt(decl.Body!)));
                BasicBlockBranch? branch = graph.Start.Outgoing.FirstOrDefault(b => b.To.Stmts.Any(s => s.Kind == BoundNodeKind.RetStmt && ((BoundRetStmt)s).Value is not null));
                if (branch is not null)
                    fnType = ((BoundRetStmt)branch.To.Stmts.First(s => s.Kind == BoundNodeKind.RetStmt)).Value!.Type;
            }

            if (boundExpr is not null && (!Conversion.Classify(boundExpr.Type, fnType).IsImplicit || (fnType.IsArray && !boundExpr.Type.IsArray)))
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

        private BoundStmt BindGlobalStmt(StmtNode stmt) => BindStmt(stmt, true);
        private BoundStmt BindStmt(StmtNode stmt, bool isGlobal = false)
        {
            BoundStmt res = BindStmtInternal(stmt);
            if (!_isScript || !isGlobal)
            {
                if (res is BoundExpressionStmt e)
                {
                    bool allowed = e.Expr.Kind == BoundNodeKind.ErrorExpr || e.Expr.Kind == BoundNodeKind.AssignmentExpr || e.Expr.Kind == BoundNodeKind.CallExpr;
                    if (!allowed)
                        _diagnostics.Report(stmt.Location, $"Only assignment and call expressions can be used as a statement.");
                }
            }

            return res;
        }

        public BoundStmt BindStmtInternal(StmtNode stmt)
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
                stmts.Add(BindStmtInternal(stmt));

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
                return typeClause.LBracket is null ? t : new(t.Name, true);

            _diagnostics.Report(typeClause.Id.Location, $"Undefined type \"{typeClause.Id.Lexeme}\".");
            return null;
        }

        private BoundIfStmt BindIfStmt(IfStmt i)
        {
            BoundExpr condition = BindExpr(i.Condition, TypeSymbol.Bool);
            if (condition.Type != TypeSymbol.Bool)
                _diagnostics.Report(i.Condition.Location, $"Condition needs to be a bool.");

            BoundStmt thenBranch = BindStmtInternal(i.ThenBranch);
            BoundStmt? elseClause = i.ElseClause is not null ? BindStmtInternal(i.ElseClause.Stmt) : null;
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
            BoundStmt boundStmt = BindStmtInternal(stmt);
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
                        value = BindConversion(r.Value!, _fn.Type, false, false, $"Return type must match the type of function \"{_fn.Name}\"; expected \"{_fn.Type}\" - got \"{value.Type}\".");
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
                ArrayExpr a => BindArrayExpr(a),
                IndexingExpr a => BindIndexingExpr(a),
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
                    string bestMatch = FindBestMatch(name, _scope.GetVariables().Select(v => v.Name));
                    _diagnostics.Report(n.Identifier.Location, $"Could not find \"{name}\".", _scope.TryLookupFn(name, out _) ? $"\"{name}\" is a function therefore needs to be called." : bestMatch != string.Empty ? $"Did you mean \"{bestMatch}\"." : null);
                    return new BoundError();
                }

                return new BoundName(variable!);
            }
            else
            {
                if (!_scope.TryLookupVar(name, out VariableSymbol? variable))
                {
                    string bestMatch = FindBestMatch(name, _scope.GetVariables().Select(v => v.Name));
                    _diagnostics.Report(n.Identifier.Location, $"Could not find \"{name}\".", _scope.TryLookupFn(name, out _) ? $"\"{name}\" is a function therefore needs to be called." : bestMatch != string.Empty ? $"Did you mean \"{bestMatch}\"." : null);
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
                string bestMatch = FindBestMatch(id.Lexeme, _scope.GetVariables().Select(v => v.Name));
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
                return BindConversion(c.Args[0], type, true, true);

            string name = c.Callee.Lexeme;
            if (!_scope.TryLookupFn(name, out FunctionSymbol? fn) && fn is null)
            {
                string bestMatch = FindBestMatch(name, _scope.GetFunctions().Select(f => f.Name));
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
                args.Add(BindConversion(arg, param.Type, new(rawArg.Location.Source, rawArg.Span), false, false, $"Parameter \"{param.Name}\" has a type of \"{param.Type}\" - got a value with type of \"{arg.Type}\" as argument.", null,
                    $"\"{source[..rawArg.Span.Start]}{param.Type}({rawArg.Location.Text}){source[rawArg.Span.End..]}\""));
            }

            return new BoundCall(fn, args.ToImmutable());
        }

        private BoundExpr BindArrayExpr(ArrayExpr a)
        {
            if (a.Elements.Any())
            {
                ImmutableArray<BoundExpr>.Builder elements = ImmutableArray.CreateBuilder<BoundExpr>();
                foreach (ExprNode el in a.Elements)
                    elements.Add(BindExpr(el));

                TypeSymbol type;
                if (a.Type is null)
                {
                    type = elements.First().Type;
                    for (int i = 0; i < elements.Count; ++i)
                    {
                        BoundExpr el = elements[i];
                        TextLocation location = a.Elements[i].Location;
                        elements[i] = BindConversion(el, type, location, false, false, $"An array cannot have multiple types.");
                    }
                }
                else
                {
                    type = LookupType(a.Type.Lexeme) ?? TypeSymbol.Unknown;
                    for (int i = 0; i < elements.Count; ++i)
                    {
                        BoundExpr el = elements[i];
                        TextLocation location = a.Elements[i].Location;
                        elements[i] = BindConversion(el, type, location);
                    }
                }

                return new BoundArray(elements.ToImmutable(), new(type.Name, true));
            }
            else if (a.Type is null)
                return new BoundError();
            return new BoundArray(ImmutableArray<BoundExpr>.Empty, new((LookupType(a.Type!.Lexeme) ?? TypeSymbol.Unknown).Name, true));
        }

        private BoundExpr BindIndexingExpr(IndexingExpr a)
        {
            BoundExpr array = BindExpr(a.Array);
            BoundExpr index = BindExpr(a.Index, TypeSymbol.Int);
            if (!array.Type.IsArray)
            {
                _diagnostics.Report(a.Array.Location, $"Expected array - got \"{array.Type}\".");
                return new BoundError();
            }

            return new BoundIndexing(array, index);
        }

        private BoundExpr BindConversion(ExprNode expr, TypeSymbol type, bool allowExplicit = false, bool allowArray = true, string? errorImplicit = null, string? errorExists = null) => BindConversion(BindExpr(expr), type, expr.Location, allowExplicit, allowArray, errorImplicit, errorExists);
        private BoundExpr BindConversion(BoundExpr expr, TypeSymbol type, TextLocation location, bool allowExplicit = false, bool allowArray = false, string? errorImplicit = null, string? errorExists = null, string? errorImplSuggestion = null)
        {
            if (!allowArray && expr.Type.IsArray && !type.IsArray)
            {
                _diagnostics.Report(location, $"No conversion from non-array type to array type.");
                return new BoundError();
            }

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
            {
                return expr;
            }

            return new BoundConversion(type, expr);
        }

        private VariableSymbol DeclareVar(Token id, TypeSymbol type, bool isMut = false)
        {
            VariableSymbol variable = _fn is null ? new GlobalVariableSymbol(id.Lexeme, type, isMut) : new LocalVariableSymbol(id.Lexeme, type, isMut);
            if (!id.IsMissing && !_scope.TryDeclareVar(variable))
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
