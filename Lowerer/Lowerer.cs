﻿using System.Collections.Immutable;
using Wave.Binding;
using Wave.Binding.BoundNodes;
using Wave.Nodes;
using Wave.Symbols;

namespace Wave.Lowering
{
    internal sealed class Lowerer : BoundTreeRewriter
    {
        private int _labelCount = 0;
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
            BoundGotoStmt gotoContinue = new(node.ContinueLabel);
            BoundLabelStmt bodyLabelStmt = new(node.BodyLabel);
            BoundLabelStmt continueLabelStmt = new(node.ContinueLabel);
            BoundCondGotoStmt gotoTrue = new(node.BodyLabel, node.Condition);
            BoundLabelStmt breakLabelStmt = new(node.BreakLabel);
            return RewriteStmt(new BoundBlockStmt(ImmutableArray.Create(gotoContinue, bodyLabelStmt, node.Body, continueLabelStmt, gotoTrue, breakLabelStmt)));
        }

        protected override BoundStmt RewriteDoWhileStmt(BoundDoWhileStmt node)
        {
            BoundLabelStmt bodyLabelStmt = new(node.BodyLabel);
            BoundLabelStmt continueLabelStmt = new(node.ContinueLabel);
            BoundCondGotoStmt gotoTrue = new(node.BodyLabel, node.Condition);
            BoundLabelStmt breakLabelStmt = new(node.BreakLabel);
            return RewriteStmt(new BoundBlockStmt(ImmutableArray.Create(bodyLabelStmt, node.Body, continueLabelStmt, gotoTrue, breakLabelStmt)));
        }

        protected override BoundStmt RewriteForStmt(BoundForStmt node)
        {
            BoundVarStmt varDecl = new(node.Variable, node.LowerBound);
            BoundName varExpr = new(node.Variable);
            LocalVariableSymbol upperBoundSymbol = new("upperBound", TypeSymbol.Int, false);
            BoundVarStmt upperBoundDecl = new(upperBoundSymbol, node.UpperBound);
            BoundBinary condition = new(varExpr, BoundBinOperator.Bind(SyntaxKind.LessEq, TypeSymbol.Int, TypeSymbol.Int)!, new BoundName(upperBoundSymbol));
            BoundLabelStmt continueLabelStmt = new(node.ContinueLabel);
            BoundExpressionStmt increment = new(new BoundAssignment(
                    node.Variable,
                    new BoundBinary(
                        varExpr,
                        BoundBinOperator.Bind(SyntaxKind.Plus, TypeSymbol.Int, TypeSymbol.Int)!,
                        new BoundLiteral(1))
                    )
                );

            BoundBlockStmt whileBody = new(ImmutableArray.Create<BoundStmt>(node.Body, continueLabelStmt, increment));
            BoundWhileStmt whileStmt = new(condition, whileBody, node.BodyLabel, node.BreakLabel, GenerateLabel());
            return RewriteStmt(new BoundBlockStmt(ImmutableArray.Create<BoundStmt>(
                varDecl,
                upperBoundDecl,
                whileStmt
                )));
        }
    }
}
