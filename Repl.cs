using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Wave.Nodes;
using Wave.Symbols;
using Wave.Syntax;
using Wave.Syntax.Nodes;

namespace Wave
{
    internal abstract class Repl
    {
        private readonly List<string> _history = new();
        private int _historyIndex = 0;
        private bool _done = false;
        public void Run()
        {
            while (true)
            {
                string? text = EditSubmission();
                if (string.IsNullOrEmpty(text))
                    continue;

                if (!text.Contains(Environment.NewLine) && text.StartsWith("#"))
                    EvaluateMetaCommand(text);
                else
                    EvaluateSubmission(text);

                _history.Add(text);
                _historyIndex = 0;
            }
        }

        private sealed class SubmissionView
        {
            private readonly Action<string> _lineRenderer;
            private readonly ObservableCollection<string> _document;
            private int _cursorTop;
            private int _renderedLineCount, _currentLine, _currentColumn;

            public SubmissionView(Action<string> lineRenderer, ObservableCollection<string> document)
            {
                _lineRenderer = lineRenderer;
                _document = document;
                _document.CollectionChanged += SubmissionDocumentChanged;
                _cursorTop = Console.CursorTop;
                Render();
            }

            private void SubmissionDocumentChanged(object? sender, NotifyCollectionChangedEventArgs e) => Render();
            private void Render()
            {
                Console.CursorVisible = false;
                int lineCount = 0;
                foreach (string line in _document)
                {
                    if (_cursorTop + lineCount >= Console.WindowHeight)
                    {
                        Console.SetCursorPosition(0, Console.WindowHeight - 1);
                        Console.WriteLine();
                        if (_cursorTop > 0)
                            --_cursorTop;
                    }

                    Console.SetCursorPosition(0, _cursorTop + lineCount);
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    if (lineCount == 0)
                        Console.Write("» ");
                    else
                        Console.Write("· ");

                    Console.ResetColor();
                    _lineRenderer(line);
                    Console.Write(new string(' ', Console.WindowWidth - line.Length));
                    ++lineCount;
                }

                int numOfBlankLines = _renderedLineCount - lineCount;
                if (numOfBlankLines > 0)
                {
                    string blankLine = new(' ', Console.WindowWidth);
                    for (int i = 0; i < numOfBlankLines; ++i)
                    {
                        Console.SetCursorPosition(0, _cursorTop + lineCount);
                        Console.WriteLine(blankLine);
                    }
                }

                _renderedLineCount = lineCount;
                Console.CursorVisible = true;
                UpdateCursorPosition();
            }

            private void UpdateCursorPosition() => Console.SetCursorPosition(2 + _currentColumn, _cursorTop + _currentLine);
            public int CurrentLine
            {
                get => _currentLine;
                set
                {
                    if (_currentLine != value)
                    {
                        _currentLine = value;
                        _currentColumn = Math.Min(_document[_currentLine].Length, _currentColumn);
                        UpdateCursorPosition();
                    }
                }
            }

            public int CurrentColumn
            {
                get => _currentColumn;
                set
                {
                    if (_currentColumn != value)
                    {
                        _currentColumn = value;
                        UpdateCursorPosition();
                    }
                }
            }
        }

        private string? EditSubmission()
        {
            _done = false;
            ObservableCollection<string> document = new() { "" };
            SubmissionView view = new(RenderLine, document);
            while (!_done)
                HandleKey(Console.ReadKey(true), document, view);

            view.CurrentLine = document.Count - 1;
            view.CurrentColumn = document[view.CurrentLine].Length;

            Console.WriteLine();
            return string.Join(Environment.NewLine, document);
        }

        private void HandleKey(ConsoleKeyInfo key, ObservableCollection<string> document, SubmissionView view)
        {
            if (key.Modifiers == default)
                switch (key.Key)
                {
                    case ConsoleKey.Escape:
                        HandleEscape(document, view);
                        break;
                    case ConsoleKey.Enter:
                        HandleEnter(document, view);
                        break;
                    case ConsoleKey.LeftArrow:
                        HandleLeftArrow(view);
                        break;
                    case ConsoleKey.RightArrow:
                        HandleRightArrow(document, view);
                        break;
                    case ConsoleKey.UpArrow:
                        HandleUpArrow(view);
                        break;
                    case ConsoleKey.DownArrow:
                        HandleDownArrow(document, view);
                        break;
                    case ConsoleKey.Backspace:
                        HandleBackspace(document, view);
                        break;
                    case ConsoleKey.Delete:
                        HandleDelete(document, view);
                        break;
                    case ConsoleKey.Home:
                        HandleHome(view);
                        break;
                    case ConsoleKey.End:
                        HandleEnd(document, view);
                        break;
                    case ConsoleKey.Tab:
                        HandleTab(document, view);
                        break;
                    case ConsoleKey.PageUp:
                        HandlePageUp(document, view);
                        break;
                    case ConsoleKey.PageDown:
                        HandlePageDown(document, view);
                        break;
                }
            else if (key.Modifiers == ConsoleModifiers.Control)
                switch (key.Key)
                {
                    case ConsoleKey.Enter:
                        HandleCtrlEnter();
                        break;
                }

            if (key.KeyChar >= ' ')

                HandleTyping(document, view, key.KeyChar.ToString());
        }

