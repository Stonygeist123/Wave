using Wave.Syntax.Nodes;

namespace Wave.Nodes
{
    public sealed class ElseClause : Node
    {
        public ElseClause(Token keyword, StmtNode stmt)
        {
            Keyword = keyword;
            Stmt = stmt;
        }

        public override SyntaxKind Kind => SyntaxKind.ElseClause;
        public Token Keyword { get; }
        public StmtNode Stmt { get; }
    }
}
