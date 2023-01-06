using Wave.Syntax.Nodes;

namespace Wave.Nodes
{
    public sealed class CompilationUnit : Node
    {
        public CompilationUnit(StmtNode stmt, Token eofToken)
        {
            Stmt = stmt;
            EofToken = eofToken;
        }

        public StmtNode Stmt { get; }
        public Token EofToken { get; }
        public override SyntaxKind Kind => SyntaxKind.CompilationUnit;
    }
}