        private static void HandleEscape(ObservableCollection<string> document, SubmissionView view)
        {
            document.Clear();
            document.Add(string.Empty);
            view.CurrentLine = 0;
            view.CurrentColumn = 0;
        }

        private void HandleEnter(ObservableCollection<string> document, SubmissionView view)
        {
            string text = string.Join(Environment.NewLine, document);
            if (text.StartsWith("#") || IsCompleteSubmission(text))
            {
                _done = true;
                return;
            }

            InsertLine(document, view);
        }

        private void HandleCtrlEnter() => _done = true;
        private static void InsertLine(ObservableCollection<string> document, SubmissionView view)
        {
            string remaining = document[view.CurrentLine][view.CurrentColumn..];
            document[view.CurrentLine] = document[view.CurrentLine][..view.CurrentColumn];

            int lineIndex = view.CurrentLine + 1;
            document.Insert(lineIndex, remaining);
            view.CurrentColumn = 0;
            view.CurrentLine = lineIndex;
        }

        private static void HandleLeftArrow(SubmissionView view)
        {
            if (view.CurrentColumn > 0)
                --view.CurrentColumn;
        }

        private static void HandleRightArrow(ObservableCollection<string> document, SubmissionView view)
        {
            if (view.CurrentColumn <= document[view.CurrentLine].Length - 1)
                ++view.CurrentColumn;
        }

        private static void HandleUpArrow(SubmissionView view)
        {
            if (view.CurrentLine > 0)
                --view.CurrentLine;
        }

        private static void HandleDownArrow(ObservableCollection<string> document, SubmissionView view)
        {
            if (view.CurrentLine < document.Count - 1)
                ++view.CurrentLine;
        }

        private static void HandleBackspace(ObservableCollection<string> document, SubmissionView view)
        {
            int start = view.CurrentColumn;
            if (start == 0)
            {
                if (view.CurrentLine == 0)
                    return;

                string curLine = document[view.CurrentLine];
                string prevLine = document[view.CurrentLine - 1];
                document.RemoveAt(view.CurrentLine);
                --view.CurrentLine;
                document[view.CurrentLine] = prevLine + curLine;
                view.CurrentColumn = prevLine.Length;
            }
            else
            {
                int lineIndex = view.CurrentLine;
                string line = document[lineIndex];
                string before = line[..(start - 1)];
                string after = line[start..];
                document[lineIndex] = before + after;
                --view.CurrentColumn;
            }
        }

        private static void HandleDelete(ObservableCollection<string> document, SubmissionView view)
        {
            int lineIndex = view.CurrentLine;
            string line = document[lineIndex];
            int start = view.CurrentColumn;
            if (start >= line.Length)
            {
                if (view.CurrentLine >= document.Count - 1)
                    return;

                string nextLine = document[view.CurrentLine + 1];
                document[view.CurrentLine] = nextLine;
                document.RemoveAt(view.CurrentLine + 1);
            }
            else
            {
                string before = line[..start];
                string after = line[(start + 1)..];
                document[lineIndex] = before + after;
            }
        }

        private static void HandleHome(SubmissionView view) => view.CurrentColumn = 0;
        private static void HandleEnd(ObservableCollection<string> document, SubmissionView view) => view.CurrentColumn = document[view.CurrentLine].Length;
        private static void HandleTab(ObservableCollection<string> document, SubmissionView view)
        {
            const int TabWidth = 4;
            int start = view.CurrentColumn;
            int remainingSpaces = TabWidth - start % TabWidth;
            string line = document[view.CurrentLine];
            document[view.CurrentLine] = line.Insert(start, new string(' ', remainingSpaces));
            view.CurrentColumn += remainingSpaces;
        }

        private void HandlePageUp(ObservableCollection<string> document, SubmissionView view)
        {
            --_historyIndex;
            if (_historyIndex < 0)
                _historyIndex = _history.Count - 1;

            UpdateDocumentFromHistory(document, view);
        }

        private void HandlePageDown(ObservableCollection<string> document, SubmissionView view)
        {
            ++_historyIndex;
            if (_historyIndex >= _history.Count)
                _historyIndex = 0;

            UpdateDocumentFromHistory(document, view);
        }

        private void UpdateDocumentFromHistory(ObservableCollection<string> document, SubmissionView view)
        {
            if (!_history.Any())
                return;

            document.Clear();
            string historyItem = _history[_historyIndex];
            string[] lines = historyItem.Split(Environment.NewLine);
            foreach (string line in lines)
                document.Add(line);

            view.CurrentLine = document.Count - 1;
            view.CurrentColumn = document[view.CurrentLine].Length;
        }

