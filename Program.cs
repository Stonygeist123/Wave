using Wave.IO;
using Wave.Repl;
using Wave.Source.Compilation;
using Wave.Source.Syntax;

namespace Wave
{
    public static class Program
    {
        private static void Main(string[] args)
        {
            if (!args.Any())
            {
                WaveRepl repl = new();
                repl.Run();
            }
            else
            {
                string path = Path.GetFullPath(args[0]);
                if (!File.Exists(path))
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine($"File \"{path}\" does not exist.");
                    Console.ResetColor();
                    return;
                }

                string text = File.ReadAllText(path);
                if (string.IsNullOrEmpty(text))
                    return;

                SyntaxTree syntaxTree = SyntaxTree.Parse(SourceText.From(text, path));
                Compilation compilation = Compilation.Create(syntaxTree);
                EvaluationResult result = compilation.Evaluate(new());
                if (result.Diagnostics.Any())
                    Console.Out.WriteDiagnostics(result.Diagnostics);
            }
        }
    }
}
