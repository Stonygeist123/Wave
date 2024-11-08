﻿using System.Collections.Immutable;
using Wave.Source.Binding.BoundNodes;

namespace Wave.Source.Binding
{
    public abstract class BoundTreeRewriter
    {
        public virtual BoundStmt RewriteStmt(BoundStmt node) => node.Kind switch
        {
            BoundNodeKind.ExpressionStmt => RewriteExpressionStmt((BoundExpressionStmt)node),
            BoundNodeKind.BlockStmt => RewriteBlockStmt((BoundBlockStmt)node),
            BoundNodeKind.VarStmt => RewriteVarStmt((BoundVarStmt)node),
            BoundNodeKind.IfStmt => RewriteIfStmt((BoundIfStmt)node),
            BoundNodeKind.WhileStmt => RewriteWhileStmt((BoundWhileStmt)node),
            BoundNodeKind.DoWhileStmt => RewriteDoWhileStmt((BoundDoWhileStmt)node),
            BoundNodeKind.ForStmt => RewriteForStmt((BoundForStmt)node),
            BoundNodeKind.ForEachStmt => RewriteForEachStmt((BoundForEachStmt)node),
            BoundNodeKind.LabelStmt => RewriteLabelStmt((BoundLabelStmt)node),
            BoundNodeKind.GotoStmt => RewriteGotoStmt((BoundGotoStmt)node),
            BoundNodeKind.CondGotoStmt => RewriteCondGotoStmt((BoundCondGotoStmt)node),
            BoundNodeKind.RetStmt => RewriteRetStmt((BoundRetStmt)node),
            _ => throw new Exception($"Unexpected node to lower: \"{node.Kind}\"."),
        };

        public virtual BoundExpr RewriteExpr(BoundExpr node)
        {
            return node.Kind switch
            {
                BoundNodeKind.LiteralExpr => RewriteLiteralExpr((BoundLiteral)node),
                BoundNodeKind.UnaryExpr => RewriteUnaryExpr((BoundUnary)node),
                BoundNodeKind.BinaryExpr => RewriteBinaryExpr((BoundBinary)node),
                BoundNodeKind.NameExpr => RewriteNameExpr((BoundName)node),
                BoundNodeKind.AssignmentExpr => RewriteAssignmentExpr((BoundAssignment)node),
                BoundNodeKind.ArrayAssignmentExpr => RewriteArrayAssignmentExpr((BoundArrayAssignment)node),
                BoundNodeKind.CallExpr => RewriteCallExpr((BoundCall)node),
                BoundNodeKind.ArrayExpr => RewriteArrayExpr((BoundArray)node),
                BoundNodeKind.EnumIndexingExpr => RewriteEnumIndexingExpr((BoundEnumIndexing)node),
                BoundNodeKind.IndexingExpr => RewriteIndexingExpr((BoundIndexing)node),
                BoundNodeKind.InstanceExpr => RewriteInstanceExpr((BoundInstance)node),
                BoundNodeKind.EnumGetExpr => RewriteEnumGetExpr((BoundEnumGet)node),
                BoundNodeKind.GetExpr => RewriteGetExpr((BoundGet)node),
                BoundNodeKind.MethodExpr => RewriteMethodExpr((BoundMethod)node),
                BoundNodeKind.SetExpr => RewriteSetExpr((BoundSet)node),
                BoundNodeKind.ConversionExpr => RewriteConversionExpr((BoundConversion)node),
                BoundNodeKind.ErrorExpr => RewriteErrorExpr((BoundError)node),
                _ => throw new Exception($"Unexpected node to lower: \"{node.Kind}\"."),
            };
        }

        protected virtual BoundStmt RewriteExpressionStmt(BoundExpressionStmt node)
        {
            BoundExpr expr = RewriteExpr(node.Expr);
            if (expr == node.Expr)
                return node;

            return new BoundExpressionStmt(expr);

        }
        protected virtual BoundStmt RewriteBlockStmt(BoundBlockStmt node)
        {
            ImmutableArray<BoundStmt>.Builder? builder = null;
            for (int i = 0; i < node.Stmts.Length; i++)
            {
                BoundStmt stmt = node.Stmts[i];
                BoundStmt oldStmt = stmt;
                BoundStmt newStmt = RewriteStmt(oldStmt);
                if (newStmt != oldStmt)
                {
                    if (builder is null)
                    {
                        builder = ImmutableArray.CreateBuilder<BoundStmt>(node.Stmts.Length);
                        for (int j = 0; j < i; ++j)
                            builder.Add(node.Stmts[j]);
                    }
                }

                builder?.Add(newStmt);
            }

            if (builder is null)
                return node;

            return new BoundBlockStmt(builder.ToImmutable());
        }

