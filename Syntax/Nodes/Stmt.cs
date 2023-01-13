using System.Collections.Immutable;
using Wave.Syntax.Nodes;

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
        public ExpressionStmt(ExprNode expr, Token? semicolon)
        {
            Expr = expr;
            Semicolon = semicolon;
        }

        public override SyntaxKind Kind => SyntaxKind.ExpressionStmt;
        public ExprNode Expr { get; }
        public Token? Semicolon { get; }
    }

    public sealed class VarStmt : StmtNode
    {
        public VarStmt(Token keyword, Token? mutKeyword, Token name, TypeClause? typeClause, Token eqToken, ExprNode value, Token semicolon)
        {
            Keyword = keyword;
            MutKeyword = mutKeyword;
            Name = name;
            TypeClause = typeClause;
            EqToken = eqToken;
            Value = value;
            Semicolon = semicolon;
        }

        public override SyntaxKind Kind => SyntaxKind.VarStmt;
        public Token Keyword { get; }
        public Token? MutKeyword { get; }
        public Token Name { get; }
        public TypeClause? TypeClause { get; }
        public Token EqToken { get; }
        public ExprNode Value { get; }
        public Token Semicolon { get; }
    }

    public sealed class IfStmt : StmtNode
    {
        public IfStmt(Token keyword, ExprNode condition, StmtNode thenBranch, ElseClause? elseClause)
        {
            Keyword = keyword;
            Condition = condition;
            ThenBranch = thenBranch;
            ElseClause = elseClause;
        }

        public override SyntaxKind Kind => SyntaxKind.IfStmt;
        public Token Keyword { get; }
        public ExprNode Condition { get; }
        public StmtNode ThenBranch { get; }
        public ElseClause? ElseClause { get; }
    }

    public sealed class WhileStmt : StmtNode
    {
        public WhileStmt(Token keyword, ExprNode condition, StmtNode stmt)
        {
            Keyword = keyword;
            Condition = condition;
            Stmt = stmt;
        }

        public override SyntaxKind Kind => SyntaxKind.ElseClause;
        public Token Keyword { get; }
        public ExprNode Condition { get; }
        public StmtNode Stmt { get; }
    }

    public sealed class DoWhileStmt : StmtNode
    {
        public DoWhileStmt(Token keyword, StmtNode stmt, Token whileKeyword, ExprNode condition)
        {
            Keyword = keyword;
            Stmt = stmt;
            WhileKeyword = whileKeyword;
            Condition = condition;
        }

        public override SyntaxKind Kind => SyntaxKind.ElseClause;
        public Token Keyword { get; }
        public StmtNode Stmt { get; }
        public Token WhileKeyword { get; }
        public ExprNode Condition { get; }
    }

    public sealed class ForStmt : StmtNode
    {
        public ForStmt(Token keyword, Token id, Token eqToken, ExprNode lowerBound, Token toKeyword, ExprNode upperBound, StmtNode stmt)
        {
            Keyword = keyword;
            Id = id;
            EqToken = eqToken;
            LowerBound = lowerBound;
            ToKeyword = toKeyword;
            UpperBound = upperBound;
            Stmt = stmt;
        }

        public override SyntaxKind Kind => SyntaxKind.ForStmt;
        public Token Keyword { get; }
        public Token Id { get; }
        public Token EqToken { get; }
        public ExprNode LowerBound { get; }
        public Token ToKeyword { get; }
        public ExprNode UpperBound { get; }
        public StmtNode Stmt { get; }
    }
}
