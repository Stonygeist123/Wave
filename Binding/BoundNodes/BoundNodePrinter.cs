using System.CodeDom.Compiler;
using Wave.IO;
using Wave.Nodes;
using Wave.Symbols;
using Wave.Syntax;

namespace Wave.Binding.BoundNodes
{
    public static class BoundNodePrinter
    {
        public static void WriteTo(this BoundNode node, TextWriter writer)
        {
            if (writer is IndentedTextWriter iw)
                WriteTo(node, iw);
            else
                WriteTo(node, new IndentedTextWriter(writer));
        }

        public static void WriteTo(this BoundNode node, IndentedTextWriter writer)
        {
            switch (node.Kind)
            {
                case BoundNodeKind.BlockStmt:
                    WriteBlockStmt((BoundBlockStmt)node, writer);
                    break;
                case BoundNodeKind.VarStmt:
                    WriteVarStmt((BoundVarStmt)node, writer);
                    break;
                case BoundNodeKind.IfStmt:
                    WriteIfStmt((BoundIfStmt)node, writer);
                    break;
                case BoundNodeKind.WhileStmt:
                    WriteWhileStmt((BoundWhileStmt)node, writer);
                    break;
                case BoundNodeKind.DoWhileStmt:
                    WriteDoWhileStmt((BoundDoWhileStmt)node, writer);
                    break;
                case BoundNodeKind.ForStmt:
                    WriteForStmt((BoundForStmt)node, writer);
                    break;
                case BoundNodeKind.LabelStmt:
                    WriteLabelStmt((BoundLabelStmt)node, writer);
                    break;
                case BoundNodeKind.GotoStmt:
                    WriteGotoStmt((BoundGotoStmt)node, writer);
                    break;
                case BoundNodeKind.CondGotoStmt:
                    WriteCondGotoStmt((BoundCondGotoStmt)node, writer);
                    break;
                case BoundNodeKind.ExpressionStmt:
                    WriteExprStmt((BoundExpressionStmt)node, writer);
                    break;
                case BoundNodeKind.ErrorExpr:
                    WriteErrorExpr(writer);
                    break;
                case BoundNodeKind.LiteralExpr:
                    WriteLiteralExpr((BoundLiteral)node, writer);
                    break;
                case BoundNodeKind.NameExpr:
                    WriteVariableExpr((BoundName)node, writer);
                    break;
                case BoundNodeKind.AssignmentExpr:
                    WriteAssignmentExpr((BoundAssignment)node, writer);
                    break;
                case BoundNodeKind.UnaryExpr:
                    WriteUnaryExpr((BoundUnary)node, writer);
                    break;
                case BoundNodeKind.BinaryExpr:
                    WriteBinaryExpr((BoundBinary)node, writer);
                    break;
                case BoundNodeKind.CallExpr:
                    WriteCallExpr((BoundCall)node, writer);
                    break;
                case BoundNodeKind.ConversionExpr:
                    WriteConversionExpr((BoundConversion)node, writer);
                    break;
                default:
                    throw new Exception($"Unexpected node {node.Kind}");
            }
        }

        private static void WriteNestedStmt(this IndentedTextWriter writer, BoundStmt stmt)
        {
            bool needsIndentation = stmt is not BoundBlockStmt;
            if (needsIndentation)
                ++writer.Indent;

            stmt.WriteTo(writer);
            if (needsIndentation)
                --writer.Indent;
        }

        private static void WriteNestedExpr(this IndentedTextWriter writer, ushort precedence, BoundExpr expr)
        {
            if (expr is BoundUnary un)
                writer.WriteNestedExpr(precedence, un.Op.SyntaxKind.GetUnOpPrecedence(), un);
            else if (expr is BoundBinary bin)
                writer.WriteNestedExpr(precedence, bin.Op.SyntaxKind.GetUnOpPrecedence(), bin);
            else
                expr.WriteTo(writer);
        }

        private static void WriteNestedExpr(this IndentedTextWriter writer, ushort parentPrecedence, ushort curPrecedence, BoundExpr expr)
        {
            bool needsParens = parentPrecedence >= curPrecedence;
            if (needsParens)
                writer.WritePunctuation("(");

            expr.WriteTo(writer);
            if (needsParens)
                writer.WritePunctuation(")");
        }

        private static void WriteBlockStmt(BoundBlockStmt node, IndentedTextWriter writer)
        {
            writer.WritePunctuation(SyntaxKind.LBrace);
            writer.WriteLine();
            ++writer.Indent;

            foreach (BoundStmt s in node.Stmts)
                s.WriteTo(writer);

            --writer.Indent;
            writer.WritePunctuation(SyntaxKind.RBrace);
            writer.WriteLine();
        }

        private static void WriteVarStmt(BoundVarStmt node, IndentedTextWriter writer)
        {
            writer.WriteKeyword(SyntaxKind.Var);
            if (node.Variable.IsMut)
            {
                writer.WriteSpace();
                writer.WriteKeyword(SyntaxKind.Mut);
            }

            writer.WriteSpace();
            writer.WriteIdentifier(node.Variable.Name);
            writer.WriteSpace();
            writer.WritePunctuation(SyntaxKind.Eq);
            writer.WriteSpace();
            node.Value.WriteTo(writer);
            writer.WriteLine();
        }

        private static void WriteIfStmt(BoundIfStmt node, IndentedTextWriter writer)
        {
            writer.WriteKeyword(SyntaxKind.If);
            writer.WriteSpace();
            node.Condition.WriteTo(writer);
            writer.WriteLine();
            writer.WriteNestedStmt(node.ThenBranch);

            if (node.ElseClause is not null)
            {
                writer.WriteKeyword(SyntaxKind.Else);
                writer.WriteLine();
                writer.WriteNestedStmt(node.ElseClause);
            }
        }

