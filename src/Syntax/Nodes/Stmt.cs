using System.Collections.Immutable;

namespace Wave.Source.Syntax.Nodes
{
    public abstract class StmtNode : Node
    {
        public StmtNode(SyntaxTree syntaxTree) : base(syntaxTree) { }
    }

    public sealed class BlockStmt : StmtNode
    {
        public BlockStmt(SyntaxTree syntaxTree, Token lbrace, ImmutableArray<StmtNode> stmts, Token rBrace)
            : base(syntaxTree)
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
        public ExpressionStmt(SyntaxTree syntaxTree, ExprNode expr, Token? semicolon)
            : base(syntaxTree)
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
        public VarStmt(SyntaxTree syntaxTree, Token keyword, Token? mutKeyword, Token name, TypeClause? typeClause, Token eqToken, ExprNode value, Token semicolon)
            : base(syntaxTree)
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
        public IfStmt(SyntaxTree syntaxTree, Token keyword, ExprNode condition, StmtNode thenBranch, ElseClause? elseClause)
            : base(syntaxTree)
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
        public WhileStmt(SyntaxTree syntaxTree, Token keyword, ExprNode condition, StmtNode stmt)
            : base(syntaxTree)
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
        public DoWhileStmt(SyntaxTree syntaxTree, Token keyword, StmtNode stmt, Token whileKeyword, ExprNode condition)
            : base(syntaxTree)
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
        public ForStmt(SyntaxTree syntaxTree, Token keyword, Token id, Token eqToken, ExprNode lowerBound, Token toKeyword, ExprNode upperBound, StmtNode stmt)
        : base(syntaxTree)
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

    public sealed class BreakStmt : StmtNode
    {
        public BreakStmt(SyntaxTree syntaxTree, Token keyword, Token semicolon)
        : base(syntaxTree)
        {
            Keyword = keyword;
            Semicolon = semicolon;
        }

        public override SyntaxKind Kind => SyntaxKind.BreakStmt;
        public Token Keyword { get; }
        public Token Semicolon { get; }
    }

    public sealed class ContinueStmt : StmtNode
    {
        public ContinueStmt(SyntaxTree syntaxTree, Token keyword, Token semicolon)
        : base(syntaxTree)
        {
            Keyword = keyword;
            Semicolon = semicolon;
        }

        public override SyntaxKind Kind => SyntaxKind.ContinueStmt;
        public Token Keyword { get; }
        public Token Semicolon { get; }
    }

    public sealed class RetStmt : StmtNode
    {
        public RetStmt(SyntaxTree syntaxTree, Token keyword, ExprNode? value, Token semicolon)
        : base(syntaxTree)
        {
            Keyword = keyword;
            Value = value;
            Semicolon = semicolon;
        }

        public override SyntaxKind Kind => SyntaxKind.RetStmt;
        public Token Keyword { get; }
        public ExprNode? Value { get; }
        public Token Semicolon { get; }
    }
}
