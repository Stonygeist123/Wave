using System.Collections.Immutable;
using Wave.Symbols;

namespace Wave.Source.Binding.BoundNodes
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

        public override BoundNodeKind Kind => BoundNodeKind.IfStmt;
        public BoundExpr Condition { get; }
        public BoundStmt ThenBranch { get; }
        public BoundStmt? ElseClause { get; }
    }

    public abstract class BoundLoopStmt : BoundStmt
    {
        public BoundLoopStmt(BoundStmt body, LabelSymbol bodyLabel, LabelSymbol breakLabel, LabelSymbol continueLabel)
        {
            Body = body;
            BodyLabel = bodyLabel;
            BreakLabel = breakLabel;
            ContinueLabel = continueLabel;
        }

        public override BoundNodeKind Kind => BoundNodeKind.LoopStmt;
        public LabelSymbol BreakLabel { get; }
        public LabelSymbol ContinueLabel { get; }
        public LabelSymbol BodyLabel { get; }
        public BoundStmt Body { get; }
    }

    public sealed class BoundWhileStmt : BoundLoopStmt
    {
        public BoundWhileStmt(BoundExpr condition, BoundStmt body, LabelSymbol bodyLabel, LabelSymbol breakLabel, LabelSymbol continueLabel)
            : base(body, bodyLabel, breakLabel, continueLabel) => Condition = condition;
        public override BoundNodeKind Kind => BoundNodeKind.WhileStmt;
        public BoundExpr Condition { get; }
    }

    public sealed class BoundDoWhileStmt : BoundLoopStmt
    {
        public BoundDoWhileStmt(BoundStmt body, BoundExpr condition, LabelSymbol bodyLabel, LabelSymbol breakLabel, LabelSymbol continueLabel)
            : base(body, bodyLabel, breakLabel, continueLabel) => Condition = condition;

        public override BoundNodeKind Kind => BoundNodeKind.DoWhileStmt;
        public BoundExpr Condition { get; }
    }

    public sealed class BoundForStmt : BoundLoopStmt
    {
        public BoundForStmt(VariableSymbol variable, BoundExpr lowerBound, BoundExpr upperBound, BoundStmt body, LabelSymbol bodyLabel, LabelSymbol breakLabel, LabelSymbol continueLabel)
            : base(body, bodyLabel, breakLabel, continueLabel)
        {
            Variable = variable;
            LowerBound = lowerBound;
            UpperBound = upperBound;
        }

        public override BoundNodeKind Kind => BoundNodeKind.ForStmt;

        public VariableSymbol Variable { get; }
        public BoundExpr LowerBound { get; }
        public BoundExpr UpperBound { get; }
    }

    public sealed class BoundForEachStmt : BoundLoopStmt
    {
        public BoundForEachStmt(VariableSymbol variable, VariableSymbol? index, BoundExpr array, BoundStmt body, LabelSymbol bodyLabel, LabelSymbol breakLabel, LabelSymbol continueLabel)
            : base(body, bodyLabel, breakLabel, continueLabel)
        {
            Variable = variable;
            Index = index;
            Array = array;
        }

        public override BoundNodeKind Kind => BoundNodeKind.ForEachStmt;
        public VariableSymbol Variable { get; }
        public VariableSymbol? Index { get; }
        public BoundExpr Array { get; }
    }

    public sealed class BoundLabelStmt : BoundStmt
    {
        public BoundLabelStmt(LabelSymbol label) => Label = label;
        public override BoundNodeKind Kind => BoundNodeKind.LabelStmt;
        public LabelSymbol Label { get; }
    }

    public sealed class BoundGotoStmt : BoundStmt
    {
        public BoundGotoStmt(LabelSymbol label) => Label = label;
        public override BoundNodeKind Kind => BoundNodeKind.GotoStmt;
        public LabelSymbol Label { get; }
    }

    public sealed class BoundCondGotoStmt : BoundStmt
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

    public sealed class BoundRetStmt : BoundStmt
    {
        public BoundRetStmt(BoundExpr? value) => Value = value;
        public override BoundNodeKind Kind => BoundNodeKind.RetStmt;
        public BoundExpr? Value { get; }
    }

    public sealed class BoundErrorStmt : BoundStmt
    {
        public BoundErrorStmt() { }
        public override BoundNodeKind Kind => BoundNodeKind.ErrorStmt;
    }
}
