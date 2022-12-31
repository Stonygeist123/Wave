using System.Reflection;

namespace Wave.Nodes
{
    public abstract class Node
    {
        public abstract SyntaxKind Kind { get; }
        public virtual TextSpan Span
        {
            get
            {
                TextSpan? first = GetChildren().FirstOrDefault()?.Span,
                    last = GetChildren().LastOrDefault()?.Span;
                return TextSpan.From(first?.Start ?? 0, last?.End ?? 0);
            }
        }

        public IEnumerable<Node?> GetChildren()
        {
            PropertyInfo[]? properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo property in properties)
            {
                if (typeof(Node).IsAssignableFrom(property.PropertyType))
                    yield return (Node?)property.GetValue(this);
                else if (typeof(IEnumerable<Node>).IsAssignableFrom(property.PropertyType))
                {
                    IEnumerable<Node>? children = (IEnumerable<Node>?)property.GetValue(this);
                    if (children is not null)
                        foreach (var child in children)
                            yield return child;
                }
            }
        }
        public void WriteTo(TextWriter writer) => Print(writer, this);

        private static void Print(TextWriter writer, Node? node, string indent = "", bool isLast = true)
        {
            if (node is Node n)
            {
                string marker = isLast ? "└──" : "├──";
                writer.Write(indent);
                writer.Write(marker);
                writer.Write(n.Kind);
                if (n is Token t && t.Value is not null)
                    writer.Write($" {t.Value}");

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
}
