using System.Text.RegularExpressions;
using Wave.IO;
using Wave.Repl;
using Wave.Source.Compilation;
using Wave.Source.Syntax;

namespace Wave
{
    public static partial class Program
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

                SyntaxTree syntaxTree = SyntaxTree.Parse(SourceText.From(ReplaceImportStmt(text), path));
                Compilation compilation = Compilation.Create(syntaxTree);
                EvaluationResult result = compilation.Evaluate(new());
                if (result.Diagnostics.Any())
                    Console.Out.WriteDiagnostics(result.Diagnostics);
            }
        }

        private static string ReplaceImportStmt(string text) => ImportStmtRegex().Replace(text, delegate (Match m)
                                                                         {
                                                                             return ReplaceImportStmt(File.ReadAllText(m.Value.Remove(0, 6).Replace("\"", "").TrimStart()[..^1]));
                                                                         });

        [GeneratedRegex("import \".*\";")]
        private static partial Regex ImportStmtRegex();
    }
}
