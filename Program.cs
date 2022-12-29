using Wave.Binding;
using Wave.Nodes;

namespace Wave
{
    internal static class Program
    {
        static void Main()
        {
            bool showTree = false;
            Dictionary<VariableSymbol, object?> vars = new();
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
                Compilation compilation = new(syntaxTree);
                EvaluationResult result = compilation.Evaluate(vars);
                IReadOnlyList<Diagnostic> diagnostics = result.Diagnostics;

                if (showTree)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Print(syntaxTree.Root);
                    Console.ResetColor();
                }

                if (!diagnostics.Any())
                    Console.WriteLine(result.Value);
                else
                    foreach (Diagnostic d in diagnostics)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.WriteLine(d);
                        Console.ResetColor();

                        string prefix = line[..d.Span.Start];
                        string error = line.Substring(d.Span.Start, d.Span.Length);
                        string suffix = line[d.Span.End..];

                        Console.Write("  ");
                        Console.Write(prefix);

                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.Write(error);
                        Console.ResetColor();

                        Console.WriteLine(suffix);
                        Console.WriteLine();
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