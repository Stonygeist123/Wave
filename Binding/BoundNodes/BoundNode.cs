using System.Reflection;

namespace Wave.Binding.BoundNodes
{
    public enum BoundNodeKind
    {
        // Expr
        LiteralExpr,
        UnaryExpr,
        BinaryExpr,
        NameExpr,
        AssignmentExpr,
        CallExpr,
        ErrorExpr,

        // Stmt
        ExpressionStmt,
        BlockStmt,
        VarStmt,
        IfStmt,
        WhileStmt,
        ForStmt,
        LabelStmt,
        GotoStmt,
        CondGotoStmt
    }

    public abstract class BoundNode
    {
        public abstract BoundNodeKind Kind { get; }
        public IEnumerable<BoundNode?> GetChildren()
        {
            PropertyInfo[]? properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo property in properties)
            {
                if (typeof(BoundNode).IsAssignableFrom(property.PropertyType))
                    yield return (BoundNode?)property.GetValue(this);
                else if (typeof(IEnumerable<BoundNode>).IsAssignableFrom(property.PropertyType))
                {
                    IEnumerable<BoundNode>? children = (IEnumerable<BoundNode>?)property.GetValue(this);
                    if (children is not null)
                        foreach (BoundNode? child in children)
                            yield return child;
                }
            }
        }

        public IEnumerable<(string Name, object Value)> GetProps()
        {
            PropertyInfo[]? properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo property in properties)
            {
                if (property.Name == nameof(Kind) || property.Name == nameof(BoundBinary.Op))
                    continue;

                if (typeof(BoundNode).IsAssignableFrom(property.PropertyType)
                    || typeof(IEnumerable<BoundNode>).IsAssignableFrom(property.PropertyType))
                    continue;

                object? value = property.GetValue(this);
                if (value is not null)
                    yield return (property.Name, value);
            }
        }

        public void WriteTo(TextWriter writer) => Print(writer, this);

        private static void Print(TextWriter writer, BoundNode? node, string indent = "", bool isLast = true)
        {
            bool isConsole = writer == Console.Out;

            if (node is BoundNode n)
            {
                string marker = isLast ? "└── " : "├── ";
                writer.Write(indent);
                writer.Write(marker);

                if (isConsole)
                    Console.ForegroundColor = GetColor(node);

                string text = GetText(node);
                writer.Write(text);

                bool isFirstProp = true;
                foreach ((string Name, object Value) in n.GetProps())
                {
                    if (isFirstProp)
                        isFirstProp = false;
                    else
                    {
                        if (isConsole)
                            Console.ForegroundColor = ConsoleColor.DarkGray;

                        writer.Write(",");
                    }


                    writer.Write(" ");

                    if (isConsole)
                        Console.ForegroundColor = ConsoleColor.Yellow;

                    writer.Write(Name);
                    if (isConsole)
                        Console.ForegroundColor = ConsoleColor.DarkGray;

                    writer.Write(" = ");
                    if (isConsole)
                        Console.ForegroundColor = ConsoleColor.DarkYellow;

                    writer.Write(Value);
                }

                if (isConsole)
                    Console.ResetColor();

                writer.WriteLine();
                indent += isLast ? "    " : "│   ";
                BoundNode? lastChild = node?.GetChildren().LastOrDefault();

                foreach (BoundNode? child in n.GetChildren())
                    Print(writer, child, indent, child == lastChild);
            }
        }

        private static string GetText(BoundNode node) => node switch
        {
            BoundBinary b => b.Op.Kind.ToString() + "Expression",
            BoundUnary u => u.Op.Kind.ToString() + "Expression",
            _ => node.Kind.ToString()
        };

        public override string ToString()
        {
            using StringWriter writer = new();
            WriteTo(writer);
            return writer.ToString();
        }

        private static ConsoleColor GetColor(BoundNode node) => node switch
        {
            BoundExpr => ConsoleColor.Blue,
            BoundStmt => ConsoleColor.Cyan,
            _ => ConsoleColor.Yellow
        };
    }
}
