using System;

namespace UnityLearningCompanion
{
    /// Decides what may be sent to an LLM provider. Defaults are the most private option.
    public sealed class PayloadPolicy
    {
        /// Off by default. When off, only facts (names, paths, line numbers) are sent, never source text.
        public bool AllowSourceSnippets { get; set; }

        /// Hard cap on payload size. A full project is never sent.
        public int MaxChars { get; set; } = 2000;

        public string Build(string prompt, string factsSummary, string snippet)
        {
            var parts = new System.Collections.Generic.List<string> { Redactor.Redact(prompt), Redactor.Redact(factsSummary) };
            if (AllowSourceSnippets && !string.IsNullOrEmpty(snippet))
            {
                parts.Add(Redactor.Redact(snippet));
            }

            string payload = string.Join("\n\n", parts);
            if (payload.Length > MaxChars)
            {
                payload = payload.Substring(0, MaxChars);
            }

            return payload;
        }
    }
}
