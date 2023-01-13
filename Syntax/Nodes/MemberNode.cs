using Wave.Syntax.Nodes;

namespace Wave.Nodes
{
    public abstract class MemberNode : Node { }
    public sealed class GlobalStmt : MemberNode
    {
        public GlobalStmt(StmtNode stmt) => Stmt = stmt;
        public StmtNode Stmt { get; }
        public override SyntaxKind Kind => SyntaxKind.GlobalStmt;
    }

    public sealed class ParameterNode : MemberNode
    {
        public ParameterNode(Token id, TypeClause type)
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
        public ParameterList(Token lParen, SeparatedList<ParameterNode> parameters, Token rParen)
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
        public FnDeclStmt(Token keyword, Token id, ParameterList? parameters, TypeClause? typeClause, StmtNode body)
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