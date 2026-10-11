using System;
using System.Collections.Generic;

namespace PulseArena.Core
{
    /// Holds the active tuning, one staged candidate, and one step of rollback history.
    /// A candidate must pass validation and a simulated tournament before it can be promoted.
    /// Configs are treated as immutable once passed in. Callers must not change them afterwards.
    public sealed class ConfigRegistry
    {
        private readonly DiagnosticsLog _log;
        private ChallengeConfig _previous;

        public ConfigRegistry(ChallengeConfig active, DiagnosticsLog log)
        {
            if (active == null)
            {
                throw new ArgumentNullException(nameof(active));
            }

            List<string> errors = active.Validate();
            if (errors.Count > 0)
            {
                throw new ArgumentException("Active config is invalid: " + string.Join(" ", errors), nameof(active));
            }

            Active = active;
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public ChallengeConfig Active { get; private set; }
        public ChallengeConfig Candidate { get; private set; }

        /// Validates the candidate, then plays one simulated tournament per seed. Every simulated run must verify.
        /// Returns the problems found. An empty list means the candidate is staged and ready to promote.
        public IReadOnlyList<string> StageCandidate(ChallengeConfig candidate, IReadOnlyList<ulong> seeds, IReadOnlyList<BotProfile> bots)
        {
            var problems = new List<string>();
            if (candidate == null)
            {
                problems.Add("Candidate is null.");
                return Reject(problems);
            }

            problems.AddRange(candidate.Validate());
            if (candidate.Version == Active.Version)
            {
                problems.Add($"Candidate version '{candidate.Version}' must differ from the active version.");
            }

            if (seeds == null || seeds.Count == 0 || bots == null || bots.Count == 0)
            {
                problems.Add("Staging needs at least one seed and one bot.");
            }

            if (problems.Count > 0)
            {
                return Reject(problems);
            }

            int rejectedRuns = 0;
            for (int i = 0; i < seeds.Count; i++)
            {
                TournamentSimulator.Run(candidate, "stage-" + i, seeds[i], bots, long.MaxValue, 0, out IReadOnlyList<BotOutcome> outcomes);
                foreach (BotOutcome outcome in outcomes)
                {
                    if (outcome.Status != SubmissionStatus.Accepted)
                    {
                        rejectedRuns++;
                    }
                }
            }

            if (rejectedRuns > 0)
            {
                problems.Add($"Simulation produced {rejectedRuns} runs that did not verify.");
                return Reject(problems);
            }

            Candidate = candidate;
            _log.Record(DiagnosticLevel.Info, "config.staged", $"Staged '{candidate.Version}' after {seeds.Count} simulated rounds.");
            return problems;
        }

        public bool Promote()
        {
            if (Candidate == null)
            {
                return false;
            }

            _previous = Active;
            Active = Candidate;
            Candidate = null;
            _log.Record(DiagnosticLevel.Info, "config.promoted", $"Active is now '{Active.Version}'.");
            return true;
        }

        public bool Rollback()
        {
            if (_previous == null)
            {
                return false;
            }

            Candidate = null;
            Active = _previous;
            _previous = null;
            _log.Record(DiagnosticLevel.Warning, "config.rolled_back", $"Active is now '{Active.Version}'.");
            return true;
        }

        private IReadOnlyList<string> Reject(List<string> problems)
        {
            _log.Record(DiagnosticLevel.Warning, "config.rejected", string.Join(" ", problems));
            return problems;
        }
    }
}
