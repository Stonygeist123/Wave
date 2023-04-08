namespace Wave.Source.Syntax.Nodes
{
    public abstract class MemberNode : Node
    {
        public MemberNode(SyntaxTree syntaxTree) : base(syntaxTree) { }
    }

    public sealed class GlobalStmt : MemberNode
    {
        public GlobalStmt(SyntaxTree syntaxTree, StmtNode stmt)
            : base(syntaxTree) => Stmt = stmt;

        public StmtNode Stmt { get; }
        public override SyntaxKind Kind => SyntaxKind.GlobalStmt;
    }

    public sealed class ParameterNode : MemberNode
    {
        public ParameterNode(SyntaxTree syntaxTree, Token id, TypeClause type)
            : base(syntaxTree)
        {
            Id = id;
            Type = type;
        }

        public Token Id { get; }
        public TypeClause Type { get; }
        public override SyntaxKind Kind => SyntaxKind.ParameterSyntax;
    }

    public sealed class ParameterList : MemberNode
    {
        public ParameterList(SyntaxTree syntaxTree, Token lParen, SeparatedList<ParameterNode> parameters, Token rParen)
            : base(syntaxTree)
        {
            LParen = lParen;
            Parameters = parameters;
            RParen = rParen;
        }

        public Token LParen { get; }
        public SeparatedList<ParameterNode> Parameters { get; }
        public Token RParen { get; }
        public override SyntaxKind Kind => SyntaxKind.ParameterSyntax;
    }

    public sealed class FnDeclStmt : MemberNode
    {
        public FnDeclStmt(SyntaxTree syntaxTree, Token keyword, Token id, ParameterList? parameters, TypeClause? typeClause, StmtNode body)
            : base(syntaxTree)
        {
            Keyword = keyword;
            Name = id;
            Parameters = parameters;
            TypeClause = typeClause;
            Body = body;
        }

        public Token Keyword { get; }
        public Token Name { get; }
        public ParameterList? Parameters { get; }
        public TypeClause? TypeClause { get; }
        public StmtNode Body { get; }
        public override SyntaxKind Kind => SyntaxKind.FnDecl;
    }
}