        private static void HandleTyping(ObservableCollection<string> document, SubmissionView view, string text)
        {
            int lineIndex = view.CurrentLine;
            int start = view.CurrentColumn;
            document[lineIndex] = document[lineIndex].Insert(start, text);
            view.CurrentColumn += text.Length;
        }

        protected void ClearHistory() => _history.Clear();
        protected virtual void RenderLine(string line) => Console.Write(line);

        protected virtual void EvaluateMetaCommand(string input)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Invalid command \"{input}\".\n");
            Console.ResetColor();
        }

        protected abstract bool IsCompleteSubmission(string text);
        protected abstract void EvaluateSubmission(string text);
    }

    internal sealed class WaveRepl : Repl
    {
        private Compilation? _previous = null;
        private bool _showTree = false, _showProgram = false;
        private readonly Dictionary<VariableSymbol, object?> _vars = new();

        protected override void RenderLine(string line)
        {
            ImmutableArray<Token> tokens = SyntaxTree.ParseTokens(line);
            foreach (Token token in tokens)
            {
                if (token.Kind == SyntaxKind.Int || token.Kind == SyntaxKind.Float || token.Kind == SyntaxKind.True || token.Kind == SyntaxKind.False)
                    Console.ForegroundColor = ConsoleColor.Cyan;
                else if (SyntaxFacts.GetKeyWordKind(token.Lexeme ?? "?") != SyntaxKind.Identifier)
                    Console.ForegroundColor = ConsoleColor.DarkMagenta;
                else if (token.Kind == SyntaxKind.Identifier)
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                else if (token.Kind == SyntaxKind.String)
                    Console.ForegroundColor = ConsoleColor.Green;
                else
                    Console.ForegroundColor = ConsoleColor.DarkGray;

                Console.Write(token.Lexeme);
                Console.ResetColor();
            }
        }

        protected override void EvaluateMetaCommand(string input)
        {
            switch (input.ToLower())
            {
                case "#tree":
                    _showTree = !_showTree;
                    Console.WriteLine($"{(_showTree ? "Enabled" : "Disabled")} showing tree.\n");
                    break;
                case "#program":
                    _showProgram = !_showProgram;
                    Console.WriteLine($"{(_showProgram ? "Enabled" : "Disabled")} showing program.\n");
                    break;
                case "#cls":
                    Console.Clear();
                    break;
                case "#reset":
                    _previous = null;
                    break;
                default:
                    base.EvaluateMetaCommand(input);
                    break;
            }
        }

        protected override void EvaluateSubmission(string text)
        {
            SyntaxTree syntaxTree = SyntaxTree.Parse(text);

            Compilation compilation = _previous is null
                            ? new(syntaxTree)
                            : _previous.ContinueWith(syntaxTree);

            if (_showTree)
            {
                syntaxTree.Root.WriteTo(Console.Out);
                Console.WriteLine();
            }

            if (_showProgram)
            {
                compilation.EmitTree(Console.Out);
                Console.WriteLine();
            }

            EvaluationResult result = compilation.Evaluate(_vars);

            if (!result.Diagnostics.Any())
            {
                if (result.Value is not null)
                {
                    Console.ForegroundColor = ConsoleColor.DarkBlue;
                    Console.WriteLine(result.Value);
                    Console.ResetColor();
                }
                Console.WriteLine();
                _previous = compilation;
            }
            else
            {
                foreach (Diagnostic d in result.Diagnostics.OrderBy(d => d.Span, Comparer<TextSpan>.Create((TextSpan x, TextSpan y) => x.Start - y.Start == 0 ? x.Length - y.Length : x.Start - y.Start)))
                {
                    SourceText source = syntaxTree.Source;
                    int lineIndex = source.GetLineIndex(d.Span.Start);
                    int lineNumber = lineIndex + 1;
                    TextLine line = source.Lines[lineIndex];
                    int column = d.Span.Start - line.Start + 1;

                    string where = $"[{$"{lineNumber}:{column}"}]: ";
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.Write(where);
                    Console.ResetColor();

                    TextSpan prefixSpan = TextSpan.From(line.Start, d.Span.Start);
                    TextSpan suffixSpan = TextSpan.From(d.Span.End, line.End);

                    string prefix = source.ToString(prefixSpan),
                        error = source.ToString(d.Span),
                        suffix = source.ToString(suffixSpan);

                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write(prefix);

                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.Write(error);

                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine(suffix);

                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.Write(new string(' ', prefix.Length + where.Length));
                    Console.Write(new string('^', error.Length == 0 ? 1 : error.Length));
                    Console.WriteLine($" {d}\n");
                    Console.ResetColor();
                }
            }
        }

        protected override bool IsCompleteSubmission(string text)
        {
            if (string.IsNullOrEmpty(text))
                return true;

            if (text.Split(Environment.NewLine).Reverse().Where(string.IsNullOrEmpty).Take(2).Count() == 2)
                return true;

            SyntaxTree syntaxTree = SyntaxTree.Parse(text);
            return !syntaxTree.Root.Members.Last().GetLastToken().IsMissing;
        }
    }
}
