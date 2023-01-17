using Wave.Nodes;
using Wave.Syntax;

namespace Wave.IO
{
    public static class TextWriterExtensions
    {
        public static void SetForeground(this TextWriter writer, ConsoleColor color)
        {
            if (writer == Console.Out)
                Console.ForegroundColor = color;
        }

        public static void ResetColor(this TextWriter writer)
        {
            if (writer == Console.Out)
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
    }
}
