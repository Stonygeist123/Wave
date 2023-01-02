using System.Collections.Immutable;

namespace Wave.Binding.BoundNodes
{
    public abstract class BoundStmt : BoundNode { }

    public sealed class BoundExpressionStmt : BoundStmt
    {
        public BoundExpressionStmt(BoundExpr expr) => Expr = expr;
        public override BoundNodeKind Kind => BoundNodeKind.ExpressionStmt;
        public BoundExpr Expr { get; }
    }

    public sealed class BoundBlockStmt : BoundStmt
    {
        public BoundBlockStmt(ImmutableArray<BoundStmt> stmts) => Stmts = stmts;
        public override BoundNodeKind Kind => BoundNodeKind.BlockStmt;
        public ImmutableArray<BoundStmt> Stmts { get; }
    }

    public sealed class BoundVarStmt : BoundStmt
    {
        public BoundVarStmt(VariableSymbol variable, BoundExpr value)
        {
            Variable = variable;
            Value = value;
        }

        public override BoundNodeKind Kind => BoundNodeKind.VarStmt;
        public VariableSymbol Variable { get; }
        public BoundExpr Value { get; }
    }

    public sealed class BoundIfStmt : BoundStmt
    {
        public BoundIfStmt(BoundExpr condition, BoundStmt thenBranch, BoundStmt? elseClause)
        {
            Condition = condition;
            ThenBranch = thenBranch;
            ElseClause = elseClause;
        }

        public override BoundNodeKind Kind => BoundNodeKind.VarStmt;
        public BoundExpr Condition { get; }
        public BoundStmt ThenBranch { get; }
        public BoundStmt? ElseClause { get; }
    }

    public sealed class BoundWhileStmt : BoundStmt
    {
        public BoundWhileStmt(BoundExpr condition, BoundStmt stmt)
        {
            Condition = condition;
            Stmt = stmt;
        }

        public override BoundNodeKind Kind => BoundNodeKind.VarStmt;
        public BoundExpr Condition { get; }
        public BoundStmt Stmt { get; }
    }

    public sealed class BoundForStmt : BoundStmt
    {
        public BoundForStmt(VariableSymbol variable, BoundExpr lowerBound, BoundExpr upperBound, BoundStmt stmt)
        {
            Variable = variable;
            LowerBound = lowerBound;
            UpperBound = upperBound;
            Stmt = stmt;
        }

        public override BoundNodeKind Kind => BoundNodeKind.ForStmt;

        public VariableSymbol Variable { get; }
        public BoundExpr LowerBound { get; }
        public BoundExpr UpperBound { get; }
        public BoundStmt Stmt { get; }
    }
}
