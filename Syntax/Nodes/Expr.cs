namespace Wave.Nodes
{
    public abstract class ExprNode : Node { }
    public sealed class LiteralExpr : ExprNode
    {
        public override SyntaxKind Kind => SyntaxKind.LiteralExpr;
        public Token Token { get; }
        public object Value { get; }
        public LiteralExpr(Token token, object value)
        {
            Token = token;
            Value = value;
        }

        public LiteralExpr(Token token)
            : this(token, token.Value ?? 0)
        {
        }
    }

    public sealed class UnaryExpr : ExprNode
    {
        public override SyntaxKind Kind => SyntaxKind.UnaryExpr;
        public Token Op { get; }
        public ExprNode Operand { get; }
        public UnaryExpr(Token op, ExprNode operand)
        {
            Op = op;
            Operand = operand;
        }
    }

    public sealed class BinaryExpr : ExprNode
    {
        public override SyntaxKind Kind => SyntaxKind.BinaryExpr;
        public ExprNode Left { get; }
        public Token Op { get; }
        public ExprNode Right { get; }
        public BinaryExpr(ExprNode left, Token op, ExprNode right)
        {
            Left = left;
            Op = op;
            Right = right;
        }
    }

    public sealed class GroupingExpr : ExprNode
    {
        public override SyntaxKind Kind => SyntaxKind.GroupingExpr;

        public Token LParen { get; }
        public ExprNode Expr { get; }
        public Token RParen { get; }
        public GroupingExpr(Token lParen, ExprNode _expr, Token rParen)
        {
            LParen = lParen;
            Expr = _expr;
            RParen = rParen;
        }
    }

    public sealed class NameExpr : ExprNode
    {
        public override SyntaxKind Kind => SyntaxKind.NameExpr;
        public Token Identifier { get; }
        public NameExpr(Token identifier) => Identifier = identifier;
    }

    public sealed class AssignmentExpr : ExprNode
    {
        public override SyntaxKind Kind => SyntaxKind.NameExpr;
        public Token Identifier { get; }
        public ExprNode Value { get; }
        public AssignmentExpr(Token identifier, ExprNode value)
        {
            Identifier = identifier;
            Value = value;
        }
    }
}