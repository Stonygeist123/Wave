using System.Collections.Immutable;

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

    public sealed class ParameterDecl : MemberNode
    {
        public ParameterDecl(SyntaxTree syntaxTree, Token id, TypeClause type)
            : base(syntaxTree)
        {
            Id = id;
            Type = type;
        }

        public Token Id { get; }
        public TypeClause Type { get; }
        public override SyntaxKind Kind => SyntaxKind.ParameterDecl;
    }

    public sealed class ParameterList : MemberNode
    {
        public ParameterList(SyntaxTree syntaxTree, Token lParen, SeparatedList<ParameterDecl> parameters, Token rParen)
            : base(syntaxTree)
        {
            LParen = lParen;
            Parameters = parameters;
            RParen = rParen;
        }

        public Token LParen { get; }
        public SeparatedList<ParameterDecl> Parameters { get; }
        public Token RParen { get; }
        public override SyntaxKind Kind => SyntaxKind.ParameterDecl;
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

    public sealed class FieldDecl : MemberNode
    {
        public FieldDecl(SyntaxTree syntaxTree, Token? accessibility, Token? mutKeyword, Token name, TypeClause? typeClause, Token eqToken, ExprNode value, Token semicolon)
            : base(syntaxTree)
        {
            Accessibility = accessibility;
            MutKeyword = mutKeyword;
            Name = name;
            TypeClause = typeClause;
            EqToken = eqToken;
            Value = value;
            Semicolon = semicolon;
        }

        public Token? Accessibility { get; }
        public Token? MutKeyword { get; }
        public Token Name { get; }
        public TypeClause? TypeClause { get; }
        public Token EqToken { get; }
        public ExprNode Value { get; }
        public Token Semicolon { get; }
        public override SyntaxKind Kind => SyntaxKind.FieldDecl;
    }

    public sealed class ClassDeclStmt : MemberNode
    {
        public ClassDeclStmt(SyntaxTree syntaxTree, Token keyword, Token name, Token lBrace, ImmutableArray<FnDeclStmt> fnDecls, ImmutableArray<FieldDecl> fieldDecls, Token rBrace)
            : base(syntaxTree)
        {
            Keyword = keyword;
            Name = name;
            LBrace = lBrace;
            FnDecls = fnDecls;
            FieldDecls = fieldDecls;
            RBrace = rBrace;
        }

        public Token Keyword { get; }
        public Token Name { get; }
        public Token LBrace { get; }
        public ImmutableArray<FnDeclStmt> FnDecls { get; }
        public ImmutableArray<FieldDecl> FieldDecls { get; }
        public Token RBrace { get; }
        public override SyntaxKind Kind => SyntaxKind.ClassDecl;
    }
}
