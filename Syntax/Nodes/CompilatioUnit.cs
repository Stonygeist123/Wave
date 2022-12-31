namespace Wave.Nodes
{
    public sealed class CompilationUnit : Node
    {
        public CompilationUnit(ExprNode expr, Token eofToken)
        {
            Expr = expr;
            EofToken = eofToken;
        }

        public ExprNode Expr { get; }
        public Token EofToken { get; }
        public override SyntaxKind Kind => SyntaxKind.CompilationUnit;
    }
}