namespace Wave.Source.Syntax.Nodes
{
    public abstract class ExprNode : Node
    {
        public ExprNode(SyntaxTree syntaxTree) : base(syntaxTree) { }
    }
    public sealed class LiteralExpr : ExprNode
    {
        public override SyntaxKind Kind => SyntaxKind.LiteralExpr;
        public Token Token { get; }
        public object Value { get; }
        public LiteralExpr(SyntaxTree syntaxTree, Token token, object value)
            : base(syntaxTree)
        {
            Token = token;
            Value = value;
        }

        public LiteralExpr(SyntaxTree syntaxTree, Token token)
            : this(syntaxTree, token, token.Value ?? 0) { }
    }

    public sealed class UnaryExpr : ExprNode
    {
        public override SyntaxKind Kind => SyntaxKind.UnaryExpr;
        public Token Op { get; }
        public ExprNode Operand { get; }
        public UnaryExpr(SyntaxTree syntaxTree, Token op, ExprNode operand)
            : base(syntaxTree)
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
        public BinaryExpr(SyntaxTree syntaxTree, ExprNode left, Token op, ExprNode right)
            : base(syntaxTree)
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
        public GroupingExpr(SyntaxTree syntaxTree, Token lParen, ExprNode _expr, Token rParen)
            : base(syntaxTree)
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
        public NameExpr(SyntaxTree syntaxTree, Token identifier)
            : base(syntaxTree) => Identifier = identifier;
    }

    public sealed class AssignmentExpr : ExprNode
    {
        public override SyntaxKind Kind => SyntaxKind.AssignmentExpr;
        public Token Identifier { get; }
        public Token EqToken { get; }
        public ExprNode Value { get; }
        public AssignmentExpr(SyntaxTree syntaxTree, Token identifier, Token eqToken, ExprNode value)
            : base(syntaxTree)
        {
            Identifier = identifier;
            EqToken = eqToken;
            Value = value;
        }
    }

    public sealed class CallExpr : ExprNode
    {
        public override SyntaxKind Kind => SyntaxKind.CallExpr;
        public Token Callee { get; }
        public Token LParen { get; }
        public SeparatedList<ExprNode> Args { get; }
        public Token RParen { get; }
        public CallExpr(SyntaxTree syntaxTree, Token callee, Token lParen, SeparatedList<ExprNode> args, Token rParen)
            : base(syntaxTree)
        {
            Callee = callee;
            LParen = lParen;
            Args = args;
            RParen = rParen;
        }
    }

    public sealed class ArrayExpr : ExprNode
    {
        public override SyntaxKind Kind => SyntaxKind.CallExpr;
        public Token? LessToken { get; }
        public Token? Type { get; }
        public Token? GreaterToken { get; }
        public Token LBracket { get; }
        public SeparatedList<ExprNode> Elements { get; }
        public Token RBracket { get; }
        public ArrayExpr(SyntaxTree syntaxTree, Token lBracket, Token? lessToken, Token? type, Token? greaterToken, SeparatedList<ExprNode> elements, Token rBracket)
            : base(syntaxTree)
        {
            LessToken = lessToken;
            Type = type;
            GreaterToken = greaterToken;
            LBracket = lBracket;
            Elements = elements;
            RBracket = rBracket;
        }
    }

    public sealed class IndexingExpr : ExprNode
    {
        public override SyntaxKind Kind => SyntaxKind.CallExpr;
        public ExprNode Array { get; }
        public Token LBracket { get; }
        public ExprNode Index { get; }
        public Token RBracket { get; }
        public IndexingExpr(SyntaxTree syntaxTree, ExprNode array, Token lBracket, ExprNode index, Token rBracket)
            : base(syntaxTree)
        {
            Array = array;
            LBracket = lBracket;
            Index = index;
            RBracket = rBracket;
        }
    }
}