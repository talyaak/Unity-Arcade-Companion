using System;
using System.Collections.Generic;
using System.Globalization;

namespace PulseArena.Core
{
    /// Minimal storage contract. The Game layer implements it with PlayerPrefs; tests use the in-memory version.
    public interface IKeyValueStore
    {
        /// Returns null when the key is absent.
        string Get(string key);

        void Set(string key, string value);
    }

    public sealed class InMemoryKeyValueStore : IKeyValueStore
    {
        private readonly Dictionary<string, string> _values = new Dictionary<string, string>(StringComparer.Ordinal);

        public string Get(string key) => _values.TryGetValue(key, out string v) ? v : null;

        public void Set(string key, string value) => _values[key] = value;
    }

    /// Saves and loads one round's standings as tab-separated lines. Malformed lines are skipped and counted,
    /// so one corrupt record cannot hide the rest of the board.
    public sealed class LeaderboardStore
    {
        private readonly IKeyValueStore _store;

        public LeaderboardStore(IKeyValueStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public static string KeyFor(string roundId) => "pulsearena.leaderboard." + roundId;

        public void Save(string roundId, IReadOnlyList<LeaderboardEntry> entries)
        {
            var lines = new List<string>();
            foreach (LeaderboardEntry e in entries)
            {
                if (e.PlayerId.IndexOf('\t') >= 0 || e.PlayerId.IndexOf('\n') >= 0)
                {
                    throw new ArgumentException("Player ids cannot contain tabs or newlines: " + e.PlayerId);
                }

                lines.Add(string.Join("\t",
                    e.PlayerId,
                    e.Score.ToString(CultureInfo.InvariantCulture),
                    e.BestCombo.ToString(CultureInfo.InvariantCulture),
                    e.Hits.ToString(CultureInfo.InvariantCulture),
                    e.Misses.ToString(CultureInfo.InvariantCulture),
                    e.SubmittedAtMillis.ToString(CultureInfo.InvariantCulture)));
            }

            _store.Set(KeyFor(roundId), string.Join("\n", lines));
        }

        public IReadOnlyList<LeaderboardEntry> Load(string roundId, out int skippedLines)
        {
            skippedLines = 0;
            var entries = new List<LeaderboardEntry>();
            string raw = _store.Get(KeyFor(roundId));
            if (string.IsNullOrEmpty(raw))
            {
                return entries;
            }

            foreach (string line in raw.Split('\n'))
            {
                string[] parts = line.Split('\t');
                if (parts.Length != 6
                    || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int score)
                    || !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int combo)
                    || !int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int hits)
                    || !int.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int misses)
                    || !long.TryParse(parts[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out long time)
                    || parts[0].Length == 0)
                {
                    skippedLines++;
                    continue;
                }

                entries.Add(new LeaderboardEntry(parts[0], score, combo, hits, misses, time));
            }

            entries.Sort(LeaderboardOrder.Instance);
            return entries;
        }
    }
}
