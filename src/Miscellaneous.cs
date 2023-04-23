using System.Collections.Immutable;
using Wave.Source.Binding.BoundNodes;

namespace Wave
{
    public readonly struct TextSpan
    {
        public TextSpan(int start, int length)
        {
            Start = start;
            Length = length;
        }

        public int Start { get; }
        public int Length { get; }
        public int End => Start + Length;
        public static TextSpan From(int start, int end) => new(start, end - start);
    }

    public sealed class SourceText
    {
        public ImmutableArray<TextLine> Lines { get; }
        private readonly string _text;
        private SourceText(string text, string fileName)
        {
            Lines = ParseLines(this, text);
            _text = text;
            FileName = fileName;
        }

        public static SourceText From(string text, string fileName = "") => new(text, fileName);
        private static ImmutableArray<TextLine> ParseLines(SourceText source, string text)
        {
            ImmutableArray<TextLine>.Builder result = ImmutableArray.CreateBuilder<TextLine>();

            int position = 0;
            int lineStart = 0;
            while (position < text.Length)
            {
                int lineBreakWidth = GetLineBreakWidth(text, position);
                if (lineBreakWidth == 0)
                    ++position;
                else
                {
                    AddLine(result, source, position, lineStart, lineBreakWidth);
                    position += lineBreakWidth;
                    lineStart = position;
                }
            }

            if (position >= lineStart)
                AddLine(result, source, position, lineStart, 0);

            return result.ToImmutable();
        }

        public int GetLineIndex(int position)
        {
            int lower = 0, upper = Lines.Length - 1;
            while (lower <= upper)
            {
                int index = lower + (upper - lower) / 2;
                int start = Lines[index].Start;

                if (start == position)
                    return index;
                else if (start > position)
                    upper = index - 1;
                else
                    lower = index + 1;
            }

            return lower - 1;
        }

        private static void AddLine(ImmutableArray<TextLine>.Builder result, SourceText source, int position, int lineStart, int lineBreakWidth)
            => result.Add(new TextLine(source, lineStart, position - lineStart, position - lineStart + lineBreakWidth));

        private static int GetLineBreakWidth(string text, int i)
        {
            char c = text[i];
            char l = i + 1 >= text.Length ? '\0' : text[i + 1];
            if (c == '\r' && l == '\n')
                return 2;
            else if (c == '\r' || l == '\n')
                return 1;
            return 0;
        }

        public override string ToString() => _text;
        public string ToString(int start, int length) => _text.Substring(start, length);
        public string ToString(TextSpan span) => _text.Substring(span.Start, span.Length);
        public string this[Range range] => _text[range];
        public char this[int position] => _text[position];
        public int Length => _text.Length;
        public string FileName { get; }
    }

    public sealed class TextLocation
    {
        public TextLocation(SourceText source, TextSpan span)
        {
            Source = source;
            Span = span;
        }

        public SourceText Source { get; }
        public string Text => Source[Span.Start..Span.End];
        public string FileName => Source.FileName;
        public TextSpan Span { get; }
        public int StartLine => Source.GetLineIndex(Span.Start);
        public int StartColumn => Span.Start - Source.Lines[StartLine].Start;
        public int EndLine => Source.GetLineIndex(Span.End);
        public int EndColumn => Span.End - Source.Lines[EndLine].Start;
    }

    public sealed class TextLine
    {
        public TextLine(SourceText source, int start, int length, int lengthWithLineBreak)
        {
            Source = source;
            Start = start;
            Length = length;
            LengthWithLineBreak = lengthWithLineBreak;
        }

        public SourceText Source { get; }
        public int Start { get; }
        public int Length { get; }
        public int End => Start + Length;
        public int LengthWithLineBreak { get; }
        public TextSpan Span => new(Start, Length);
        public TextSpan SpanWithLineBreak => new(Start, LengthWithLineBreak);
        public override string ToString() => Source.ToString(Span);
    }

    public static class Extensions
    {
        public static string? Stringify(this object? v)
        {
            switch (v)
            {
                case Array arr:
                {
                    string res = "[";
                    IEnumerable<object> en = arr.Cast<object>();
                    if (arr.Length > 0)
                        res += en.First().Stringify() ?? "";

                    for (int i = 1; i < arr.Length; ++i)
                        res += ", " + ((object?)en.ElementAt(i)).Stringify();
                    return res + "]";
                }
                case bool b:
                    return b ? "true" : "false";
                case ClassInstance c:
                    return c.ToString();
                default:
                    return Convert.ToString(v);
            }
        }
    }

    internal static class IDs
    {
        public static int InstanceID = 0;
    }

    internal class ClassInstance
    {
        public ClassInstance(string name, Dictionary<string, BoundBlockStmt> fns, Dictionary<string, object?> fields)
        {
            Name = name;
            Fns = fns;
            Fields = fields;
            _id = IDs.InstanceID++;
        }

        public string Name { get; }
        public Dictionary<string, BoundBlockStmt> Fns { get; }
        public Dictionary<string, object?> Fields { get; }
        private readonly int _id;
        public override int GetHashCode() => _id;
        public override string ToString() => Name;
    }
}
