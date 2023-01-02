using System.Collections.Immutable;
using System.Text;
using Wave.Binding;

namespace Wave
{
    internal static class Program
    {
        static void Main()
        {
            bool showTree = false;
            Dictionary<VariableSymbol, object?> vars = new();
            StringBuilder textBuilder = new();
            Compilation? previous = null;
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                if (textBuilder.Length == 0)
                    Console.Write("> ");
                else
                    Console.Write("| ");
                Console.ResetColor();

                string input = Console.ReadLine() ?? "";
                bool isBlank = string.IsNullOrWhiteSpace(input);

                if (textBuilder.Length == 0)
                {
                    if (isBlank)
                        continue;
                    else if (input.ToLower() == "#tree")
                    {
                        showTree = !showTree;
                        continue;
                    }
                    else if (input.ToLower() == "#cls")
                    {
                        Console.Clear();
                        continue;
                    }
                    else if (input.ToLower() == "#reset")
                    {
                        previous = null;
                        continue;
                    }
                }

                textBuilder.Append(input);
                string text = textBuilder.ToString();
                SyntaxTree syntaxTree = SyntaxTree.Parse(text);

                if (!isBlank && syntaxTree.Diagnostics.Any())
                    continue;

                Compilation compilation = previous is null
                                ? new(syntaxTree)
                                : previous.ContinueWith(syntaxTree);

                previous = compilation;
                EvaluationResult result = compilation.Evaluate(vars);
                ImmutableArray<Diagnostic> diagnostics = result.Diagnostics;

                if (showTree)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    syntaxTree.Root.WriteTo(Console.Out);
                    Console.ResetColor();
                }

                if (!diagnostics.Any())
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine(result.Value);
                    Console.WriteLine();
                    Console.ResetColor();
                    previous = compilation;
                }
                else
                {
                    foreach (Diagnostic d in diagnostics)
                    {
                        int lineIndex = syntaxTree.Source.GetLineIndex(d.Span.Start);
                        int lineNumber = lineIndex + 1;
                        TextLine line = syntaxTree.Source.Lines[lineIndex];
                        int column = d.Span.Start - line.Start + 1;
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.Write($"[{lineNumber}:{column}]: ");
                        Console.ResetColor();

                        string prefix = syntaxTree.Source.ToString(line.Start, d.Span.Start),
                            error = syntaxTree.Source.ToString(d.Span),
                            suffix = syntaxTree.Source[d.Span.End..];

                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Write(prefix);

                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.Write(error);

                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine(suffix);

                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.Write(new string(' ', prefix.Length + 7));
                        Console.Write(new string('^', Math.Clamp(1, error.Length, error.Length + 1)));
                        Console.WriteLine(' ' + d.ToString() + '\n');
                        Console.ResetColor();
                    }
                }

                textBuilder.Clear();
            }
        }
    }
}