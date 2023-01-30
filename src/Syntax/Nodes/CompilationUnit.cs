using System.Collections.Immutable;

namespace Wave.Source.Syntax.Nodes
{
    public sealed class CompilationUnit : Node
    {
        public CompilationUnit(SyntaxTree syntaxTree, ImmutableArray<MemberNode> members, Token eofToken)
            : base(syntaxTree)
        {
            Members = members;
            EofToken = eofToken;
        }

        public ImmutableArray<MemberNode> Members { get; }
        public Token EofToken { get; }
        public override SyntaxKind Kind => SyntaxKind.CompilationUnit;
    }
}