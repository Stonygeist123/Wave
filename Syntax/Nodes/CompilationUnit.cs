using System.Collections.Immutable;
using Wave.Syntax.Nodes;

namespace Wave.Nodes
{
    public sealed class CompilationUnit : Node
    {
        public CompilationUnit(ImmutableArray<MemberNode> members, Token eofToken)
        {
            Members = members;
            EofToken = eofToken;
        }

        public ImmutableArray<MemberNode> Members { get; }
        public Token EofToken { get; }
        public override SyntaxKind Kind => SyntaxKind.CompilationUnit;
    }
}