﻿using System.Collections.Immutable;
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
        public DiagnosticBag Diagnostics => _diagnostics;
        private readonly DiagnosticBag _diagnostics = new();
        private readonly bool _isScript;
        private FunctionSymbol? _fn;
        private BoundScope _scope;
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
            IEnumerable<EnumDeclStmt> enumDecls = syntaxTrees.SelectMany(s => s.Root.Members).OfType<EnumDeclStmt>();
            foreach (EnumDeclStmt enumDecl in enumDecls)
                binder.BindEnumDecl(enumDecl);

            IEnumerable<FnDeclStmt> fnDecls = syntaxTrees.SelectMany(s => s.Root.Members).OfType<FnDeclStmt>();
            foreach (FnDeclStmt fnDecl in fnDecls)
                binder.BindFnDecl(fnDecl);

            IEnumerable<ClassDeclStmt> classDecls = syntaxTrees.SelectMany(s => s.Root.Members).OfType<ClassDeclStmt>();
            foreach (ClassDeclStmt classDecl in classDecls)
                binder.BindClassDecl(classDecl);

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
            if (previous is not null)
                diagnostics.InsertRange(0, previous.Diagnostics);
            return new(previous, mainFn, scriptFn, new BoundBlockStmt(stmts.ToImmutable()), binder._scope.GetDeclaredVars(), functions, binder._scope.GetDeclaredClasses(), binder._scope.GetDeclaredEnums(), diagnostics);
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

            return new(previous, globalScope.MainFn, globalScope.ScriptFn, diagnostics.ToImmutableArray(), fnBodies.ToImmutable(), scope.Classes, scope.ADTs);
        }

        private void BindFnDecl(FnDeclStmt decl, string? className = null, bool isStatic = false, ClassSymbol? classSymbol = null)
        {
            ImmutableArray<ParameterSymbol>.Builder parameters = ImmutableArray.CreateBuilder<ParameterSymbol>();
            if (decl.Parameters is not null)
            {
                HashSet<string> seenParameters = new();
                foreach (ParameterDecl param in decl.Parameters.Parameters)
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

                if (className is not null)
                    _fn = new MethodSymbol(decl.Name.Lexeme, ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Unknown, className, decl.Accessibility is null || decl.Accessibility.Kind == SyntaxKind.Private ? Accessibility.Priv : Accessibility.Pub, decl, isStatic);
                else
                    _fn = new FunctionSymbol(decl.Name.Lexeme, ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Unknown, decl);

                if (classSymbol is not null)
                    _scope.TryDeclareClass(classSymbol);

                boundExpr = BindExpr(((ExpressionStmt)decl!.Body).Expr, true);
                _fn = null;
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
            FunctionSymbol fn = className is null ? new FunctionSymbol(name.Lexeme, parameters.ToImmutable(), fnType, decl) : new MethodSymbol(name.Lexeme, parameters.ToImmutable(), fnType, className, Accessibility.Pub, decl, decl.StaticDot is not null);
            if (!_scope.TryDeclareFn(fn))
                _diagnostics.Report(name.Location, $"Function \"{name.Lexeme}\" was already declared with those parameters.");
        }

        private void BindClassDecl(ClassDeclStmt cd)
        {
            string name = cd.Name.Lexeme;
            Dictionary<FieldSymbol, BoundExpr> fields = new();
            foreach (FieldDecl f in cd.FieldDecls)
            {
                BoundExpr v = BindExpr(f.Value);
                TypeSymbol t = BindTypeClause(f.TypeClause) ?? v.Type;
                v = BindConversion(v, t, f.Value.Location, false, t.IsArray);
                if (fields.Any(f1 => f1.Key.Name == f.Name.Lexeme))
                    _diagnostics.Report(f.Name.Location, $"Field \"{f.Name.Lexeme}\" was already declared in class \"{name}\".");
                else
                    fields.Add(new(f.Name.Lexeme, t, f.Accessibility is null || f.Accessibility.Kind == SyntaxKind.Private ? Accessibility.Priv : Accessibility.Pub, f.MutKeyword is not null, f.StaticDot is not null, name), v);
            }

            KeyValuePair<CtorSymbol, BoundBlockStmt>? ctor = null;
            if (cd.Ctor is not null)
            {
                ImmutableArray<ParameterSymbol>.Builder ctorParameters = ImmutableArray.CreateBuilder<ParameterSymbol>();
                if (cd.Ctor.Parameters is not null)
                {
                    HashSet<string> seenParameters = new();
                    foreach (ParameterDecl param in cd.Ctor.Parameters.Parameters)
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
                                ctorParameters.Add(new ParameterSymbol(pName, pType));
                        }
                    }
                }

                _scope = new(_scope);
                _scope.TryDeclareClass(new(name, null, ImmutableDictionary<MethodSymbol, BoundBlockStmt>.Empty, fields));
                foreach (ParameterSymbol parameter in ctorParameters.ToImmutable())
                    _scope.TryDeclareVar(parameter);

                _fn = new MethodSymbol("", _scope.GetDeclaredVars().Cast<ParameterSymbol>().ToImmutableArray(), TypeSymbol.Void, name, Accessibility.Pub, null);
                BoundBlockStmt loweredCtorBody = Lowerer.Lower(BindStmtInternal(cd.Ctor.Body));
                _fn = null;
                ctor = new(new(ctorParameters.ToImmutable(), name, cd.Ctor.Accessibility is null || cd.Ctor.Accessibility.Kind == SyntaxKind.Private ? Accessibility.Priv : Accessibility.Pub, cd.Ctor), loweredCtorBody);
                _scope = _scope.Parent!;
            }

            _scope = new BoundScope(_scope);
            foreach (FnDeclStmt fnDecl in cd.FnDecls)
                BindFnDecl(fnDecl, name, fnDecl.StaticDot is not null,
                    new(name, ctor, _scope.GetDeclaredFns().ToImmutableDictionary(x => (MethodSymbol)x, x => new BoundBlockStmt(ImmutableArray<BoundStmt>.Empty)), fields));

            ImmutableDictionary<MethodSymbol, BoundBlockStmt> fnsOld = _scope.GetDeclaredFns().ToImmutableDictionary(x => (MethodSymbol)x, x => new BoundBlockStmt(ImmutableArray<BoundStmt>.Empty));
            _scope = _scope.Parent!;

            ImmutableDictionary<MethodSymbol, BoundBlockStmt>.Builder fns = ImmutableDictionary.CreateBuilder<MethodSymbol, BoundBlockStmt>();
            foreach (FnDeclStmt fnDecl in cd.FnDecls)
            {
                bool isStatic = fnDecl.StaticDot is not null;
                ImmutableArray<ParameterSymbol>.Builder parameters = ImmutableArray.CreateBuilder<ParameterSymbol>();
                if (fnDecl.Parameters is not null)
                {
                    HashSet<string> seenParameters = new();
                    foreach (ParameterDecl param in fnDecl.Parameters.Parameters)
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

                Token fnName = fnDecl.Name;
                BoundExpr? boundExpr = null;
                if (fnDecl.Body.Kind == SyntaxKind.ExpressionStmt)
                {
                    _scope = new(_scope);
                    foreach (ParameterSymbol parameter in parameters.ToImmutable())
                        _scope.TryDeclareVar(parameter);

                    foreach ((FieldSymbol f, BoundExpr e) in fields)
                        _scope.TryDeclareVar(new LocalVariableSymbol(f.Name, f.Type, f.IsMut), true);

                    foreach ((FunctionSymbol fn, BoundBlockStmt e) in fns)
                        _scope.TryDeclareFn(fn);

                    _fn = new MethodSymbol(fnName.Lexeme, ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Unknown, name, fnDecl.Accessibility is null || fnDecl.Accessibility.Kind == SyntaxKind.Private ? Accessibility.Priv : Accessibility.Pub, fnDecl, isStatic);
                    _scope.TryDeclareClass(new(name, ctor, fns.ToImmutable(), fields));
                    boundExpr = BindExpr(((ExpressionStmt)fnDecl.Body).Expr, true);
                    _fn = null;
                    _scope = _scope.Parent!;
                }

                TypeSymbol fnType = fnDecl.TypeClause?.Id.Lexeme == "void" ? TypeSymbol.Void : BindTypeClause(fnDecl.TypeClause) ?? boundExpr?.Type ?? TypeSymbol.Void;
                if (fnDecl.TypeClause is null && fnType == TypeSymbol.Void)
                {
                    Binder binder = new(_isScript, null, new(fnDecl.Name.Lexeme, parameters.ToImmutable(), fnType));
                    ControlFlowGraph graph = CreateGraph(Lowerer.Lower(binder.BindStmt(fnDecl.Body!)));
                    BasicBlockBranch? branch = graph.Start.Outgoing.FirstOrDefault(b => b.To.Stmts.Any(s => s.Kind == BoundNodeKind.RetStmt && ((BoundRetStmt)s).Value is not null));
                    if (branch is not null)
                        fnType = ((BoundRetStmt)branch.To.Stmts.First(s => s.Kind == BoundNodeKind.RetStmt)).Value!.Type;
                }

                if (boundExpr is not null && (!Conversion.Classify(boundExpr.Type, fnType).IsImplicit || (fnType.IsArray && !boundExpr.Type.IsArray)))
                    _diagnostics.Report(fnDecl!.Body.Location, $"Expected a value with type of \"{fnType}\" - got \"{boundExpr.Type}\".");

                if (fns.Any(f => f.Key.Name == fnName.Lexeme && f.Key.Parameters.Select(p => p.Type).SequenceEqual(parameters.ToImmutable().Select(p => p.Type)) && f.Key.Parameters.Select(p => p.Type.IsArray).SequenceEqual(parameters.ToImmutable().Select(p => p.Type.IsArray))))
                    _diagnostics.Report(fnName.Location, $"Function \"{fnName.Lexeme}\" with those parameters was already declared in class \"{name}\".");
                else
                {
                    _scope = new(_scope);
                    _fn = new MethodSymbol(fnName.Lexeme, parameters.ToImmutable(), fnType, name, fnDecl.Accessibility is null || fnDecl.Accessibility.Kind == SyntaxKind.Private ? Accessibility.Priv : Accessibility.Pub, fnDecl, isStatic);
                    _scope.TryDeclareClass(new(name, ctor, fnsOld, fields));
                    foreach (ParameterSymbol parameter in parameters.ToImmutable())
                        _scope.TryDeclareVar(parameter);

                    BoundBlockStmt loweredBody = Lowerer.Lower(BindStmtInternal(fnDecl.Body));
                    fns.Add(new MethodSymbol(fnName.Lexeme, parameters.ToImmutable(), fnType, name, fnDecl.Accessibility is null || fnDecl.Accessibility.Kind == SyntaxKind.Private ? Accessibility.Priv : Accessibility.Pub, fnDecl, isStatic), loweredBody);
                    if (fnType != TypeSymbol.Void && !AllPathsReturn(loweredBody) && fnDecl!.Body.Kind != SyntaxKind.ExpressionStmt)
                        _diagnostics.Report(fnName.Location, $"All code paths must return a value.");

                    _fn = null;
                    _scope = _scope.Parent!;
                }
            }

            ClassSymbol c = new(name, ctor, fns.ToImmutable(), fields);
            if (!_scope.TryDeclareClass(c))
                _diagnostics.Report(cd.Name.Location, $"Class \"{name}\" was already declared.");
        }

        private void BindEnumDecl(EnumDeclStmt ed)
        {
            string name = ed.Name.Lexeme;
            int i = 0;
            string[] memberNames = ed.Members.Select(t => t.Lexeme).ToArray();
            foreach (string? member in ed.Members.Select(t => t.Lexeme).GroupBy(x => x).Where(g => g.Count() > 1).Select(y => y.Key).ToList())
                _diagnostics.Report(ed.Members.Last(t => t.Lexeme == member).Location, $"Duplicated member \"{member}\" in type \"{name}\".");

            ImmutableDictionary<string, BoundExpr> members = memberNames.ToDictionary(id => id, _ => (BoundExpr)new BoundLiteral(i++)).ToImmutableDictionary();
            if (!_scope.TryDeclareADT(new(name, members)))
                _diagnostics.Report(ed.Name.Location, $"Type \"{name}\" was already declared.");
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

                foreach (ClassSymbol c in previous.Classes)
                    scope.TryDeclareClass(c);

                foreach (ADTSymbol a in previous.ADTs)
                    scope.TryDeclareADT(a);

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
                if (res is BoundExpressionStmt e)
                {
                    bool allowed = e.Expr.Kind == BoundNodeKind.ErrorExpr || e.Expr.Kind == BoundNodeKind.AssignmentExpr || e.Expr.Kind == BoundNodeKind.CallExpr || e.Expr.Kind == BoundNodeKind.SetExpr || e.Expr.Kind == BoundNodeKind.MethodExpr;
                    if (!allowed)
                        _diagnostics.Report(stmt.Location, $"Only assignment and call expressions can be used as a statement.");
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
                ForEachStmt fe => BindForEachStmt(fe),
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
            value = BindConversion(value, type, v.Value.Location, false, type.IsArray);
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
            BoundExpr condition = w.Condition is null ? new BoundLiteral(true) : BindExpr(w.Condition, TypeSymbol.Bool);
            BoundStmt stmt = BoundLoopStmt(w.Stmt, out LabelSymbol bodyLabel, out LabelSymbol breakLabel, out LabelSymbol continueLabel);
            return new(condition, stmt, bodyLabel, breakLabel, continueLabel);
        }

        private BoundDoWhileStmt BindDoWhileStmt(DoWhileStmt d)
        {
            BoundStmt stmt = BoundLoopStmt(d.Stmt, out LabelSymbol bodyLabel, out LabelSymbol breakLabel, out LabelSymbol continueLabel);
            BoundExpr condition = d.Condition is null ? new BoundLiteral(true) : BindExpr(d.Condition, TypeSymbol.Bool);
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

        private BoundForEachStmt BindForEachStmt(ForEachStmt fe)
        {
            BoundExpr array = BindExpr(fe.Array);
            if (!array.Type.IsArray)
                _diagnostics.Report(fe.Array.Location, $"Expected an array.");

            _scope = new(_scope);
            VariableSymbol variable = DeclareVar(fe.Id, new(array.Type.Name, false));
            VariableSymbol? index = fe.Index is null ? null : DeclareVar(fe.Index, TypeSymbol.Int);
            BoundStmt stmt = BoundLoopStmt(fe.Stmt, out LabelSymbol bodyLabel, out LabelSymbol breakLabel, out LabelSymbol continueLabel);
            _scope = _scope.Parent!;
            return new(variable, index, array, stmt, bodyLabel, breakLabel, continueLabel);
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
                IndexingExpr i => BindIndexingExpr(i),
                GetExpr g => BindGetExpr(g),
                MethodExpr m => BindMethodExpr(m),
                SetExpr s => BindSetExpr(s),
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

        string FindBestMatch(string stringToCompare, IEnumerable<string> strs, TypeSymbol? type = null)
        {

            HashSet<string> strCompareHash = stringToCompare.Split(' ').ToHashSet();
            int maxIntersectCount = 0;
            string bestMatch = string.Empty;

            foreach (string str in strs)
            {
                HashSet<string> strHash = str.Split(' ').ToHashSet();
                int intersectCount = strCompareHash.Intersect(strCompareHash).Count();
                if (intersectCount > maxIntersectCount && (type is null || _scope.GetVariables().Any(v => v.Name == str && v.Type == type)))
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

            if (!_scope.TryLookupVar(name, out VariableSymbol? variable))
            {
                string bestMatch = FindBestMatch(name, _scope.GetVariables().Select(v => v.Name));
                _diagnostics.Report(n.Identifier.Location, $"Could not find variable \"{name}\".", _scope.TryLookupFn(name, out _) ? $"\"{name}\" is a function therefore needs to be called." : bestMatch != string.Empty ? $"Did you mean \"{bestMatch}\"." : null);
                return new BoundError();
            }

            return new BoundName(variable!);
        }

        private BoundExpr BindAssignmentExpr(AssignmentExpr a)
        {
            ExprNode id = a.Id;
            BoundExpr value = BindExpr(a.Value);
            if (id.Kind == SyntaxKind.NameExpr)
            {
                Token name = ((NameExpr)id).Identifier;
                if (!_scope.TryLookupVar(name.Lexeme, out VariableSymbol? variable))
                {
                    string bestMatch = FindBestMatch(name.Lexeme, _scope.GetVariables().Select(v => v.Name));
                    _diagnostics.Report(id.Location, $"Could not find variable \"{name.Lexeme}\".", bestMatch != string.Empty ? $"Did you mean \"{bestMatch}\"." : null);
                    return new BoundError();
                }

                if (!variable!.IsMut)
                    _diagnostics.Report(a.Location, $"Cannot assign to \"{name.Lexeme}\" - it is a constant.");

                return new BoundAssignment(variable, BindConversion(value, variable.Type, a.Location));
            }
            else if (id.Kind == SyntaxKind.IndexingExpr)
            {
                IndexingExpr indexing = (IndexingExpr)a.Id;
                if (indexing.Expr.Kind != SyntaxKind.NameExpr)
                {
                    _diagnostics.Report(indexing.Expr.Location, "Cannot change value of constant expression.");
                    return new BoundError();
                }

                string arrName = ((NameExpr)indexing.Expr).Identifier.Lexeme;
                if (!_scope.TryLookupVar(arrName, out VariableSymbol? variable))
                {
                    string bestMatch = FindBestMatch(arrName, _scope.GetVariables().Select(v => v.Name), new(value.Type.Name, true));
                    _diagnostics.Report(indexing.Expr.Location, $"Could not find variable \"{arrName}\".", bestMatch != string.Empty ? $"Did you mean \"{bestMatch}\"." : null);
                    return new BoundError();
                }

                if (!variable!.IsMut)
                    _diagnostics.Report(a.Location, $"Cannot assign to \"{arrName}\" - it is a constant.");

                return new BoundArrayAssignment(variable!, BindExpr(indexing.Index), value);
            }

            return new BoundError();
        }

        private BoundExpr BindCallExpr(CallExpr c)
        {
            if (c.Args.Count == 1 && LookupType(c.Callee.Lexeme) is TypeSymbol type && !type.IsClass && !type.IsADT)
                return BindConversion(c.Args[0], type, true, true);

            string name = c.Callee.Lexeme;
            SourceText source = c.SyntaxTree.Source;
            ImmutableArray<Node> argNodes = c.Args.GetWithSeps();
            BoundExpr[] args = c.Args.Select(a => BindExpr(a)).ToArray();
            ImmutableArray<BoundExpr>.Builder ctorBoundArgs = ImmutableArray.CreateBuilder<BoundExpr>();
            if (_scope.TryLookupClass(name, out ClassSymbol? cs))
            {
                CtorSymbol? ctor = cs!.Ctor?.Key;
                if (ctor is not null && ctor.Accessibility == Accessibility.Priv && (_fn is null || _fn is not MethodSymbol m || m.ClassName != name))
                {
                    _diagnostics.Report(c.Location, $"Cannot instantiate a class with a private constructor.");
                    return new BoundError();
                }

                if (c.Args.Any())
                {
                    if (ctor is null)
                        _diagnostics.Report(new(source, TextSpan.From(argNodes.First().Span.Start, argNodes.Last().Location.Span.End)), $"Class \"{name}\" does not have a constructor with parameters.");
                    else
                    {
                        int paramCount = ctor.Parameters.Length;
                        if (c.Args.Count > 0 && ctor!.Parameters.Length == 0)
                        {
                            _diagnostics.Report(new(source, TextSpan.From(argNodes.First().Span.Start, argNodes.Last().Location.Span.End)), $"Wrong number of arguments; expected {(paramCount == 0 ? "none" : paramCount)} - got \"{c.Args.Count}\".");
                            return new BoundError();
                        }

                        if (c.Args.Count > ctor!.Parameters.Length)
                        {
                            _diagnostics.Report(new(source, TextSpan.From(argNodes.First(a => a.Span.Start != argNodes[ctor.Parameters.Length - 1].Span.Start).Span.Start, c.Args.Last().Location.Span.End)),
                                $"Wrong number of arguments; expected \"{ctor.Parameters.Length}\" - got \"{c.Args.Count}\".",
                                $"\"{name}({c.Location.Source[c.Args.First().Location.StartColumn..c.Args[ctor!.Parameters.Length - 1].Location.EndColumn]})\".");

                            return new BoundError();
                        }

                        if (c.Args.Count == 0 && ctor!.Parameters.Length > 0)
                        {
                            _diagnostics.Report(new(source, TextSpan.From(c.LParen.Span.Start, c.RParen.Span.End)), $"Wrong number of arguments; expected \"{ctor.Parameters.Length}\" - got none.");
                            return new BoundError();
                        }

                        if (c.Args.Count < ctor.Parameters.Length)
                        {
                            _diagnostics.Report(new(source, TextSpan.From(c.Args.First().Span.Start, c.Args.Last().Span.End)), $"Wrong number of arguments; expected \"{ctor.Parameters.Length}\" - got \"{c.Args.Count}\".");
                            return new BoundError();
                        }

                        for (int i = 0; i < ctor.Parameters.Length; ++i)
                        {
                            ParameterSymbol param = ctor.Parameters[i];
                            ExprNode rawArg = c.Args[i];
                            BoundExpr arg = args[i];
                            ctorBoundArgs.Add(BindConversion(arg, param.Type, new(rawArg.Location.Source, rawArg.Span), false, false, $"Parameter \"{param.Name}\" has a type of \"{param.Type}\" - got a value with type of \"{arg.Type}\" as argument.", null,
                                $"\"{source[..rawArg.Span.Start]}{param.Type}({rawArg.Location.Text}){source[rawArg.Span.End..]}\""));
                        }
                    }
                }

                return new BoundInstance(name, args.ToImmutableArray());
            }

            if (!_scope.TryLookupFn(name, out FunctionSymbol[] fns) && !fns.Any())
            {
                string bestMatch = FindBestMatch(name, _scope.GetFunctions().Select(f => f.Name));
                _diagnostics.Report(c.Callee.Location, $"Could not find function \"{name}\".", bestMatch != string.Empty ? $"Did you mean \"{bestMatch}\"." : null);
                return new BoundError();
            }

            FunctionSymbol? fn = fns.Length == 1 ? fns.First() : fns.SingleOrDefault(f => f.Parameters.Length == c.Args.Count && f.Parameters.Select(p => p.Type).SequenceEqual(args.Select(a => a.Type)));
            fn ??= fns.FirstOrDefault(f => f.Parameters.Length == c.Args.Count);

            int[] paramCounts = fns.Select(fn => fn.Parameters.Length).ToArray();
            if (fn is null)
            {
                _diagnostics.Report(new(source, TextSpan.From(argNodes.First().Span.Start, argNodes.Last().Location.Span.End)), $"Wrong number of arguments; expected {(paramCounts.All(l => l == 0) ? "none" : string.Join(", ", paramCounts))} - got \"{c.Args.Count}\".");
                return new BoundError();
            }

            if (c.Args.Count > 0 && fn!.Parameters.Length == 0)
            {
                _diagnostics.Report(new(source, TextSpan.From(argNodes.First().Span.Start, argNodes.Last().Location.Span.End)), $"Wrong number of arguments; expected {(paramCounts.All(l => l == 0) ? "none" : string.Join(", ", paramCounts))} - got \"{c.Args.Count}\".");
                return new BoundError();
            }

            if (c.Args.Count > fn!.Parameters.Length)
            {
                _diagnostics.Report(new(source, TextSpan.From(argNodes.First(a => a.Span.Start != argNodes[fn.Parameters.Length - 1].Span.Start).Span.Start, c.Args.Last().Location.Span.End)),
                    $"Wrong number of arguments; expected \"{fn.Parameters.Length}\" - got \"{c.Args.Count}\".",
                    $"\"{name}({c.Location.Source[c.Args.First().Location.StartColumn..c.Args[fn!.Parameters.Length - 1].Location.EndColumn]})\".");

                return new BoundError();
            }

            if (c.Args.Count == 0 && fn!.Parameters.Length > 0)
            {
                _diagnostics.Report(new(source, TextSpan.From(c.LParen.Span.Start, c.RParen.Span.End)), $"Wrong number of arguments; expected \"{fn.Parameters.Length}\" - got none.");
                return new BoundError();
            }

            if (c.Args.Count < fn.Parameters.Length)
            {
                _diagnostics.Report(new(source, TextSpan.From(c.Args.First().Span.Start, c.Args.Last().Span.End)), $"Wrong number of arguments; expected \"{fn.Parameters.Length}\" - got \"{c.Args.Count}\".");
                return new BoundError();
            }

            ImmutableArray<BoundExpr>.Builder boundArgs = ImmutableArray.CreateBuilder<BoundExpr>();
            for (int i = 0; i < fn.Parameters.Length; ++i)
            {
                ParameterSymbol param = fn.Parameters[i];
                ExprNode rawArg = c.Args[i];
                BoundExpr arg = args[i];
                boundArgs.Add(BindConversion(arg, param.Type, new(rawArg.Location.Source, rawArg.Span), false, false, $"Parameter \"{param.Name}\" has a type of \"{param.Type}\" - got a value with type of \"{arg.Type}\" as argument.", null,
                    $"\"{source[..rawArg.Span.Start]}{param.Type}({rawArg.Location.Text}){source[rawArg.Span.End..]}\""));
            }

            return new BoundCall(fn, boundArgs.ToImmutable());
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

        private BoundExpr BindIndexingExpr(IndexingExpr i)
        {
            if (i.Expr is NameExpr n && _scope.TryLookupEnum(n.Identifier.Lexeme, out ADTSymbol? e))
            {
                BoundExpr member = BindExprInternal(i.Index);
                if (member.Type.Name == e!.Name && !member.Type.IsArray && member.Type.IsADT)
                    return new BoundEnumIndexing(e, member);
            }

            BoundExpr index = BindExpr(i.Index, TypeSymbol.Int);
            BoundExpr expr = BindExpr(i.Expr);
            if (!expr.Type.IsArray && expr.Type != TypeSymbol.String)
            {
                _diagnostics.Report(i.Expr.Location, $"Expected array or string - got \"{expr.Type}\".");
                return new BoundError();
            }

            return new BoundIndexing(expr, index);
        }

        private BoundExpr BindGetExpr(GetExpr g)
        {
            if (g.Id is not null)
            {
                string id = g.Id.Lexeme;
                if (_scope.TryLookupEnum(id, out ADTSymbol? e))
                {
                    if (e!.Members.Any(f => f.Key == g.Field.Lexeme))
                        return new BoundEnumGet(e!, g.Field.Lexeme);

                    _diagnostics.Report(g.Field.Location, $"Could not find member \"{g.Field.Lexeme}\" on type \"{id}\".");
                    return new BoundError();
                }

                bool staticAccess = false;
                if (!_scope.TryLookupVar(id, out VariableSymbol? variable))
                {
                    if (!_scope.TryLookupClass(id, out _))
                    {
                        _diagnostics.Report(g.Id.Location, $"Could not find instance \"{id}\".");
                        return new BoundError();
                    }

                    staticAccess = true;
                }

                if (!_scope.TryLookupClass(staticAccess ? id : variable!.Type.Name, out ClassSymbol? c))
                {
                    _diagnostics.Report(g.Id.Location, $"\"{id}\" needs to be an instance of a class.");
                    return new BoundError();
                }

                FieldSymbol f = c!.Fields.Single(f => f.Key.Name == g.Field.Lexeme).Key;
                if (f.Accessibility == Accessibility.Priv && (_fn is null || _fn is not MethodSymbol m || m.ClassName != c.Name))
                {
                    _diagnostics.Report(g.Field.Location, $"Cannot access field \"{g.Field.Lexeme}\" - it is private.");
                    return new BoundError();
                }

                return new BoundGet(variable, f);
            }
            else
            {
                if (_fn is null || _fn is not MethodSymbol m)
                {
                    _diagnostics.Report(g.Location, "You need an instance to access non-static class fields.");
                    return new BoundError();
                }

                ClassSymbol c = _scope.GetClasses().Single(c => c.Name == m.ClassName);
                if (!c!.Fields.Any(f => f.Key.Name == g.Field.Lexeme))
                {
                    _diagnostics.Report(g.Field.Location, $"Could not find field \"{g.Field.Lexeme}\".");
                    return new BoundError();
                }

                FieldSymbol f = c!.Fields.Single(f => f.Key.Name == g.Field.Lexeme).Key;
                if (m.IsStatic && !f.IsStatic)
                {
                    _diagnostics.Report(g.Location, "Cannot access a non-static class field while being in a static function.");
                    return new BoundError();
                }

                return new BoundGet(null, f);
            }
        }

        private BoundExpr BindMethodExpr(MethodExpr m)
        {
            string callee = m.Callee.Lexeme;
            if (m.Id is not null)
            {
                bool staticAccess = false;
                string name = m.Id.Lexeme;
                if (!_scope.TryLookupVar(name, out VariableSymbol? variable))
                {
                    if (!_scope.TryLookupClass(name, out _))
                    {
                        _diagnostics.Report(m.Id.Location, $"Could not find instance \"{name}\".");
                        return new BoundError();
                    }

                    staticAccess = true;
                }

                if (!_scope.TryLookupClass(staticAccess ? name : variable!.Type.Name, out ClassSymbol? c))
                {
                    _diagnostics.Report(m.Id.Location, $"\"{name}\" needs to be an instance of a class.");
                    return new BoundError();
                }

                MethodSymbol fn = c!.Fns.Single(f => f.Key.Name == callee).Key;
                if (staticAccess && fn is MethodSymbol m1 && !m1.IsStatic)
                {
                    _diagnostics.Report(m.Callee.Location, $"Cannot access non-static method \"{fn.Name}\" without an instance of \"{name}\".");
                    return new BoundError();
                }

                if (fn.Accessibility == Accessibility.Priv && (_fn is null || _fn is not MethodSymbol m2 || m2.ClassName != c.Name))
                {
                    _diagnostics.Report(m.Callee.Location, $"Cannot access method \"{fn.Name}\" - it is private.");
                    return new BoundError();
                }

                SourceText source = m.SyntaxTree.Source;
                ImmutableArray<Node> argNodes = m.Args.GetWithSeps();
                BoundExpr[] args = m.Args.Select(a => BindExpr(a)).ToArray();
                int paramCount = fn.Parameters.Length;
                if (m.Args.Count > 0 && fn.Parameters.Length == 0)
                {
                    _diagnostics.Report(new(source, TextSpan.From(argNodes.First().Span.Start, argNodes.Last().Location.Span.End)), $"Wrong number of arguments; expected {(paramCount == 0 ? "none" : paramCount)} - got \"{m.Args.Count}\".");
                    return new BoundError();
                }

                if (m.Args.Count > fn.Parameters.Length)
                {
                    _diagnostics.Report(new(source, TextSpan.From(argNodes.First(a => a.Span.Start != argNodes[fn.Parameters.Length - 1].Span.Start).Span.Start, m.Args.Last().Location.Span.End)),
                        $"Wrong number of arguments; expected \"{fn.Parameters.Length}\" - got \"{m.Args.Count}\".",
                        $"\"{name}.{callee}({m.Location.Source[m.Args.First().Location.StartColumn..m.Args[fn!.Parameters.Length - 1].Location.EndColumn]})\".");

                    return new BoundError();
                }

                if (m.Args.Count == 0 && fn.Parameters.Length > 0)
                {
                    _diagnostics.Report(new(source, TextSpan.From(m.LParen.Span.Start, m.RParen.Span.End)), $"Wrong number of arguments; expected \"{fn.Parameters.Length}\" - got none.");
                    return new BoundError();
                }

                if (m.Args.Count < fn.Parameters.Length)
                {
                    _diagnostics.Report(new(source, TextSpan.From(m.Args.First().Span.Start, m.Args.Last().Span.End)), $"Wrong number of arguments; expected \"{fn.Parameters.Length}\" - got \"{m.Args.Count}\".");
                    return new BoundError();
                }

                ImmutableArray<BoundExpr>.Builder boundArgs = ImmutableArray.CreateBuilder<BoundExpr>();
                for (int i = 0; i < fn.Parameters.Length; ++i)
                {
                    ParameterSymbol param = fn.Parameters[i];
                    ExprNode rawArg = m.Args[i];
                    BoundExpr arg = args[i];
                    boundArgs.Add(BindConversion(arg, param.Type, new(rawArg.Location.Source, rawArg.Span), false, false, $"Parameter \"{param.Name}\" has a type of \"{param.Type}\" - got a value with type of \"{arg.Type}\" as argument.", null,
                        $"\"{source[..rawArg.Span.Start]}{param.Type}({rawArg.Location.Text}){source[rawArg.Span.End..]}\""));
                }

                return new BoundMethod(variable, fn, boundArgs.ToImmutable());
            }
            else
            {
                if (_fn is null || _fn is not MethodSymbol m1)
                {
                    _diagnostics.Report(m.Location, "You need an instance to access class members.");
                    return new BoundError();
                }

                ClassSymbol c = _scope.GetClasses().Single(c => c.Name == m1.ClassName);
                if (!c!.Fns.Any(fn => fn.Key.Name == callee))
                {
                    _diagnostics.Report(m.Callee.Location, $"Could not find method \"{callee}\".");
                    return new BoundError();
                }

                MethodSymbol fn = c!.Fns.Single(f => f.Key.Name == callee).Key;
                if (m1.IsStatic && _fn is MethodSymbol m2 && !m2.IsStatic)
                {
                    _diagnostics.Report(m.Location, "Cannot access a non-static class member while being in a static function.");
                    return new BoundError();
                }

                SourceText source = m.SyntaxTree.Source;
                ImmutableArray<Node> argNodes = m.Args.GetWithSeps();
                BoundExpr[] args = m.Args.Select(a => BindExpr(a)).ToArray();
                int paramCount = fn.Parameters.Length;
                if (m.Args.Count > 0 && fn.Parameters.Length == 0)
                {
                    _diagnostics.Report(new(source, TextSpan.From(argNodes.First().Span.Start, argNodes.Last().Location.Span.End)), $"Wrong number of arguments; expected {(paramCount == 0 ? "none" : paramCount)} - got \"{m.Args.Count}\".");
                    return new BoundError();
                }

                if (m.Args.Count > fn.Parameters.Length)
                {
                    _diagnostics.Report(new(source, TextSpan.From(argNodes.First(a => a.Span.Start != argNodes[fn.Parameters.Length - 1].Span.Start).Span.Start, m.Args.Last().Location.Span.End)),
                        $"Wrong number of arguments; expected \"{fn.Parameters.Length}\" - got \"{m.Args.Count}\".",
                        $"\".{callee}({m.Location.Source[m.Args.First().Location.StartColumn..m.Args[fn!.Parameters.Length - 1].Location.EndColumn]})\".");

                    return new BoundError();
                }

                if (m.Args.Count == 0 && fn.Parameters.Length > 0)
                {
                    _diagnostics.Report(new(source, TextSpan.From(m.LParen.Span.Start, m.RParen.Span.End)), $"Wrong number of arguments; expected \"{fn.Parameters.Length}\" - got none.");
                    return new BoundError();
                }

                if (m.Args.Count < fn.Parameters.Length)
                {
                    _diagnostics.Report(new(source, TextSpan.From(m.Args.First().Span.Start, m.Args.Last().Span.End)), $"Wrong number of arguments; expected \"{fn.Parameters.Length}\" - got \"{m.Args.Count}\".");
                    return new BoundError();
                }

                ImmutableArray<BoundExpr>.Builder boundArgs = ImmutableArray.CreateBuilder<BoundExpr>();
                for (int i = 0; i < fn.Parameters.Length; ++i)
                {
                    ParameterSymbol param = fn.Parameters[i];
                    ExprNode rawArg = m.Args[i];
                    BoundExpr arg = args[i];
                    boundArgs.Add(BindConversion(arg, param.Type, new(rawArg.Location.Source, rawArg.Span), false, false, $"Parameter \"{param.Name}\" has a type of \"{param.Type}\" - got a value with type of \"{arg.Type}\" as argument.", null,
                        $"\"{source[..rawArg.Span.Start]}{param.Type}({rawArg.Location.Text}){source[rawArg.Span.End..]}\""));
                }

                return new BoundMethod(null, fn, boundArgs.ToImmutable());
            }
        }

        private BoundExpr BindSetExpr(SetExpr s)
        {
            if (s.Id is not null)
            {
                bool staticAccess = false;
                string id = s.Id.Lexeme;
                if (!_scope.TryLookupVar(id, out VariableSymbol? variable))
                {
                    if (!_scope.TryLookupClass(id, out _))
                    {
                        _diagnostics.Report(s.Id.Location, $"Could not find instance \"{id}\".");
                        return new BoundError();
                    }

                    staticAccess = true;
                }

                if (!_scope.TryLookupClass(staticAccess ? id : variable!.Type.Name, out ClassSymbol? c))
                {
                    _diagnostics.Report(s.Id.Location, $"\"{id}\" needs to be an instance of a class.");
                    return new BoundError();
                }

                FieldSymbol f = c!.Fields.Single(f => f.Key.Name == s.Field.Lexeme).Key;
                if (f.Accessibility == Accessibility.Priv && (_fn is null || _fn is not MethodSymbol m || m.ClassName != c.Name))
                {
                    _diagnostics.Report(s.Field.Location, $"Cannot access field \"{s.Field.Lexeme}\" - it is private.");
                    return new BoundError();
                }

                if (!f.IsStatic && (_fn is null || _fn is not MethodSymbol m1 || m1.IsStatic))
                {
                    _diagnostics.Report(s.Field.Location, $"Cannot access \"{s.Field.Lexeme}\" since it requires an instance.");
                    return new BoundError();
                }

                if (!staticAccess && !variable!.IsMut)
                {
                    _diagnostics.Report(new(s.Location.Source, TextSpan.From(s.EqToken.Span.Start, s.Value.Span.End)), $"Cannot reassign the field \"{s.Field.Lexeme}\" of a constant instance.");
                    return new BoundError();
                }

                if (!f.IsMut)
                {
                    _diagnostics.Report(new(s.Location.Source, TextSpan.From(s.EqToken.Span.Start, s.Value.Span.End)), $"Cannot reassign the constant field \"{s.Field.Lexeme}\" of an instance of class \"{c.Name}\".");
                    return new BoundError();
                }

                BoundExpr v = BindConversion(s.Value, f.Type, false, f.Type.IsArray);
                return new BoundSet(variable, f, v);
            }
            else
            {
                if (_fn is null || _fn is not MethodSymbol m)
                {
                    _diagnostics.Report(new(s.Location.Source, TextSpan.From(s.Dot.Span.Start, s.Field.Span.End)), "You need an instance to access class members.");
                    return new BoundError();
                }

                ClassSymbol c = _scope.GetClasses().Single(c => c.Name == m.ClassName);
                if (!c!.Fields.Any(f => f.Key.Name == s.Field.Lexeme))
                {
                    _diagnostics.Report(s.Field.Location, $"Could not find field \"{s.Field.Lexeme}\".");
                    return new BoundError();
                }

                FieldSymbol f = c!.Fields.Single(f => f.Key.Name == s.Field.Lexeme).Key;
                if (m.IsStatic && !f.IsStatic)
                {
                    _diagnostics.Report(s.Location, "Cannot access a non-static class member while being in a static function.");
                    return new BoundError();
                }

                if (!f.IsMut && !string.IsNullOrEmpty(_fn.Name))
                {
                    _diagnostics.Report(new(s.Location.Source, TextSpan.From(s.EqToken.Span.Start, s.Value.Span.End)), $"Cannot reassign the field \"{s.Field.Lexeme}\" - it is a constant.");
                    return new BoundError();
                }

                BoundExpr v = BindConversion(s.Value, f.Type, false, f.Type.IsArray);
                return new BoundSet(null, f, v);
            }
        }

        private BoundExpr BindConversion(ExprNode expr, TypeSymbol type, bool allowExplicit = false, bool allowArray = true, string? errorImplicit = null, string? errorExists = null) => BindConversion(BindExpr(expr), type, expr.Location, allowExplicit, allowArray, errorImplicit, errorExists);
        private BoundExpr BindConversion(BoundExpr expr, TypeSymbol type, TextLocation location, bool allowExplicit = false, bool allowArray = false, string? errorImplicit = null, string? errorExists = null, string? errorImplSuggestion = null)
        {
            if (!allowArray && expr.Type.IsArray && !type.IsArray)
            {
                _diagnostics.Report(location, $"No conversion from non-array type to array type possible.");
                return new BoundError();
            }

            Conversion conversion = Conversion.Classify(expr.Type, type);
            if (!conversion.Exists)
            {
                if (expr.Type != TypeSymbol.Unknown && type != TypeSymbol.Unknown)
                    _diagnostics.Report(location, errorExists ?? $"No conversion from \"{expr.Type}\" to \"{type}\" possible.");
                return new BoundError();
            }

            if (!allowExplicit && conversion.IsExplicit && !conversion.IsIdentity)
                _diagnostics.Report(location, errorImplicit ?? $"No implicit conversion from \"{expr.Type}\" to \"{type}\" possible; though an explicit cast is.", errorImplSuggestion ?? $"\"{type.Name}({location.Text})\".");

            if (conversion.IsIdentity)
                return expr;
            return new BoundConversion(type, expr);
        }

        private VariableSymbol DeclareVar(Token id, TypeSymbol type, bool isMut = false)
        {
            VariableSymbol variable = _fn is null ? new GlobalVariableSymbol(id.Lexeme, type, isMut) : new LocalVariableSymbol(id.Lexeme, type, isMut);
            if (!id.IsMissing && !_scope.TryDeclareVar(variable))
                _diagnostics.Report(id.Location, $"\"{id.Lexeme}\" already exists.");

            return variable;
        }

        private TypeSymbol? LookupType(string name) => name switch
        {
            "bool" => TypeSymbol.Bool,
            "int" => TypeSymbol.Int,
            "float" => TypeSymbol.Float,
            "string" => TypeSymbol.String,
            _ => null,
        } ?? (_scope.TryLookupClass(name, out _) ? new(name, false, true) : (_scope.TryLookupEnum(name, out _) ? new(name, false, false, true) : null));
    }
}