        protected virtual BoundStmt RewriteVarStmt(BoundVarStmt node)
        {
            BoundExpr value = RewriteExpr(node.Value);
            if (value == node.Value)
                return node;

            return new BoundVarStmt(node.Variable, value);
        }
        protected virtual BoundStmt RewriteIfStmt(BoundIfStmt node)
        {
            BoundExpr condition = RewriteExpr(node.Condition);
            BoundStmt thenBranch = RewriteStmt(node.ThenBranch);
            BoundStmt? elseClause = node.ElseClause is not null ? RewriteStmt(node.ElseClause) : null;
            if (condition == node.Condition && thenBranch == node.ThenBranch && elseClause == node.ElseClause)
                return node;

            return new BoundIfStmt(condition, thenBranch, elseClause);
        }

        protected virtual BoundStmt RewriteWhileStmt(BoundWhileStmt node)
        {
            BoundExpr condition = RewriteExpr(node.Condition);
            BoundStmt stmt = RewriteStmt(node.Body);
            if (condition == node.Condition && stmt == node.Body)
                return node;

            return new BoundWhileStmt(condition, stmt, node.BodyLabel, node.BreakLabel, node.ContinueLabel);
        }

        protected virtual BoundStmt RewriteDoWhileStmt(BoundDoWhileStmt node)
        {
            BoundStmt stmt = RewriteStmt(node.Body);
            BoundExpr condition = RewriteExpr(node.Condition);
            if (stmt == node.Body && condition == node.Condition)
                return node;

            return new BoundDoWhileStmt(stmt, condition, node.BodyLabel, node.BreakLabel, node.ContinueLabel);
        }

        protected virtual BoundStmt RewriteForStmt(BoundForStmt node)
        {
            BoundExpr lowerBound = RewriteExpr(node.LowerBound);
            BoundExpr upperBound = RewriteExpr(node.UpperBound);
            BoundStmt stmt = RewriteStmt(node.Body);
            if (lowerBound == node.LowerBound && upperBound == node.UpperBound && stmt == node.Body)
                return node;

            return new BoundForStmt(node.Variable, lowerBound, upperBound, stmt, node.BodyLabel, node.BreakLabel, node.ContinueLabel);
        }

        protected virtual BoundStmt RewriteForEachStmt(BoundForEachStmt node)
        {
            BoundExpr array = RewriteExpr(node.Array);
            BoundStmt stmt = RewriteStmt(node.Body);
            if (array == node.Array && stmt == node.Body)
                return node;

            return new BoundForEachStmt(node.Variable, node.Index, array, stmt, node.BodyLabel, node.BreakLabel, node.ContinueLabel);
        }

        protected virtual BoundStmt RewriteLabelStmt(BoundLabelStmt node) => node;
        protected virtual BoundStmt RewriteGotoStmt(BoundGotoStmt node) => node;
        protected virtual BoundStmt RewriteCondGotoStmt(BoundCondGotoStmt node)
        {
            BoundExpr condition = RewriteExpr(node.Condition);
            if (condition == node.Condition)
                return node;

            return new BoundCondGotoStmt(node.Label, condition, node.JumpIfTrue);
        }

        protected virtual BoundStmt RewriteRetStmt(BoundRetStmt node)
        {
            if (node.Value is null)
                return node;

            BoundExpr value = RewriteExpr(node.Value);
            if (value == node.Value)
                return node;

            return new BoundRetStmt(value);
        }

        protected virtual BoundExpr RewriteLiteralExpr(BoundLiteral node) => node;
        protected virtual BoundExpr RewriteUnaryExpr(BoundUnary node)
        {
            BoundExpr operand = RewriteExpr(node.Operand);
            if (operand == node.Operand)
                return node;

            return new BoundUnary(node.Op, operand);
        }

        protected virtual BoundExpr RewriteBinaryExpr(BoundBinary node)
        {
            BoundExpr left = RewriteExpr(node.Left);
            BoundExpr right = RewriteExpr(node.Right);
            if (left == node.Left && right == node.Right)
                return node;

            return new BoundBinary(left, node.Op, right);
        }

