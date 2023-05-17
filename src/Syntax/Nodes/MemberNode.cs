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
        public FnDeclStmt(SyntaxTree syntaxTree, Token? accessibility, Token keyword, Token? staticDot, Token name, ParameterList? parameters, TypeClause? typeClause, StmtNode body)
            : base(syntaxTree)
        {
            Accessibility = accessibility;
            Keyword = keyword;
            Name = name;
            Parameters = parameters;
            TypeClause = typeClause;
            Body = body;
            StaticDot = staticDot;
        }

        public Token? Accessibility { get; }
        public Token Keyword { get; }
        public Token Name { get; }
        public ParameterList? Parameters { get; }
        public TypeClause? TypeClause { get; }
        public StmtNode Body { get; }
        public Token? StaticDot { get; }
        public override SyntaxKind Kind => SyntaxKind.FnDecl;
    }

    public sealed class CtorDeclStmt : MemberNode
    {
        public CtorDeclStmt(SyntaxTree syntaxTree, Token? accessibility, Token keyword, ParameterList? parameters, StmtNode body, Token className)
            : base(syntaxTree)
        {
            Accessibility = accessibility;
            Keyword = keyword;
            Parameters = parameters;
            Body = body;
            ClassName = className;
        }

        public Token? Accessibility { get; }
        public Token Keyword { get; }
        public Token ClassName { get; }
        public ParameterList? Parameters { get; }
        public StmtNode Body { get; }
        public override SyntaxKind Kind => SyntaxKind.FnDecl;
    }

    public sealed class FieldDecl : MemberNode
    {
        public FieldDecl(SyntaxTree syntaxTree, Token? accessibility, Token? mutKeyword, Token? staticDot, Token name, TypeClause? typeClause, Token eqToken, ExprNode value, Token semicolon)
            : base(syntaxTree)
        {
            Accessibility = accessibility;
            MutKeyword = mutKeyword;
            StaticDot = staticDot;
            Name = name;
            TypeClause = typeClause;
            EqToken = eqToken;
            Value = value;
            Semicolon = semicolon;
        }

        public Token? Accessibility { get; }
        public Token? MutKeyword { get; }
        public Token? StaticDot { get; }
        public Token Name { get; }
        public TypeClause? TypeClause { get; }
        public Token EqToken { get; }
        public ExprNode Value { get; }
        public Token Semicolon { get; }
        public override SyntaxKind Kind => SyntaxKind.FieldDecl;
    }

    public sealed class ClassDeclStmt : MemberNode
    {
        public ClassDeclStmt(SyntaxTree syntaxTree, Token keyword, Token name, Token lBrace, CtorDeclStmt? ctor, ImmutableArray<FnDeclStmt> fnDecls, ImmutableArray<FieldDecl> fieldDecls, Token rBrace)
            : base(syntaxTree)
        {
            Keyword = keyword;
            Name = name;
            LBrace = lBrace;
            Ctor = ctor;
            FnDecls = fnDecls;
            FieldDecls = fieldDecls;
            RBrace = rBrace;
        }

        public Token Keyword { get; }
        public Token Name { get; }
        public Token LBrace { get; }
        public CtorDeclStmt? Ctor { get; }
        public ImmutableArray<FnDeclStmt> FnDecls { get; }
        public ImmutableArray<FieldDecl> FieldDecls { get; }
        public Token RBrace { get; }
        public override SyntaxKind Kind => SyntaxKind.ClassDecl;
    }

    public sealed class ADTDeclStmt : MemberNode
    {
        public ADTDeclStmt(SyntaxTree syntaxTree, Token keyword, Token name, Token lBrace, SeparatedList<Token> members, Token rBrace)
            : base(syntaxTree)
        {
            Keyword = keyword;
            Name = name;
            LBrace = lBrace;
            Members = members;
            RBrace = rBrace;
        }

        public Token Keyword { get; }
        public Token Name { get; }
        public Token LBrace { get; }
        public SeparatedList<Token> Members { get; }
        public Token RBrace { get; }
        public override SyntaxKind Kind => SyntaxKind.ADTDecl;
    }

    public sealed class NamespaceDeclStmt : MemberNode
    {
        public NamespaceDeclStmt(SyntaxTree syntaxTree, Token keyword, Token name, Token lBrace, ImmutableArray<MemberNode> members, Token rBrace)
            : base(syntaxTree)
        {
            Keyword = keyword;
            Name = name;
            LBrace = lBrace;
            Members = members;
            RBrace = rBrace;
        }

        public Token Keyword { get; }
        public Token Name { get; }
        public Token LBrace { get; }
        public ImmutableArray<MemberNode> Members { get; }
        public Token RBrace { get; }
        public override SyntaxKind Kind => SyntaxKind.ADTDecl;
    }
}
