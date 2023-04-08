using System.CodeDom.Compiler;
using System.Text.RegularExpressions;
using Wave.Source.Syntax;
using Wave.Source.Syntax.Nodes;

namespace Wave.IO
{
    public static partial class TextWriterExtensions
    {
        private static bool IsConsole(this TextWriter writer) => writer == Console.Out
                                                                    ? !Console.IsOutputRedirected
                                                                    : writer == Console.Error
                                                                        ? !Console.IsErrorRedirected && !Console.IsOutputRedirected
                                                                        : writer is IndentedTextWriter iw && iw.InnerWriter.IsConsole();
        public static void SetForeground(this TextWriter writer, ConsoleColor color)
        {
            if (writer.IsConsole())
                Console.ForegroundColor = color;
        }

        public static void ResetColor(this TextWriter writer)
        {
            if (writer.IsConsole())
                Console.ResetColor();
        }

        public static void WriteSpace(this TextWriter writer) => writer.Write(" ");
        public static void WriteLiteral(this TextWriter writer, string text)
        {
            writer.SetForeground(ConsoleColor.Cyan);
            writer.Write(text);
            writer.ResetColor();
        }

        public static void WriteKeyword(this TextWriter writer, string text)
        {
            writer.SetForeground(ConsoleColor.DarkMagenta);
            writer.Write(text);
            writer.ResetColor();
        }

        public static void WriteKeyword(this TextWriter writer, SyntaxKind kind)
        {
            writer.SetForeground(ConsoleColor.DarkMagenta);
            writer.Write(kind.GetLexeme());
            writer.ResetColor();
        }

        public static void WriteIdentifier(this TextWriter writer, string text)
        {
            writer.SetForeground(ConsoleColor.DarkYellow);
            writer.Write(text);
            writer.ResetColor();
        }

        public static void WriteString(this TextWriter writer, string text)
        {
            writer.SetForeground(ConsoleColor.DarkGreen);
            writer.Write(text);
            writer.ResetColor();
        }

        public static void WritePunctuation(this TextWriter writer, string text)
        {
            writer.SetForeground(ConsoleColor.DarkGray);
            writer.Write(text);
            writer.ResetColor();
        }

        public static void WritePunctuation(this TextWriter writer, SyntaxKind kind)
        {
            writer.SetForeground(ConsoleColor.DarkGray);
            writer.Write(kind.GetLexeme());
            writer.ResetColor();
        }

        public static void WriteDiagnostics(this TextWriter writer, IEnumerable<Diagnostic> diagnostics)
        {
            foreach (Diagnostic d in diagnostics.Where(d => d.Location == default))
            {
                writer.SetForeground(ConsoleColor.DarkRed);
                writer.WriteLine(d);
                writer.ResetColor();
            }

            foreach (Diagnostic d in diagnostics.OrderBy(d => d.Location?.Source.FileName).ThenBy(d => d.Location?.Span.Start).ThenBy(d => d.Location?.Span.Length))
            {
                if (d.Location == null)
                    continue;

                SourceText source = d.Location.Source;
                TextSpan span = d.Location.Span;
                int lineIndex = source.GetLineIndex(span.Start);
                int lineNumber = lineIndex + 1;
                TextLine line = source.Lines[lineIndex];
                int column = span.Start - line.Start + 1;

                writer.SetForeground(ConsoleColor.DarkGray);
                writer.WriteLine($"[{d.Location.FileName}]");
                if (lineIndex > 0 && !string.IsNullOrWhiteSpace(source.Lines[lineIndex - 1].ToString()))
                {
                    writer.SetForeground(ConsoleColor.Cyan);
                    writer.Write($"{lineIndex}| ");
                    writer.SetForeground(ConsoleColor.DarkGray);
                    writer.WriteLine(source.Lines[lineIndex - 1]);
                }

                writer.SetForeground(ConsoleColor.DarkRed);
                string where = $"{lineNumber}| ";
                writer.Write(where);

                TextSpan prefixSpan = TextSpan.From(line.Start, span.Start);
                TextSpan suffixSpan = TextSpan.From(span.End, line.End);

                string prefix = source.ToString(prefixSpan),
                    error = source.ToString(span),
                    suffix = suffixSpan.Length > 0 ? source.ToString(suffixSpan) : string.Empty;

                writer.SetForeground(ConsoleColor.White);
                writer.Write(prefix);

                writer.SetForeground(ConsoleColor.DarkRed);
                writer.Write(error);

                writer.SetForeground(ConsoleColor.White);
                writer.WriteLine(suffix);

                writer.SetForeground(ConsoleColor.DarkRed);
                writer.Write(ReplaceBySpace().Replace(where, " ") + ReplaceBySpace().Replace(prefix, " "));
                writer.Write(new string('^', Math.Clamp(error.Length, 1, error.Length + 1)));
                writer.WriteLine($" {d}");

                if (d.Suggestion is not null)
                {
                    writer.Write(ReplaceBySpace().Replace(where, " ") + ReplaceBySpace().Replace(prefix, " ") + ReplaceBySpace().Replace(error, " "));
                    writer.SetForeground(ConsoleColor.Blue);
                    writer.WriteLine($" Suggestion: {d.Suggestion}");
                    writer.ResetColor();
                }

                if (lineNumber < source.Lines.Length && !string.IsNullOrWhiteSpace(source.Lines[lineNumber].ToString()))
                {
                    writer.SetForeground(ConsoleColor.Cyan);
                    writer.Write($"\n{lineNumber + 1}| ");
                    writer.SetForeground(ConsoleColor.DarkGray);
                    writer.WriteLine(source.Lines[lineNumber]);
                }

                writer.ResetColor();
            }
        }

        [GeneratedRegex("\\S")]
        private static partial Regex ReplaceBySpace();
    }
}
