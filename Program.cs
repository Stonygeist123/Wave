using Wave.Binding;
using Wave.Binding.BoundNodes;
using Wave.Nodes;

namespace Wave
{
    internal static class Program
    {
        static void Main()
        {
            bool showTree = false;
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.Write("> ");
                Console.ResetColor();
                string? line = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (line.ToLower() == "#tree")
                {
                    showTree = !showTree;
                    continue;
                }
                else if (line.ToLower() == "#cls")
                {
                    Console.Clear();
                    continue;
                }

                SyntaxTree syntaxTree = SyntaxTree.Parse(line);
                Binder binder = new();
                BoundExpr boundExpr = binder.BindExpr(syntaxTree.Root);
                string[] diagnostics = syntaxTree.Diagnostics.Concat(binder.Diagnostics).ToArray();

                if (showTree)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Print(syntaxTree.Root);
                    Console.ResetColor();
                }

                if (diagnostics.Any())
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    foreach (string d in diagnostics)
                        Console.WriteLine(d);

                    Console.ResetColor();
                }
                else
                {
                    Evaluator evaluator = new(boundExpr);
                    Console.WriteLine(evaluator.Evaluate());
                }
            }
        }

        static void Print(Node node, string indent = "", bool isLast = true)
        {
            string marker = isLast ? "└──" : "├──";
            Console.Write(indent);
            Console.Write(marker);
            Console.Write(node.Kind);
            if (node is Token t && t.Value is not null)
                Console.Write($" {t.Value}");

            Console.WriteLine();
            indent += isLast ? "    " : "│   ";
            Node? lastChild = node.GetChildren().LastOrDefault();

            foreach (Node child in node.GetChildren())
                Print(child, indent, child == lastChild);
        }
    }
}