using Wave.Syntax.Nodes;

namespace Wave.Nodes
{
    public sealed class TypeClause : Node
    {
        public override SyntaxKind Kind => SyntaxKind.TypeClause;
        public Token Sign { get; }
        public Token Id { get; }
        public TypeClause(Token sign, Token id)
        {
            Sign = sign;
            Id = id;
        }
    }
}