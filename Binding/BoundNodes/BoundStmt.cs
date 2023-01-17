using System.Collections.Immutable;
using Wave.Symbols;

namespace Wave.Binding.BoundNodes
{
    public abstract class BoundStmt : BoundNode { }

    internal sealed class BoundExpressionStmt : BoundStmt
    {
        public BoundExpressionStmt(BoundExpr expr) => Expr = expr;
        public override BoundNodeKind Kind => BoundNodeKind.ExpressionStmt;
        public BoundExpr Expr { get; }
    }

    internal sealed class BoundBlockStmt : BoundStmt
    {
        public BoundBlockStmt(ImmutableArray<BoundStmt> stmts) => Stmts = stmts;
        public override BoundNodeKind Kind => BoundNodeKind.BlockStmt;
        public ImmutableArray<BoundStmt> Stmts { get; }
    }

    internal sealed class BoundVarStmt : BoundStmt
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

    internal sealed class BoundIfStmt : BoundStmt
    {
        public BoundIfStmt(BoundExpr condition, BoundStmt thenBranch, BoundStmt? elseClause)
        {
            Condition = condition;
            ThenBranch = thenBranch;
            ElseClause = elseClause;
        }

        public override BoundNodeKind Kind => BoundNodeKind.IfStmt;
        public BoundExpr Condition { get; }
        public BoundStmt ThenBranch { get; }
        public BoundStmt? ElseClause { get; }
    }

    internal abstract class BoundLoopStmt : BoundStmt
    {
        public BoundLoopStmt(LabelSymbol breakLabel, LabelSymbol continueLabel)
        {
            BreakLabel = breakLabel;
            ContinueLabel = continueLabel;
        }

        public override BoundNodeKind Kind => BoundNodeKind.LoopStmt;
        public LabelSymbol BreakLabel { get; }
        public LabelSymbol ContinueLabel { get; }
    }

    internal sealed class BoundWhileStmt : BoundLoopStmt
    {
        public BoundWhileStmt(BoundExpr condition, BoundStmt stmt, LabelSymbol breakLabel, LabelSymbol continueLabel)
            : base(breakLabel, continueLabel)
        {
            Condition = condition;
            Stmt = stmt;
        }

        public override BoundNodeKind Kind => BoundNodeKind.WhileStmt;
        public BoundExpr Condition { get; }
        public BoundStmt Stmt { get; }
    }

    internal sealed class BoundDoWhileStmt : BoundLoopStmt
    {
        public BoundDoWhileStmt(BoundStmt stmt, BoundExpr condition, LabelSymbol breakLabel, LabelSymbol continueLabel)
            : base(breakLabel, continueLabel)
        {
            Stmt = stmt;
            Condition = condition;
        }

        public override BoundNodeKind Kind => BoundNodeKind.DoWhileStmt;
        public BoundStmt Stmt { get; }
        public BoundExpr Condition { get; }
    }

    internal sealed class BoundForStmt : BoundLoopStmt
    {
        public BoundForStmt(VariableSymbol variable, BoundExpr lowerBound, BoundExpr upperBound, BoundStmt stmt, LabelSymbol breakLabel, LabelSymbol continueLabel)
            : base(breakLabel, continueLabel)
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

    internal sealed class BoundLabelStmt : BoundStmt
    {
        public BoundLabelStmt(LabelSymbol label) => Label = label;
        public override BoundNodeKind Kind => BoundNodeKind.LabelStmt;
        public LabelSymbol Label { get; }
    }

    internal sealed class BoundGotoStmt : BoundStmt
    {
        public BoundGotoStmt(LabelSymbol label) => Label = label;
        public override BoundNodeKind Kind => BoundNodeKind.GotoStmt;
        public LabelSymbol Label { get; }
    }

    internal sealed class BoundCondGotoStmt : BoundStmt
    {
        public BoundCondGotoStmt(LabelSymbol label, BoundExpr condition, bool jumpIfTrue = true)
        {
            Label = label;
            Condition = condition;
            JumpIfTrue = jumpIfTrue;
        }

        public override BoundNodeKind Kind => BoundNodeKind.CondGotoStmt;
        public LabelSymbol Label { get; }
        public BoundExpr Condition { get; }
        public bool JumpIfTrue { get; }
    }

    internal sealed class BoundErrorStmt : BoundStmt
    {
        public BoundErrorStmt() { }
        public override BoundNodeKind Kind => BoundNodeKind.ErrorStmt;
    }
}
