using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityLearningCompanion
{
    /// Anything that turns a question plus facts into text. A remote LLM is one implementation.
    /// Remote providers are optional and must be enabled explicitly. No API keys are stored in this package.
    public interface IExplainer
    {
        string Name { get; }
        string Explain(string question, ScanReport report, PayloadPolicy policy);
    }

    /// Offline explainer. It matches the question to concept keywords and cites the verified and inferred facts it has.
    /// Works with no network and no key, and is the default.
    public sealed class LocalExplainer : IExplainer
    {
        public string Name => "local";

        public string Explain(string question, ScanReport report, PayloadPolicy policy)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            var sb = new StringBuilder();
            string q = question ?? string.Empty;
            IEnumerable<Concept> matches = ConceptLibrary.All.Where(c =>
                c.Keywords.Any(k => q.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0));

            foreach (Concept concept in matches)
            {
                sb.Append("## ").Append(concept.Title).Append('\n').Append(concept.Summary).Append('\n');
                foreach (string pitfall in concept.Pitfalls)
                {
                    sb.Append("- Pitfall: ").Append(pitfall).Append('\n');
                }

                sb.Append('\n');
            }

            if (sb.Length == 0)
            {
                sb.Append("No concept matched that question. Try a keyword such as asmdef, Awake, ScriptableObject or EditorWindow.\n\n");
            }

            sb.Append("Facts in this project:\n");
            foreach (Fact fact in report.Facts.Where(f => f.Confidence == Confidence.Verified).Take(10))
            {
                sb.Append("- [verified] ").Append(fact.Subject).Append(' ').Append(fact.Detail).Append(" (").Append(fact.Source).Append(")\n");
            }

            foreach (Fact fact in report.InferredFacts.Take(5))
            {
                sb.Append("- [inferred] ").Append(fact.Subject).Append(' ').Append(fact.Detail).Append(" (").Append(fact.Source).Append(")\n");
            }

            foreach (Fact warning in report.Warnings)
            {
                sb.Append("- [warning] ").Append(warning.Subject).Append(": ").Append(warning.Detail).Append(" (").Append(warning.Source).Append(")\n");
            }

            return sb.ToString();
        }
    }

    /// Uses a remote provider only when one is set and the policy allows it. Otherwise falls back to LocalExplainer.
    /// The payload is built by PayloadPolicy, so a provider never receives raw files by accident.
    public sealed class ExplainerRouter : IExplainer
    {
        private readonly IExplainer _remote;
        private readonly IExplainer _local = new LocalExplainer();

        public ExplainerRouter(IExplainer remote)
        {
            _remote = remote;
        }

        public string Name => _remote == null ? "local" : _remote.Name;

        public string Explain(string question, ScanReport report, PayloadPolicy policy)
        {
            if (_remote == null)
            {
                return _local.Explain(question, report, policy);
            }

            try
            {
                return _remote.Explain(question, report, policy);
            }
            catch (Exception)
            {
                // A remote failure should never block learning. Fall back and say so.
                return "[remote provider failed; showing local answer]\n" + _local.Explain(question, report, policy);
            }
        }
    }
}
