namespace Wave.Source.Syntax.Nodes
{
    public sealed class TypeClause : Node
    {
        public override SyntaxKind Kind => SyntaxKind.TypeClause;
        public Token Sign { get; }
        public Token Id { get; }
        public Token? LBracket { get; }
        public Token? RBracket { get; }
        public TypeClause(SyntaxTree syntaxTree, Token sign, Token id, Token? lBracket = null, Token? rBracket = null)
            : base(syntaxTree)
        {
            Sign = sign;
            Id = id;
            LBracket = lBracket;
            RBracket = rBracket;
        }
    }
}