        protected virtual BoundExpr RewriteNameExpr(BoundName node) => node;
        protected virtual BoundExpr RewriteAssignmentExpr(BoundAssignment node)
        {
            BoundExpr value = RewriteExpr(node.Value);
            if (value == node.Value)
                return node;

            return new BoundAssignment(node.Variable, value);
        }

        protected virtual BoundExpr RewriteArrayAssignmentExpr(BoundArrayAssignment node)
        {
            BoundExpr index = RewriteExpr(node.Index);
            BoundExpr value = RewriteExpr(node.Value);
            if (index == node.Index && value == node.Value)
                return node;

            return new BoundArrayAssignment(node.Variable, index, value);
        }

        protected virtual BoundExpr RewriteCallExpr(BoundCall node)
        {
            ImmutableArray<BoundExpr>.Builder? builder = null;
            for (int i = 0; i < node.Args.Length; i++)
            {
                BoundExpr stmt = node.Args[i];
                BoundExpr oldExpr = stmt;
                BoundExpr newExpr = RewriteExpr(oldExpr);
                if (newExpr != oldExpr)
                {
                    if (builder is null)
                    {
                        builder = ImmutableArray.CreateBuilder<BoundExpr>(node.Args.Length);
                        for (int j = 0; j < i; ++j)
                            builder.Add(node.Args[j]);
                    }
                }

                builder?.Add(newExpr);
            }

            if (builder is null)
                return node;

            return new BoundCall(node.Function, builder.MoveToImmutable());
        }

        protected virtual BoundExpr RewriteArrayExpr(BoundArray node)
        {
            ImmutableArray<BoundExpr>.Builder? builder = null;
            for (int i = 0; i < node.Elements.Length; i++)
            {
                BoundExpr stmt = node.Elements[i];
                BoundExpr oldExpr = stmt;
                BoundExpr newExpr = RewriteExpr(oldExpr);
                if (newExpr != oldExpr)
                {
                    if (builder is null)
                    {
                        builder = ImmutableArray.CreateBuilder<BoundExpr>(node.Elements.Length);
                        for (int j = 0; j < i; ++j)
                            builder.Add(node.Elements[j]);
                    }
                }

                builder?.Add(newExpr);
            }

            if (builder is null)
                return node;

            return new BoundArray(builder.MoveToImmutable(), node.Type);
        }

        protected virtual BoundExpr RewriteInstanceExpr(BoundInstance node) => node;
        protected virtual BoundExpr RewriteIndexingExpr(BoundIndexing node)
        {
            BoundExpr expr = RewriteExpr(node.Expr);
            BoundExpr index = RewriteExpr(node.Index);
            if (index == node.Index && expr == node.Expr)
                return node;

            return new BoundIndexing(expr, index);
        }

        protected virtual BoundExpr RewriteEnumIndexingExpr(BoundEnumIndexing node)
        {
            BoundExpr index = RewriteExpr(node.Index);
            if (index == node.Index)
                return node;

            return new BoundEnumIndexing(node.ADT, index);
        }

        protected virtual BoundExpr RewriteEnumGetExpr(BoundEnumGet node) => node;
        protected virtual BoundExpr RewriteGetExpr(BoundGet node) => node;
        protected virtual BoundExpr RewriteMethodExpr(BoundMethod node)
        {
            ImmutableArray<BoundExpr>.Builder? builder = null;
            for (int i = 0; i < node.Args.Length; i++)
            {
                BoundExpr stmt = node.Args[i];
                BoundExpr oldExpr = stmt;
                BoundExpr newExpr = RewriteExpr(oldExpr);
                if (newExpr != oldExpr)
                {
                    if (builder is null)
                    {
                        builder = ImmutableArray.CreateBuilder<BoundExpr>(node.Args.Length);
                        for (int j = 0; j < i; ++j)
                            builder.Add(node.Args[j]);
                    }
                }

                builder?.Add(newExpr);
            }

            if (builder is null)
                return node;
            return new BoundMethod(node.Id, node.Function, builder.MoveToImmutable());
        }

        protected virtual BoundExpr RewriteSetExpr(BoundSet node)
        {
            BoundExpr v = RewriteExpr(node.Value);
            if (v == node.Value)
                return node;
            return new BoundSet(node.Id, node.Field, node.Value);
        }

        private BoundExpr RewriteConversionExpr(BoundConversion node)
        {
            BoundExpr expr = RewriteExpr(node.Expr);
            if (expr == node.Expr)
                return node;

            return new BoundConversion(node.Type, expr);
        }

        protected virtual BoundExpr RewriteErrorExpr(BoundError node) => node;
    }
}
