using System.Collections;

namespace Wave
{
    public sealed class Diagnostic
    {
        public Diagnostic(TextLocation location, string message, string? suggestion = null)
        {
            Location = location;
            Message = message;
            Suggestion = suggestion;
        }

        public TextLocation Location { get; }
        public string Message { get; }
        public string? Suggestion { get; }
        public override string ToString() => Message;
    }

    public sealed class DiagnosticBag : IEnumerable<Diagnostic>
    {
        private readonly List<Diagnostic> _diagnostics = new();
        public void Report(TextLocation location, string message, string? suggestion = null) => _diagnostics.Add(new(location, message, suggestion));
        public IEnumerator<Diagnostic> GetEnumerator() => _diagnostics.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public void AddRange(DiagnosticBag diagnostics) => _diagnostics.AddRange(diagnostics);
        public void AddRange(IEnumerable<Diagnostic> diagnostics) => _diagnostics.AddRange(diagnostics);
    }
}
