using System.Collections.Immutable;
using Wave.Binding.BoundNodes;
using Wave.Nodes;
using Wave.Syntax.Nodes;

namespace Wave.Binding
{
    internal class Binder
    {
        private readonly DiagnosticBag _diagnostics = new();
        private BoundScope _scope;
        public DiagnosticBag Diagnostics => _diagnostics;
        public Binder(BoundScope? parent) => _scope = new(parent);

        public static BoundGlobalScope BindGlobalScope(BoundGlobalScope? previous, CompilationUnit syntax)
        {
            BoundScope? parentScope = CreateParentScope(previous);
            Binder binder = new(parentScope);
            BoundStmt stmt = binder.BindStmt(syntax.Stmt);
            ImmutableArray<VariableSymbol> variables = binder._scope.GetDeclaredVars();
            ImmutableArray<Diagnostic> diagnostics = binder.Diagnostics.ToImmutableArray();
            return new(previous, diagnostics, variables, stmt);
        }

        private static BoundScope? CreateParentScope(BoundGlobalScope? previous)
        {
            Stack<BoundGlobalScope> stack = new();
            while (previous is not null)
            {
                stack.Push(previous);
                previous = previous.Previous;
            }

            BoundScope? parent = null;
            while (stack.Count > 0)
            {
                previous = stack.Pop();
                BoundScope scope = new(parent);
                foreach (VariableSymbol v in previous.Variables)
                    scope.TryDeclare(v);

                parent = scope;
            }

            return parent;
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
                ForStmt f => BindForStmt(f),
                _ => throw new Exception("Unexpected syntax."),
            };
        }

        private BoundExpressionStmt BindExpressionStmt(ExpressionStmt e) => new(BindExpr(e.Expr));
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
            VariableSymbol variable = DeclareVar(v.Name, value.Type, isMut);
            return new(variable, value);
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

            BoundStmt stmt = BindStmt(w.Stmt);
            return new(condition, stmt);
        }

        private BoundForStmt BindForStmt(ForStmt f)
        {
            BoundExpr lowerBound = BindExpr(f.LowerBound, TypeSymbol.Int);
            BoundExpr upperBound = BindExpr(f.UpperBound, TypeSymbol.Int);

            _scope = new(_scope);
            VariableSymbol variable = DeclareVar(f.Id, TypeSymbol.Int);
            BoundStmt stmt = BindStmt(f.Stmt);
            _scope = _scope.Parent!;
            return new(variable, lowerBound, upperBound, stmt);
        }

        private BoundExpr BindExpr(ExprNode expr, TypeSymbol targetType)
        {
            BoundExpr boundExpr = BindExpr(expr);
            if (targetType != TypeSymbol.Unknown && boundExpr.Type != TypeSymbol.Unknown && boundExpr.Type != targetType)
                _diagnostics.Report(expr.Span, $"Cannot convert type of \"{boundExpr.Type}\" to \"{targetType}\".");

            return boundExpr;
        }

        private BoundExpr BindExpr(ExprNode expr)
        {
            return expr switch
            {
                LiteralExpr l => new BoundLiteral(l.Value),
                UnaryExpr u => BindUnaryExpr(u),
                BinaryExpr b => BindBinaryExpr(b),
                GroupingExpr b => BindExpr(b.Expr),
                NameExpr n => BindNameExpr(n),
                AssignmentExpr a => BindAssignmentExpr(a),
                CallExpr c => BindCallExpr(c),
                _ => throw new Exception("Unexpected syntax."),
            };
        }

        private BoundExpr BindUnaryExpr(UnaryExpr u)
        {
            BoundExpr operand = BindExpr(u.Operand);
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
            BoundExpr left = BindExpr(b.Left);
            BoundExpr right = BindExpr(b.Right);
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

            if (!_scope.TryLookup(name, out VariableSymbol? variable))
            {
                _diagnostics.Report(n.Identifier.Span, $"Could not find \"{name}\".");
                return new BoundError();
            }

            return new BoundName(variable!);
        }

        private BoundExpr BindAssignmentExpr(AssignmentExpr a)
        {
            string name = a.Identifier.Lexeme;
            if (!_scope.TryLookup(name, out VariableSymbol? variable))
            {
                _diagnostics.Report(a.Identifier.Span, $"Could not find \"{name}\".");
                return new BoundError();
            }

            if (!variable!.IsMut)
                _diagnostics.Report(a.EqToken.Span, $"Cannot assign to \"{name}\" - it is a constant.");

            BoundExpr value = BindExpr(a.Value);
            if (variable.Type != value.Type)
            {
                _diagnostics.Report(a.Value.Span, $"Cannot assign a value with type of \"{value.Type}\" to \"{name}\" which has a type of \"{variable.Type}\".");
                return new BoundError();
            }

            return new BoundAssignment(variable, value);
        }

        private BoundExpr BindCallExpr(CallExpr c)
        {
            string name = c.Callee.Lexeme;
            IEnumerable<FunctionSymbol> fns = BuiltInFunctions.GetAll();
            FunctionSymbol? fn = fns.SingleOrDefault(f => f.Name == name);
            if (fn is null)
            {
                _diagnostics.Report(c.Callee.Span, $"Could not find function \"{name}\".");
                return new BoundError();
            }

            if (c.Args.Count > 0 && fn.Parameters.Length == 0)
            {
                ImmutableArray<Node> nodes = c.Args.GetWithSeps();
                _diagnostics.Report(TextSpan.From(nodes.First().Span.Start, nodes.Last().Span.End), $"Wrong number of arguments; expected none - got \"{c.Args.Count}\".");
                return new BoundError();
            }

            if (c.Args.Count > fn.Parameters.Length)
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
                args.Add(arg);

                if (param.Type != arg.Type)
                    _diagnostics.Report(c.Args[i].Span, $"Parameter \"{param.Name}\" has a type of \"{param.Type}\" - got a value with type of \"{arg.Type}\" as argument");
            }

            return new BoundCall(fn, args.ToImmutable());
        }

        private VariableSymbol DeclareVar(Token id, TypeSymbol type, bool isMut = false)
        {
            bool declare = !id.IsMissing;
            string name = id.Lexeme ?? "?";
            VariableSymbol variable = new(name, type, isMut);
            if (declare && !_scope.TryDeclare(variable))
                _diagnostics.Report(id.Span, $"\"{name}\" already exists.");

            return variable;
        }
    }
}
