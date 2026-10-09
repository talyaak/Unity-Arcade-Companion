using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityLearningCompanion
{
    public sealed class Question
    {
        public Question(string id, string prompt, string[] rubric)
        {
            Id = id;
            Prompt = prompt;
            Rubric = rubric;
        }

        public string Id { get; }
        public string Prompt { get; }

        /// Points an answer should cover. Each entry is a keyword or phrase.
        public string[] Rubric { get; }
    }

    public sealed class InterviewScore
    {
        public InterviewScore(IReadOnlyList<string> covered, IReadOnlyList<string> missing)
        {
            Covered = covered;
            Missing = missing;
        }

        public IReadOnlyList<string> Covered { get; }
        public IReadOnlyList<string> Missing { get; }
        public double Coverage => Covered.Count + Missing.Count == 0 ? 0 : (double)Covered.Count / (Covered.Count + Missing.Count);
    }

    /// Keyword coverage is a rough check, not a grade. It tells the learner which points they did not mention.
    public static class Interview
    {
        public static readonly IReadOnlyList<Question> Questions = new List<Question>
        {
            new Question("asmdef-why", "Why split a Unity project into assemblies?",
                new[] { "compile", "dependency", "reference", "test" }),
            new Question("lifecycle-order", "What is the difference between Awake and Start?",
                new[] { "Awake", "Start", "before", "enable" }),
            new Question("data-assets", "Why keep tuning values in a ScriptableObject?",
                new[] { "Inspector", "designer", "asset", "data" })
        };

        public static InterviewScore Score(Question question, string answer)
        {
            if (question == null)
            {
                throw new ArgumentNullException(nameof(question));
            }

            string text = answer ?? string.Empty;
            var covered = new List<string>();
            var missing = new List<string>();
            foreach (string point in question.Rubric)
            {
                if (text.IndexOf(point, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    covered.Add(point);
                }
                else
                {
                    missing.Add(point);
                }
            }

            return new InterviewScore(covered, missing);
        }
    }
}
