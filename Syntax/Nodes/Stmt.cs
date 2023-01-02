using System.Collections.Immutable;

namespace Wave.Nodes
{
    public abstract class StmtNode : Node { }

    public sealed class BlockStmt : StmtNode
    {
        public BlockStmt(Token lbrace, ImmutableArray<StmtNode> stmts, Token rBrace)
        {
            Lbrace = lbrace;
            Stmts = stmts;
            RBrace = rBrace;
        }

        public override SyntaxKind Kind => SyntaxKind.BlockStmt;
        public Token Lbrace { get; }
        public ImmutableArray<StmtNode> Stmts { get; }
        public Token RBrace { get; }
    }

    public sealed class ExpressionStmt : StmtNode
    {
        public ExpressionStmt(ExprNode expr, Token semicolon)
        {
            Expr = expr;
            Semicolon = semicolon;
        }

        public override SyntaxKind Kind => SyntaxKind.ExpressionStmt;
        public ExprNode Expr { get; }
        public Token Semicolon { get; }
    }

    public sealed class VarStmt : StmtNode
    {
        public override SyntaxKind Kind => SyntaxKind.VarStmt;

        public Token Keyword { get; }
        public Token? MutKeyword { get; }
        public Token Name { get; }
        public Token EqToken { get; }
        public ExprNode Value { get; }
        public Token Semicolon { get; }
        public VarStmt(Token keyword, Token? mutKeyword, Token name, Token eqToken, ExprNode value, Token semicolon)
        {
            Keyword = keyword;
            MutKeyword = mutKeyword;
            Name = name;
            EqToken = eqToken;
            Value = value;
            Semicolon = semicolon;
        }
    }
}
