using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PulseArena.Core
{
    /// Time comes from outside so diagnostics can be tested without a clock. The Game layer supplies the real one.
    public interface ITimeSource
    {
        long UnixMillis { get; }
    }

    public sealed class FixedTimeSource : ITimeSource
    {
        public FixedTimeSource(long unixMillis)
        {
            UnixMillis = unixMillis;
        }

        public long UnixMillis { get; set; }
    }

    public enum DiagnosticLevel
    {
        Info,
        Warning,
        Error
    }

    public sealed class DiagnosticEvent
    {
        public DiagnosticEvent(long atMillis, DiagnosticLevel level, string code, string message)
        {
            AtMillis = atMillis;
            Level = level;
            Code = code;
            Message = message;
        }

        public long AtMillis { get; }
        public DiagnosticLevel Level { get; }

        /// Stable machine-readable code, e.g. "submission.rejected". Dashboards and alerts key on this, not on Message.
        public string Code { get; }
        public string Message { get; }
    }

    /// Bounded in-memory log with per-code counters. Oldest events drop first, so memory stays flat on long sessions.
    /// Convention: Message carries values needed to act on an event, never raw player ids or input logs.
    public sealed class DiagnosticsLog
    {
        public const int MaxMessageLength = 200;

        private readonly ITimeSource _clock;
        private readonly int _capacity;
        private readonly LinkedList<DiagnosticEvent> _events = new LinkedList<DiagnosticEvent>();
        private readonly Dictionary<string, int> _counts = new Dictionary<string, int>(StringComparer.Ordinal);

        public DiagnosticsLog(ITimeSource clock, int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Must be positive.");
            }

            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _capacity = capacity;
        }

        public IReadOnlyCollection<DiagnosticEvent> Events => _events;

        public void Record(DiagnosticLevel level, string code, string message)
        {
            if (string.IsNullOrEmpty(code))
            {
                throw new ArgumentException("Code is required.", nameof(code));
            }

            string safeMessage = message ?? string.Empty;
            if (safeMessage.Length > MaxMessageLength)
            {
                safeMessage = safeMessage.Substring(0, MaxMessageLength);
            }

            _events.AddLast(new DiagnosticEvent(_clock.UnixMillis, level, code, safeMessage));
            if (_events.Count > _capacity)
            {
                _events.RemoveFirst();
            }

            _counts[code] = Count(code) + 1;
        }

        /// Lifetime count for a code. Unlike Events, this is not bounded by capacity.
        public int Count(string code)
        {
            return _counts.TryGetValue(code, out int n) ? n : 0;
        }

        public string ExportText()
        {
            var sb = new StringBuilder();
            foreach (DiagnosticEvent e in _events)
            {
                sb.Append(e.AtMillis).Append(' ').Append(e.Level.ToString().ToUpperInvariant())
                  .Append(' ').Append(e.Code).Append(' ').Append(e.Message).Append('\n');
            }

            return sb.ToString();
        }
    }
}
