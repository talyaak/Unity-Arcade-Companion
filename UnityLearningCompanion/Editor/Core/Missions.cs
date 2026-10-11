using System.Collections.Generic;
using System.Linq;

namespace UnityLearningCompanion
{
    public sealed class Mission
    {
        public Mission(string id, string prompt, string source)
        {
            Id = id;
            Prompt = prompt;
            Source = source;
        }

        public string Id { get; }
        public string Prompt { get; }

        /// The file or line the mission is about, so the learner can go and look.
        public string Source { get; }
    }

    /// Builds missions from facts that were actually found. Missions are deterministic for a given report.
    public static class MissionGenerator
    {
        public static IReadOnlyList<Mission> From(ScanReport report)
        {
            var missions = new List<Mission>();

            foreach (Fact fact in report.Facts.Where(f => f.Kind == FactKind.Assembly && f.Detail.Contains("noEngineReferences")))
            {
                missions.Add(new Mission("engine-free-" + fact.Subject,
                    $"Find one class in {fact.Subject} and explain why it can compile without UnityEngine.",
                    fact.Source));
            }

            foreach (Fact fact in report.Facts.Where(f => f.Kind == FactKind.BaseType && f.Subject.Length > 0 && f.Detail.Contains("MonoBehaviour")))
            {
                missions.Add(new Mission("lifecycle-" + fact.Subject,
                    $"Trace which lifecycle methods {fact.Subject} uses and in what order Unity calls them.",
                    fact.Source));
            }

            foreach (Fact warning in report.Warnings)
            {
                missions.Add(new Mission("fix-" + warning.Source,
                    $"Explain the rule broken at {warning.Source} ({warning.Detail}), then decide the fix. The tool will not edit the file.",
                    warning.Source));
            }

            return missions;
        }
    }
}
