using System;
using System.Collections.Generic;
using System.Linq;

namespace PulseArena.Core
{
    /// Simulated player with a fixed skill profile. Its play is seeded, so a simulation is reproducible.
    public sealed class BotProfile
    {
        public BotProfile(string name, double hitChance, double maxReactionFraction)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            HitChance = hitChance;
            MaxReactionFraction = maxReactionFraction;
        }

        public string Name { get; }

        /// Probability of hitting a given target at all, in [0, 1].
        public double HitChance { get; }

        /// Reaction time as a fraction of target lifetime, in (0, 1). Lower means faster.
        public double MaxReactionFraction { get; }
    }

    public sealed class BotOutcome
    {
        public BotOutcome(string playerId, SubmissionStatus status, int claimedScore)
        {
            PlayerId = playerId;
            Status = status;
            ClaimedScore = claimedScore;
        }

        public string PlayerId { get; }
        public SubmissionStatus Status { get; }
        public int ClaimedScore { get; }
    }

    /// Runs a local round: each bot plays the shared seed, then submits through the same path a real client would use.
    /// Everything is driven by seeds, so the same inputs give the same standings.
    public static class TournamentSimulator
    {
        public static TournamentRound Run(
            ChallengeConfig config,
            string roundId,
            ulong seed,
            IReadOnlyList<BotProfile> bots,
            long closesAtMillis,
            long submittedAtMillis,
            out IReadOnlyList<BotOutcome> outcomes)
        {
            if (bots == null)
            {
                throw new ArgumentNullException(nameof(bots));
            }

            var round = new TournamentRound(roundId, config, seed, closesAtMillis);
            var results = new List<BotOutcome>();

            for (int i = 0; i < bots.Count; i++)
            {
                BotProfile bot = bots[i];
                // Each bot gets its own stream, so adding a bot does not change the others' play.
                var random = new SeededRandom(seed ^ ((ulong)(i + 1) * 0x9E3779B97F4A7C15UL));
                List<TapRecord> taps = PlanTaps(config, seed, bot, random);

                ChallengeResult live = PlayTaps(config, seed, taps);
                var submission = new TournamentSubmission(
                    bot.Name, seed, config.Version, live.Score, submittedAtMillis, taps);
                results.Add(new BotOutcome(bot.Name, round.Submit(submission), live.Score));
            }

            outcomes = results;
            return round;
        }

        private static List<TapRecord> PlanTaps(ChallengeConfig config, ulong seed, BotProfile bot, SeededRandom random)
        {
            var session = new ChallengeSession(config);
            session.Begin(seed);

            var taps = new List<TapRecord>();
            foreach (TargetSpec target in session.Schedule)
            {
                if (random.NextDouble() >= bot.HitChance)
                {
                    continue;
                }

                double reaction = random.NextDouble() * bot.MaxReactionFraction * (target.ExpiresAt - target.SpawnTime);
                taps.Add(new TapRecord(target.SpawnTime + reaction, target.Lane));
            }

            taps.Sort((a, b) => a.PlayTime.CompareTo(b.PlayTime));
            return taps;
        }

        // Plays the planned taps through a live session. The tap log is what gets submitted.
        private static ChallengeResult PlayTaps(ChallengeConfig config, ulong seed, List<TapRecord> plannedTaps)
        {
            var session = new ChallengeSession(config);
            session.Begin(seed);
            session.Advance(config.CountdownSeconds);

            foreach (TapRecord tap in plannedTaps)
            {
                double step = tap.PlayTime - session.PlayElapsed;
                if (step > 0)
                {
                    session.Advance(step);
                }
                session.TapLane(tap.Lane);
            }

            session.Advance(config.DurationSeconds - session.PlayElapsed);
            return session.Result;
        }
    }
}
