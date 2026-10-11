using System.Text.RegularExpressions;

namespace UnityLearningCompanion
{
    /// Removes secret-looking text before anything leaves the machine. Pattern-based, so it is a last line of defence:
    /// the default is to send no source at all (see PayloadPolicy).
    public static class Redactor
    {
        public const string Placeholder = "[REDACTED]";

        private static readonly Regex[] Patterns =
        {
            // key = "value" style assignments for common secret names
            new Regex(@"(?i)(api[_-]?key|secret|token|password|passwd)\s*[:=]\s*[""']?[^\s""';,]+"),
            // provider-style keys: sk-..., AKIA..., ghp_...
            new Regex(@"\b(sk-[A-Za-z0-9_\-]{12,}|AKIA[0-9A-Z]{12,}|ghp_[A-Za-z0-9]{20,})\b"),
            // long opaque runs that look like tokens
            new Regex(@"\b[A-Za-z0-9+/_\-]{32,}\b")
        };

        public static string Redact(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            string result = text;
            foreach (Regex pattern in Patterns)
            {
                result = pattern.Replace(result, Placeholder);
            }

            return result;
        }
    }
}
