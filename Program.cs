using Wave.Nodes;

namespace Wave
{
    internal class Program
    {
        static void Main()
        {
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.Write("> ");
                Console.ResetColor();
                string? line = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                SyntaxTree syntaxTree = SyntaxTree.Parse(line);

                Console.ForegroundColor = ConsoleColor.DarkGray;
                Print(syntaxTree.Root);
                Console.ResetColor();

                if (syntaxTree.Diagnostics.Any())
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    foreach (string diagnostic in syntaxTree.Diagnostics)
                        Console.WriteLine(diagnostic);

                    Console.ResetColor();
                }
                else
                {
                    Evaluator evaluator = new(syntaxTree.Root);
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