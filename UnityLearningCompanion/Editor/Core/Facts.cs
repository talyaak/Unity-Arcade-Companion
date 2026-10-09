using System;

namespace UnityLearningCompanion
{
    /// Verified: read directly from a file's text (e.g. an asmdef reference list).
    /// Inferred: derived by pattern matching (e.g. a base class name suggests a Unity type). Always say so in the UI.
    public enum Confidence
    {
        Verified,
        Inferred
    }

    public enum FactKind
    {
        Assembly,
        AssemblyReference,
        Namespace,
        Import,
        Type,
        BaseType,
        Warning
    }

    public sealed class Fact
    {
        public Fact(FactKind kind, Confidence confidence, string subject, string detail, string source)
        {
            Kind = kind;
            Confidence = confidence;
            Subject = subject ?? string.Empty;
            Detail = detail ?? string.Empty;
            Source = source ?? string.Empty;
        }

        public FactKind Kind { get; }
        public Confidence Confidence { get; }
        public string Subject { get; }
        public string Detail { get; }

        /// Project-relative path, with line number where it applies. Every fact must have one.
        public string Source { get; }

        public override string ToString()
        {
            return $"[{Confidence.ToString().ToLowerInvariant()}] {Kind}: {Subject} {Detail} ({Source})";
        }
    }
}
