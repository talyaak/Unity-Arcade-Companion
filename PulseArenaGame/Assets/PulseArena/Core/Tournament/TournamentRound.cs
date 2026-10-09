using System;
using System.Collections.Generic;
using System.Linq;

namespace PulseArena.Core
{
    public enum SubmissionStatus
    {
        Accepted,
        WrongConfigVersion,
        WrongSeed,
        AfterDeadline,
        AlreadySubmitted,
        InvalidReplay,
        ScoreMismatch
    }

    /// What a client sends to the round. Local simulation builds these directly; a real backend would receive them over a network.
    public sealed class TournamentSubmission
    {
        public TournamentSubmission(string playerId, ulong seed, string configVersion, int claimedScore, long submittedAtMillis, IReadOnlyList<TapRecord> taps)
        {
            PlayerId = playerId ?? throw new ArgumentNullException(nameof(playerId));
            Seed = seed;
            ConfigVersion = configVersion;
            ClaimedScore = claimedScore;
            SubmittedAtMillis = submittedAtMillis;
            Taps = taps ?? throw new ArgumentNullException(nameof(taps));
        }

        public string PlayerId { get; }
        public ulong Seed { get; }
        public string ConfigVersion { get; }
        public int ClaimedScore { get; }
        public long SubmittedAtMillis { get; }
        public IReadOnlyList<TapRecord> Taps { get; }
    }

    public sealed class LeaderboardEntry
    {
        public LeaderboardEntry(string playerId, int score, int bestCombo, int hits, int misses, long submittedAtMillis)
        {
            PlayerId = playerId;
            Score = score;
            BestCombo = bestCombo;
            Hits = hits;
            Misses = misses;
            SubmittedAtMillis = submittedAtMillis;
        }

        public string PlayerId { get; }
        public int Score { get; }
        public int BestCombo { get; }
        public int Hits { get; }
        public int Misses { get; }
        public long SubmittedAtMillis { get; }
    }

    /// One fair round: everyone plays the same seed and config. Each player gets one accepted submission.
    /// Rejected submissions do not use the player's slot, so a client bug can be fixed and resent before the deadline.
    /// Acceptance means "consistent with the replay". It does not prove the taps were human (see docs).
    public sealed class TournamentRound
    {
        private readonly ChallengeConfig _config;
        private readonly Dictionary<string, LeaderboardEntry> _entries = new Dictionary<string, LeaderboardEntry>(StringComparer.Ordinal);

        public TournamentRound(string id, ChallengeConfig config, ulong seed, long closesAtMillis)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Round id is required.", nameof(id));
            }

            _config = config ?? throw new ArgumentNullException(nameof(config));
            Id = id;
            Seed = seed;
            ClosesAtMillis = closesAtMillis;
        }

        public string Id { get; }
        public ulong Seed { get; }
        public long ClosesAtMillis { get; }
        public string ConfigVersion => _config.Version;

        /// Accepted entries, ranked: score, then best combo, then earlier submission, then player id (ordinal).
        public IReadOnlyList<LeaderboardEntry> Standings()
        {
            return _entries.Values
                .OrderByDescending(e => e.Score)
                .ThenByDescending(e => e.BestCombo)
                .ThenBy(e => e.SubmittedAtMillis)
                .ThenBy(e => e.PlayerId, StringComparer.Ordinal)
                .ToList();
        }

        public SubmissionStatus Submit(TournamentSubmission submission)
        {
            if (submission == null)
            {
                throw new ArgumentNullException(nameof(submission));
            }

            if (submission.ConfigVersion != _config.Version)
            {
                return SubmissionStatus.WrongConfigVersion;
            }

            if (submission.Seed != Seed)
            {
                return SubmissionStatus.WrongSeed;
            }

            if (submission.SubmittedAtMillis > ClosesAtMillis)
            {
                return SubmissionStatus.AfterDeadline;
            }

            if (_entries.ContainsKey(submission.PlayerId))
            {
                return SubmissionStatus.AlreadySubmitted;
            }

            ChallengeResult replay;
            try
            {
                replay = ReplayVerifier.Verify(_config, Seed, submission.Taps);
            }
            catch (ArgumentException)
            {
                return SubmissionStatus.InvalidReplay;
            }

            if (replay.Score != submission.ClaimedScore)
            {
                return SubmissionStatus.ScoreMismatch;
            }

            _entries[submission.PlayerId] = new LeaderboardEntry(
                submission.PlayerId,
                replay.Score,
                replay.BestCombo,
                replay.Hits,
                replay.Misses,
                submission.SubmittedAtMillis);
            return SubmissionStatus.Accepted;
        }
    }
}
