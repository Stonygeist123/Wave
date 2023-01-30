namespace Wave.Source.Syntax.Nodes
{
    public sealed class TypeClause : Node
    {
        public override SyntaxKind Kind => SyntaxKind.TypeClause;
        public Token Sign { get; }
        public Token Id { get; }
        public TypeClause(SyntaxTree syntaxTree, Token sign, Token id)
            : base(syntaxTree)
        {
            Sign = sign;
            Id = id;
        }
    }
}