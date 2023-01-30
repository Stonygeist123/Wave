using System.CodeDom.Compiler;
using Wave.Source.Syntax;
using Wave.Source.Syntax.Nodes;

namespace Wave.IO
{
    public static class TextWriterExtensions
    {
        private static bool IsConsole(this TextWriter writer) => writer == Console.Out
                                                                    ? !Console.IsOutputRedirected
                                                                    : writer == Console.Error
                                                                        ? !Console.IsErrorRedirected && !Console.IsOutputRedirected
                                                                        : writer is IndentedTextWriter iw && iw.InnerWriter.IsConsole();
        public static void SetForeground(this TextWriter writer, ConsoleColor color)
        {
            if (IsConsole(writer))
                Console.ForegroundColor = color;
        }

        public static void ResetColor(this TextWriter writer)
        {
            if (IsConsole(writer))
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
            foreach (Diagnostic d in diagnostics.OrderBy(d => d.Location.Source.FileName).ThenBy(d => d.Location.Span.Start).ThenBy(d => d.Location.Span.Length))
            {
                SourceText source = d.Location.Source;
                int lineIndex = source.GetLineIndex(d.Location.Span.Start);
                int lineNumber = lineIndex + 1;
                TextLine line = source.Lines[lineIndex];
                TextSpan span = d.Location.Span;
                int column = span.Start - line.Start + 1;

                writer.SetForeground(ConsoleColor.DarkGray);
                writer.WriteLine($"[{d.Location.FileName}]");
                if (lineIndex > 0)
                {
                    writer.SetForeground(ConsoleColor.Cyan);
                    writer.Write($"{lineIndex}| ");
                    writer.SetForeground(ConsoleColor.DarkGray);
                    writer.WriteLine(source.Lines[lineIndex - 1]);
                }

                string where = $"{lineNumber}| ";
                writer.SetForeground(ConsoleColor.DarkRed);
                writer.Write(where);

                TextSpan prefixSpan = TextSpan.From(line.Start, span.Start);
                TextSpan suffixSpan = TextSpan.From(span.End, line.End);

                string prefix = source.ToString(prefixSpan),
                    error = source.ToString(span),
                    suffix = source.ToString(suffixSpan);

                writer.SetForeground(ConsoleColor.White);
                writer.Write(prefix);

                writer.SetForeground(ConsoleColor.DarkRed);
                writer.Write(error);

                writer.SetForeground(ConsoleColor.White);
                writer.WriteLine(suffix);

                writer.SetForeground(ConsoleColor.DarkRed);
                writer.Write(new string(' ', prefix.Length + where.Length));
                writer.Write(new string('^', Math.Clamp(error.Length, 1, error.Length + 1)));
                writer.WriteLine($" {d}");

                if (d.Suggestion is not null)
                {
                    writer.Write(new string(' ', prefix.Length + where.Length + error.Length + 1));
                    writer.SetForeground(ConsoleColor.Blue);
                    writer.WriteLine($"Suggestion: {d.Suggestion}");
                    writer.ResetColor();
                }

                if (lineNumber < source.Lines.Length)
                {
                    writer.SetForeground(ConsoleColor.Cyan);
                    writer.Write($"\n{lineNumber + 1}| ");
                    writer.SetForeground(ConsoleColor.DarkGray);
                    writer.WriteLine(source.Lines[lineNumber]);
                }

                writer.ResetColor();
                writer.WriteLine();
            }
        }
    }
}
