using System.Collections.Immutable;
using Wave.Binding;
using Wave.Binding.BoundNodes;
using Wave.Symbols;
using Wave.Syntax.Nodes;

namespace Wave.Lowering
{
    internal sealed class Lowerer : BoundTreeRewriter
    {
        private int _labelCount = 0;
        private Lowerer() { }

        private LabelSymbol GenerateLabel() => new($"Label_{++_labelCount}");

        public static BoundBlockStmt Lower(BoundStmt stmt)
        {
            Lowerer lowerer = new();
            return Flatten(lowerer.RewriteStmt(stmt));
        }

        private static BoundBlockStmt Flatten(BoundStmt stmt)
        {
            ImmutableArray<BoundStmt>.Builder builder = ImmutableArray.CreateBuilder<BoundStmt>();
            Stack<BoundStmt> stack = new();
            stack.Push(stmt);

            while (stack.Count > 0)
            {
                BoundStmt current = stack.Pop();
                if (current is BoundBlockStmt b)
                {
                    foreach (BoundStmt s in b.Stmts.Reverse())
                        stack.Push(s);
                }
                else
                    builder.Add(current);
            }

            return new(builder.ToImmutable());
        }

        protected override BoundStmt RewriteIfStmt(BoundIfStmt node)
        {
            if (node.ElseClause is null)
            {
                LabelSymbol endLabel = GenerateLabel();
                BoundCondGotoStmt gotoFalse = new(endLabel, node.Condition, false);
                BoundLabelStmt endLabelStmt = new(endLabel);
                return RewriteStmt(new BoundBlockStmt(ImmutableArray.Create(gotoFalse, node.ThenBranch, endLabelStmt)));
            }
            else
            {
                LabelSymbol elseLabel = GenerateLabel();
                LabelSymbol endLabel = GenerateLabel();
                BoundCondGotoStmt gotoFalse = new(elseLabel, node.Condition, false);
                BoundGotoStmt gotoEndStmt = new(endLabel);
                BoundLabelStmt elseLabelStmt = new(elseLabel);
                BoundLabelStmt endLabelStmt = new(endLabel);
                return RewriteStmt(new BoundBlockStmt(ImmutableArray.Create(gotoFalse, node.ThenBranch, gotoEndStmt, elseLabelStmt, node.ElseClause, endLabelStmt)));
            }
        }

        protected override BoundStmt RewriteWhileStmt(BoundWhileStmt node)
        {
            LabelSymbol continueLabel = GenerateLabel();
            LabelSymbol checkLabel = GenerateLabel();
            LabelSymbol endLabel = GenerateLabel();

            BoundGotoStmt gotoCheck = new(checkLabel);
            BoundLabelStmt continueLabelStmt = new(continueLabel);
            BoundLabelStmt checkLabelStmt = new(checkLabel);
            BoundCondGotoStmt gotoTrue = new(continueLabel, node.Condition);
            BoundLabelStmt endLabelStmt = new(endLabel);
            return RewriteStmt(new BoundBlockStmt(ImmutableArray.Create(gotoCheck, continueLabelStmt, node.Stmt, checkLabelStmt, gotoTrue, endLabelStmt)));
        }

        protected override BoundStmt RewriteDoWhileStmt(BoundDoWhileStmt node)
        {
            LabelSymbol continueLabel = GenerateLabel();
            BoundLabelStmt continueLabelStmt = new(continueLabel);
            BoundCondGotoStmt gotoTrue = new(continueLabel, node.Condition);
            return RewriteStmt(new BoundBlockStmt(ImmutableArray.Create(continueLabelStmt, node.Stmt, gotoTrue)));
        }

        protected override BoundStmt RewriteForStmt(BoundForStmt node)
        {
            BoundVarStmt varDecl = new(node.Variable, node.LowerBound);
            BoundName varExpr = new(node.Variable);
            LocalVariableSymbol upperBoundSymbol = new("upperBound", TypeSymbol.Int, false);
            BoundVarStmt upperBoundDecl = new(upperBoundSymbol, node.UpperBound);
            BoundBinary condition = new(varExpr, BoundBinOperator.Bind(SyntaxKind.LessEq, TypeSymbol.Int, TypeSymbol.Int)!, new BoundName(upperBoundSymbol));
            BoundExpressionStmt increment = new(new BoundAssignment(
                    node.Variable,
                    new BoundBinary(
                        varExpr,
                        BoundBinOperator.Bind(SyntaxKind.Plus, TypeSymbol.Int, TypeSymbol.Int)!,
                        new BoundLiteral(1))
                    )
                );

            BoundWhileStmt whileStmt = new(condition, new BoundBlockStmt(ImmutableArray.Create<BoundStmt>(node.Stmt, increment)));
            return RewriteStmt(new BoundBlockStmt(ImmutableArray.Create<BoundStmt>(
                varDecl,
                upperBoundDecl,
                whileStmt
                )));
        }
    }
}
