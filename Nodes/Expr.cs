namespace Wave.Nodes
{
    internal abstract class ExprNode : Node { }
    internal sealed class NumberExpr : ExprNode
    {
        public override SyntaxKind Kind => SyntaxKind.NumberExpr;
        public Token Token { get; }
        public NumberExpr(Token token) => Token = token;

        public override IEnumerable<Node> GetChildren()
        {
            yield return Token;
        }
    }
    internal sealed class BinaryExpr : ExprNode
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

        public override IEnumerable<Node> GetChildren()
        {
            yield return Left;
            yield return Op;
            yield return Right;
        }
    }
    internal sealed class UnaryExpr : ExprNode
    {
        public override SyntaxKind Kind => SyntaxKind.UnaryExpr;
        public ExprNode Operand { get; }
        public Token Op { get; }
        public UnaryExpr(ExprNode operand, Token op)
        {
            Operand = operand;
            Op = op;
        }

        public override IEnumerable<Node> GetChildren()
        {
            yield return Operand;
            yield return Op;
        }
    }
    internal sealed class GroupingExpr : ExprNode
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
        public override IEnumerable<Node> GetChildren()
        {
            yield return LParen;
            yield return Expr;
            yield return RParen;
        }
    }
}
