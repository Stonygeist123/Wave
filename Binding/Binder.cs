using Wave.Binding.BoundNodes;
using Wave.Nodes;

namespace Wave.Binding
{
    internal class Binder
    {
        private readonly DiagnosticBag _diagnostics = new();
        public DiagnosticBag Diagnostics => _diagnostics;
        private readonly Dictionary<VariableSymbol, object?> _variables;
        public Binder(Dictionary<VariableSymbol, object?> variables) => _variables = variables;
        public BoundExpr BindExpr(ExprNode expr)
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
            bool variableExists = _variables.Keys.Any(v => v.Name == name);
            if (!variableExists)
            {
                _diagnostics.Report(n.Identifier.Span, $"Could not find {name}.");
                return new BoundLiteral(0);
            }

            return new BoundName(new(name, _variables.Keys.First(v => v.Name == name).Type));
        }

        private BoundExpr BindAssignmentExpr(AssignmentExpr a)
        {
            string name = a.Identifier.Lexeme;
            BoundExpr value = BindExpr(a.Value);
            bool variableExists = _variables.Keys.Any(v => v.Name == name);
            if (variableExists)
                _variables.Remove(_variables.Keys.First(v => v.Name == name));

            VariableSymbol variable = new(name, value.Type);
            _variables[variable] = null;
            return new BoundAssignment(new(name, value.Type), value);
        }
    }
}
