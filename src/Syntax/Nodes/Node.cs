using System.Reflection;

namespace Wave.Source.Syntax.Nodes
{
    public abstract class Node
    {
        public SyntaxTree SyntaxTree { get; }
        public Node(SyntaxTree syntaxTree) => SyntaxTree = syntaxTree;
        public abstract SyntaxKind Kind { get; }
        public virtual TextSpan Span
        {
            get
            {
                IEnumerable<Node> children = GetChildren();
                return TextSpan.From(children.First().Span.Start, children.Last().Span.End);
            }
        }

        public TextLocation Location => new(SyntaxTree.Source, Span);
        public IEnumerable<Node> GetChildren()
        {
            PropertyInfo[]? properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo property in properties)
            {
                if (typeof(Node).IsAssignableFrom(property.PropertyType))
                {
                    Node? child = (Node?)property.GetValue(this);
                    if (child is not null)
                        yield return child;
                }
                else if (typeof(IEnumerable<Node?>).IsAssignableFrom(property.PropertyType))
                {
                    IEnumerable<Node?>? children = (IEnumerable<Node?>?)property.GetValue(this);
                    if (children is not null)
                        foreach (Node? child in children)
                            if (child is not null)
                                yield return child;
                }
            }
        }

        public Token GetLastToken()
        {
            if (this is Token token)
                return token;

            return GetChildren().Last().GetLastToken();
        }

        public void WriteTo(TextWriter writer) => Print(writer, this);

        private static void Print(TextWriter writer, Node? node, string indent = "", bool isLast = true)
        {
            bool isConsole = writer == Console.Out;

            if (node is Node n)
            {
                string marker = isLast ? "└──" : "├──";
                writer.Write(indent);
                if (isConsole)
                    Console.ForegroundColor = ConsoleColor.DarkGray;

                writer.Write(marker);
                if (isConsole)
                    Console.ForegroundColor = n is Token ? ConsoleColor.Blue : ConsoleColor.Cyan;

                writer.Write(n.Kind);
                if (n is Token t && t.Value is not null)
                    writer.Write($" {t.Value}");

                if (isConsole)
                    Console.ResetColor();

                writer.WriteLine();
                indent += isLast ? "    " : "│   ";
                Node? lastChild = node?.GetChildren().LastOrDefault();

                foreach (Node? child in n.GetChildren())
                    Print(writer, child, indent, child == lastChild);
            }
        }

        public override string ToString()
        {
            using StringWriter writer = new();
            WriteTo(writer);
            return writer.ToString();
        }
    }

    public enum SyntaxKind
    {
        // Token
        Space,
        Comment,
        Plus,
        Minus,
        Star,
        Slash,
        Power,
        Mod,
        And,
        Or,
        Xor,
        Inv,
        LParen,
        RParen,
        LBracket,
        RBracket,
        LBrace,
        RBrace,
        Bang,
        LogicAnd,
        LogicOr,
        EqEq,
        NotEq,
        Greater,
        Less,
        GreaterEq,
        LessEq,
        Eq,
        Comma,
        Semicolon,
        Colon,
        ColonColon,
        Dot,
        Arrow,
        Bad,
        Eof,

        // Literals
        Int,
        Float,
        Identifier,
        String,

        // Keywords
        True,
        False,
        Var,
        Mut,
        If,
        Else,
        While,
        Do,
        For,
        Each,
        Fn,
        Class,
        Continue,
        Break,
        Ret,
        In,
        Pub,
        Priv,
        Type,
        Namespace,

        // Nodes
        CompilationUnit,
        FnDecl,
        ClassDecl,
        ADTDecl,
        GlobalStmt,
        ElseClause,
        TypeClause,
        ParameterDecl,
        FieldDecl,

        // Expr
        LiteralExpr,
        BinaryExpr,
        UnaryExpr,
        GroupingExpr,
        NameExpr,
        AssignmentExpr,
        CallExpr,
        ArrayExpr,
        IndexingExpr,
        NamespaceGetExpr,
        GetExpr,
        MethodExpr,
        SetExpr,

        // Stmt
        ExpressionStmt,
        BlockStmt,
        VarStmt,
        IfStmt,
        ForStmt,
        ForEachStmt,
        BreakStmt,
        ContinueStmt,
        RetStmt
    }
}