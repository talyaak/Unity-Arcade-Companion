using System;

namespace UnityLearningCompanion
{
    /// Scanned source is data. Nothing in the scanner executes or follows instructions found in it.
    public sealed class SourceFile
    {
        public SourceFile(string projectPath, string text)
        {
            ProjectPath = projectPath ?? throw new ArgumentNullException(nameof(projectPath));
            Text = text ?? string.Empty;
            Hash = Fnv1a64(Text);
        }

        /// Path relative to the project root, with forward slashes, e.g. "Assets/PulseArena/Core/PulseArena.Core.asmdef".
        public string ProjectPath { get; }
        public string Text { get; }

        /// Content hash used to detect changes on refresh.
        public ulong Hash { get; }

        public string Directory
        {
            get
            {
                int slash = ProjectPath.LastIndexOf('/');
                return slash < 0 ? string.Empty : ProjectPath.Substring(0, slash);
            }
        }

        public static ulong Fnv1a64(string text)
        {
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            ulong hash = offset;
            foreach (char c in text)
            {
                hash ^= c;
                hash *= prime;
            }

            return hash;
        }
    }
}
