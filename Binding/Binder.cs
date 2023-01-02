using System.Collections.Immutable;
using Wave.Binding.BoundNodes;
using Wave.Nodes;

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
            string name = v.Name.Lexeme;
            bool isMut = v.MutKeyword is not null;
            BoundExpr value = BindExpr(v.Value);
            VariableSymbol variable = new(name, value.Type, isMut);
            if (!_scope.TryDeclare(variable))
                _diagnostics.Report(v.Name.Span, $"\"{name}\" already exists.");

            return new(variable, value);
        }

        private BoundIfStmt BindIfStmt(IfStmt i)
        {
            BoundExpr condition = BindExpr(i.Condition, typeof(bool));
            if (condition.Type != typeof(bool))
                _diagnostics.Report(i.Condition.Span, $"Condition needs to be a bool.");

            BoundStmt thenBranch = BindStmt(i.ThenBranch);
            BoundStmt? elseClause = i.ElseClause is not null ? BindStmt(i.ElseClause.Stmt) : null;
            return new(condition, thenBranch, elseClause);
        }

        private BoundWhileStmt BindWhileStmt(WhileStmt w)
        {
            BoundExpr condition = BindExpr(w.Condition, typeof(bool));
            if (condition.Type != typeof(bool))
                _diagnostics.Report(w.Condition.Span, $"Condition needs to be a bool.");

            BoundStmt stmt = BindStmt(w.Stmt);
            return new(condition, stmt);
        }

        private BoundForStmt BindForStmt(ForStmt f)
        {
            BoundExpr lowerBound = BindExpr(f.LowerBound, typeof(int));
            BoundExpr upperBound = BindExpr(f.UpperBound, typeof(int));

            _scope = new(_scope);
            string name = f.Id.Lexeme;
            VariableSymbol variable = new(name, typeof(int), false);
            if (!_scope.TryDeclare(variable))
                _diagnostics.Report(f.Id.Span, $"\"{name}\" already exists.");

            BoundStmt stmt = BindStmt(f.Stmt);
            _scope = _scope.Parent!;
            return new(variable, lowerBound, upperBound, stmt);
        }

        private BoundExpr BindExpr(ExprNode expr, Type targetType)
        {
            BoundExpr boundExpr = BindExpr(expr);
            if (boundExpr.Type != targetType)
                _diagnostics.Report(expr.Span, $"Cannot type of \"{boundExpr.Type}\" to \"{targetType}\".");

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
                _ => throw new Exception("Unexpected syntax."),
            };
        }

        private BoundExpr BindUnaryExpr(UnaryExpr u)
        {
            BoundExpr operand = BindExpr(u.Operand);
            BoundUnOperator? op = BoundUnOperator.Bind(u.Op.Kind, operand.Type);
            if (op is null)
            {
                _diagnostics.Report(u.Op.Span, $"Unary operator \"{u.Op.Kind}\" is not defined for type \"{operand.Type}\".");
                return operand;
            }

            return new BoundUnary(op, operand);
        }

        private BoundExpr BindBinaryExpr(BinaryExpr b)
        {
            BoundExpr left = BindExpr(b.Left);
            BoundExpr right = BindExpr(b.Right);
            BoundBinOperator? op = BoundBinOperator.Bind(b.Op.Kind, left.Type, right.Type);

            if (op is null)
            {
                _diagnostics.Report(b.Op.Span, $"Binary operator \"{b.Op.Kind}\" is not defined for types \"{left.Type}\" and \"{right.Type}\".");
                return left;
            }

            return new BoundBinary(left, op, right);
        }

        private BoundExpr BindNameExpr(NameExpr n)
        {
            string name = n.Identifier.Lexeme;
            if (!_scope.TryLookup(name, out VariableSymbol variable))
            {
                _diagnostics.Report(n.Identifier.Span, $"Could not find \"{name}\".");
                return new BoundLiteral(0);
            }

            return new BoundName(variable);
        }

        private BoundExpr BindAssignmentExpr(AssignmentExpr a)
        {
            string name = a.Identifier.Lexeme;
            BoundExpr value = BindExpr(a.Value);
            if (!_scope.TryLookup(name, out VariableSymbol variable))
            {
                _diagnostics.Report(a.Identifier.Span, $"Could not find \"{name}\".");
                return value;
            }

            if (!variable.IsMut)
            {
                _diagnostics.Report(a.EqToken.Span, $"Cannot assign to \"{name}\" - it is a constant.");
                return value;
            }

            if (variable.Type != value.Type)
            {
                _diagnostics.Report(a.Value.Span, $"Cannot assign a value with type of \"{value.Type}\" to \"{name}\" which has a type of \"{variable.Type}\".");
                return value;
            }

            return new BoundAssignment(variable, value);
        }
    }
}
