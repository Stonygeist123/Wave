using System.Collections.Immutable;

namespace Wave.Binding.BoundNodes
{
    public abstract class BoundStmt : BoundNode { }

    public sealed class BoundExpressionStmt : BoundStmt
    {
        public override BoundNodeKind Kind => BoundNodeKind.ExpressionStmt;
        public BoundExpr Expr { get; }

        public BoundExpressionStmt(BoundExpr expr) => Expr = expr;
    }

    public sealed class BoundBlockStmt : BoundStmt
    {
        public override BoundNodeKind Kind => BoundNodeKind.BlockStmt;
        public ImmutableArray<BoundStmt> Stmts { get; }
        public BoundBlockStmt(ImmutableArray<BoundStmt> stmts) => Stmts = stmts;
    }

    public sealed class BoundVarStmt : BoundStmt
    {
        public override BoundNodeKind Kind => BoundNodeKind.VarStmt;
        public VariableSymbol Variable { get; }
        public BoundExpr Value { get; }
        public BoundVarStmt(VariableSymbol variable, BoundExpr value)
        {
            Variable = variable;
            Value = value;
        }
    }
}
