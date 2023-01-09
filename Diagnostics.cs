﻿using System.Collections;
using System.Collections.Immutable;

namespace Wave
{
    public sealed class Diagnostic
    {
        public Diagnostic(TextSpan span, string message)
        {
            Span = span;
            Message = message;
        }

        public TextSpan Span { get; }
        public string Message { get; }
        public override string ToString() => Message;
    }

    public sealed class DiagnosticBag : IEnumerable<Diagnostic>
    {
        private readonly List<Diagnostic> _diagnostics = new();
        public void Report(TextSpan span, string message) => _diagnostics.Add(new(span, message));
        public IEnumerator<Diagnostic> GetEnumerator() => _diagnostics.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void AddRange(DiagnosticBag diagnostics) => _diagnostics.AddRange(diagnostics);
        public void AddRange(ImmutableArray<Diagnostic> diagnostics) => _diagnostics.AddRange(diagnostics);
    }
}
