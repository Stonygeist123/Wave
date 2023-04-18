using System.Text.RegularExpressions;
using Wave.IO;
using Wave.Repl;
using Wave.Source.Compilation;
using Wave.Source.Syntax;

namespace Wave
{
    public static partial class Program
    {
        private static readonly DiagnosticBag importDiagnostics = new();
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

                text = ReplaceImportStmt(text, path, path);
                if (importDiagnostics.Any())
                {
                    Console.Out.WriteDiagnostics(importDiagnostics);
                    return;
                }

                SyntaxTree syntaxTree = SyntaxTree.Parse(SourceText.From(text, path));
                Compilation compilation = Compilation.Create(syntaxTree);
                EvaluationResult result = compilation.Evaluate(new());
                if (result.Diagnostics.Any())
                    Console.Out.WriteDiagnostics(result.Diagnostics);
            }
        }

        private static string ReplaceImportStmt(string text, string startPath, string beforePath) => ImportStmtRegex().Replace(text, delegate (Match m)
                                                                         {
                                                                             string path = m.Value.Remove(0, 6).Replace("\"", string.Empty).TrimStart()[..^1];
                                                                             SourceText src = SourceText.From(text, $"{startPath} -> {beforePath}");
                                                                             if (!File.Exists(path))
                                                                             {
                                                                                 importDiagnostics.Report(new(src, new(m.Index, m.Value.Length)), $"Could not find file at path: {path}.");
                                                                                 return string.Empty;
                                                                             }

                                                                             string fileText = File.ReadAllText(path);
                                                                             bool hasError = false;
                                                                             ImportStmtRegex().Replace(fileText, delegate (Match m)
                                                                             {
                                                                                 if (m.Value.Remove(0, 6).Replace("\"", string.Empty).TrimStart()[..^1] == startPath)
                                                                                     hasError = true;
                                                                                 return string.Empty;
                                                                             });

                                                                             if (hasError)
                                                                             {
                                                                                 importDiagnostics.Report(new(src, new(m.Index, m.Value.Length)), $"Circular dependency not allowed.");
                                                                                 return string.Empty;
                                                                             }

                                                                             return ReplaceImportStmt(fileText, startPath, path);
                                                                         });

        [GeneratedRegex("import \".*\";")]
        private static partial Regex ImportStmtRegex();
    }
}
