namespace Wave.Source.Syntax.Nodes
{
    public sealed class ElseClause : Node
    {
        public ElseClause(SyntaxTree syntaxTree, Token keyword, StmtNode stmt)
            : base(syntaxTree)
        {
            Keyword = keyword;
            Stmt = stmt;
        }

        public override SyntaxKind Kind => SyntaxKind.ElseClause;
        public Token Keyword { get; }
        public StmtNode Stmt { get; }
    }
}
