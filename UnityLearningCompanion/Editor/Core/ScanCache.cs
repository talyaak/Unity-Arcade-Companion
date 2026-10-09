using System.Collections.Generic;
using System.Linq;

namespace UnityLearningCompanion
{
    /// Remembers what was scanned, by content hash, so a refresh rescans only changed files.
    public sealed class ScanCache
    {
        private readonly Dictionary<string, ulong> _seen = new Dictionary<string, ulong>();

        /// Returns the files whose content changed or are new since the last call, and forgets files that disappeared.
        public IReadOnlyList<SourceFile> Changed(IReadOnlyList<SourceFile> current)
        {
            var changed = new List<SourceFile>();
            foreach (SourceFile file in current)
            {
                if (!_seen.TryGetValue(file.ProjectPath, out ulong previous) || previous != file.Hash)
                {
                    changed.Add(file);
                }

                _seen[file.ProjectPath] = file.Hash;
            }

            var currentPaths = new HashSet<string>(current.Select(f => f.ProjectPath));
            foreach (string gone in _seen.Keys.Where(p => !currentPaths.Contains(p)).ToList())
            {
                _seen.Remove(gone);
            }

            return changed;
        }
    }
}