        private static void WriteWhileStmt(BoundWhileStmt node, IndentedTextWriter writer)
        {
            writer.WriteKeyword(SyntaxKind.While);
            writer.WriteSpace();
            node.Condition.WriteTo(writer);
            writer.WriteLine();
            writer.WriteNestedStmt(node.Stmt);
        }

        private static void WriteDoWhileStmt(BoundDoWhileStmt node, IndentedTextWriter writer)
        {
            writer.WriteKeyword(SyntaxKind.Do);
            writer.WriteLine();
            writer.WriteNestedStmt(node.Stmt);
            writer.WriteKeyword(SyntaxKind.While);
            writer.WriteSpace();
            node.Condition.WriteTo(writer);
            writer.WriteLine();
        }

        private static void WriteForStmt(BoundForStmt node, IndentedTextWriter writer)
        {
            writer.WriteKeyword(SyntaxKind.For);
            writer.WriteSpace();
            writer.WriteIdentifier(node.Variable.Name);
            writer.WriteSpace();
            writer.WritePunctuation(SyntaxKind.Eq);
            writer.WriteSpace();
            node.LowerBound.WriteTo(writer);
            writer.WriteSpace();
            writer.WriteKeyword(SyntaxKind.Arrow);
            writer.WriteSpace();
            node.UpperBound.WriteTo(writer);
            writer.WriteLine();
            writer.WriteNestedStmt(node.Stmt);
        }

        private static void WriteLabelStmt(BoundLabelStmt node, IndentedTextWriter writer)
        {
            bool unindent = writer.Indent > 0;
            if (unindent)
                --writer.Indent;

            writer.WritePunctuation(node.Label.Name);
            writer.WritePunctuation(SyntaxKind.Colon);
            writer.WriteLine();
            if (unindent)
                ++writer.Indent;
        }

        private static void WriteGotoStmt(BoundGotoStmt node, IndentedTextWriter writer)
        {
            writer.WriteKeyword("goto");
            writer.WriteSpace();
            writer.WriteIdentifier(node.Label.Name);
            writer.WriteLine();
        }

        private static void WriteCondGotoStmt(BoundCondGotoStmt node, IndentedTextWriter writer)
        {
            writer.WriteKeyword("goto");
            writer.WriteSpace();
            writer.WriteIdentifier(node.Label.Name);
            writer.WriteSpace();
            writer.WriteKeyword(node.JumpIfTrue ? "if" : "unless");
            writer.WriteSpace();
            node.Condition.WriteTo(writer);
            writer.WriteLine();
        }

        private static void WriteExprStmt(BoundExpressionStmt node, IndentedTextWriter writer)
        {
            node.Expr.WriteTo(writer);
            writer.WriteLine();
        }

        private static void WriteErrorExpr(IndentedTextWriter writer) => writer.WriteKeyword("?");
        private static void WriteLiteralExpr(BoundLiteral node, IndentedTextWriter writer)
        {
            string value = node.Value.ToString()!;
            if (node.Type == TypeSymbol.Bool || node.Type == TypeSymbol.Int || node.Type == TypeSymbol.Float)
                writer.WriteLiteral(value);
            else if (node.Type == TypeSymbol.String)
            {
                writer.WriteString($"\"{value.Replace("\"", "\"\"")}\"");
            }
            else
                throw new Exception($"Unexpected type {node.Type}");
        }

        private static void WriteVariableExpr(BoundName node, IndentedTextWriter writer) => writer.WriteIdentifier(node.Variable.Name);
        private static void WriteAssignmentExpr(BoundAssignment node, IndentedTextWriter writer)
        {
            writer.WriteIdentifier(node.Variable.Name);
            writer.WriteSpace();
            writer.WritePunctuation(SyntaxKind.Eq);
            writer.WriteSpace();
            node.Value.WriteTo(writer);
        }

        private static void WriteUnaryExpr(BoundUnary node, IndentedTextWriter writer)
        {
            ushort precedence = node.Op.SyntaxKind.GetUnOpPrecedence();
            writer.WritePunctuation(node.Op.SyntaxKind);
            writer.WriteNestedExpr(precedence, node.Operand);
        }

        private static void WriteBinaryExpr(BoundBinary node, IndentedTextWriter writer)
        {
            ushort precedence = node.Op.SyntaxKind.GetBinOpPrecedence();
            writer.WriteNestedExpr(precedence, node.Left);
            writer.WriteSpace();
            writer.WritePunctuation(node.Op.SyntaxKind);
            writer.WriteSpace();
            writer.WriteNestedExpr(precedence, node.Right);
        }

        private static void WriteCallExpr(BoundCall node, IndentedTextWriter writer)
        {
            writer.WriteIdentifier(node.Function.Name);
            writer.WritePunctuation(SyntaxKind.LParen);

            bool isFirst = true;
            foreach (BoundExpr? argument in node.Args)
            {
                if (isFirst)
                    isFirst = false;
                else
                {
                    writer.WritePunctuation(SyntaxKind.Comma);
                    writer.WriteSpace();
                }

                argument.WriteTo(writer);
            }

            writer.WritePunctuation(SyntaxKind.RParen);
        }

        private static void WriteConversionExpr(BoundConversion node, IndentedTextWriter writer)
        {
            writer.WriteIdentifier(node.Type.Name);
            writer.WritePunctuation(SyntaxKind.LParen);
            node.Expr.WriteTo(writer);
            writer.WritePunctuation(SyntaxKind.RParen);
        }
    }
}
