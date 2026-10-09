using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PulseArena.Core;

namespace PulseArena.Tests
{
    public class TournamentTests
    {
        private static ChallengeConfig Config() => new ChallengeConfig
        {
            Version = "tourney-test",
            CountdownSeconds = 1,
            DurationSeconds = 15,
            LaneCount = 3,
            MinSpawnInterval = 0.5,
            MaxSpawnInterval = 1.0,
            TargetLifetime = 0.9
        };

        private static readonly List<TapRecord> NoTaps = new List<TapRecord>();

        // Builds a valid submission by playing the given seed live with a perfect player.
        private static TournamentSubmission PerfectSubmission(string player, ulong seed, long at)
        {
            var session = new ChallengeSession(Config());
            session.Begin(seed);
            session.Advance(1);
            var taps = new List<TapRecord>();
            foreach (TargetSpec t in session.Schedule)
            {
                double time = t.SpawnTime + 0.001;
                session.Advance(time - session.PlayElapsed);
                if (session.TapLane(t.Lane).Kind == TapKind.Hit)
                {
                    taps.Add(new TapRecord(time, t.Lane));
                }
            }
            session.Advance(15 - session.PlayElapsed);
            return new TournamentSubmission(player, seed, Config().Version, session.Result.Score, at, taps);
        }

        [Test]
        public void ValidSubmission_IsAccepted_AndRanked()
        {
            var round = new TournamentRound("r1", Config(), 9, closesAtMillis: 1000);

            Assert.AreEqual(SubmissionStatus.Accepted, round.Submit(PerfectSubmission("ana", 9, 100)));
            Assert.AreEqual("ana", round.Standings().Single().PlayerId);
        }

        [Test]
        public void WrongConfigVersion_IsRejected()
        {
            var round = new TournamentRound("r1", Config(), 9, 1000);
            TournamentSubmission good = PerfectSubmission("ana", 9, 100);
            var stale = new TournamentSubmission("ana", 9, "old-version", good.ClaimedScore, 100, good.Taps);

            Assert.AreEqual(SubmissionStatus.WrongConfigVersion, round.Submit(stale));
        }

        [Test]
        public void WrongSeed_IsRejected()
        {
            var round = new TournamentRound("r1", Config(), 9, 1000);

            Assert.AreEqual(SubmissionStatus.WrongSeed, round.Submit(PerfectSubmission("ana", 10, 100)));
        }

        [Test]
        public void LateSubmission_IsRejected()
        {
            var round = new TournamentRound("r1", Config(), 9, closesAtMillis: 1000);

            Assert.AreEqual(SubmissionStatus.AfterDeadline, round.Submit(PerfectSubmission("ana", 9, 1001)));
        }

        [Test]
        public void TamperedScore_IsRejected()
        {
            var round = new TournamentRound("r1", Config(), 9, 1000);
            TournamentSubmission good = PerfectSubmission("ana", 9, 100);
            var cheat = new TournamentSubmission("ana", 9, Config().Version, good.ClaimedScore + 500, 100, good.Taps);

            Assert.AreEqual(SubmissionStatus.ScoreMismatch, round.Submit(cheat));
        }

        [Test]
        public void InvalidReplay_IsRejected()
        {
            var round = new TournamentRound("r1", Config(), 9, 1000);
            var outOfOrder = new List<TapRecord> { new TapRecord(5, 0), new TapRecord(2, 0) };
            var bad = new TournamentSubmission("ana", 9, Config().Version, 0, 100, outOfOrder);

            Assert.AreEqual(SubmissionStatus.InvalidReplay, round.Submit(bad));
        }

        [Test]
        public void Rejected_DoesNotUseThePlayersSlot_ButAcceptedDoes()
        {
            var round = new TournamentRound("r1", Config(), 9, 1000);
            TournamentSubmission good = PerfectSubmission("ana", 9, 100);
            var cheat = new TournamentSubmission("ana", 9, Config().Version, good.ClaimedScore + 1, 100, good.Taps);

            Assert.AreEqual(SubmissionStatus.ScoreMismatch, round.Submit(cheat));
            Assert.AreEqual(SubmissionStatus.Accepted, round.Submit(good));
            Assert.AreEqual(SubmissionStatus.AlreadySubmitted, round.Submit(PerfectSubmission("ana", 9, 200)));
        }

        // Perfect players share a score and combo, so these tests isolate the time and player-id tie-breaks.
        // The combo tie-break is not exercised here because no test player produces different combos at equal score.
        [Test]
        public void Ranking_TieOnScore_EarlierSubmissionWins()
        {
            var round = new TournamentRound("r", Config(), 9, 10000);
            Assert.AreEqual(SubmissionStatus.Accepted, round.Submit(PerfectSubmission("aki", 9, 300)));
            Assert.AreEqual(SubmissionStatus.Accepted, round.Submit(PerfectSubmission("bea", 9, 200)));

            Assert.AreEqual(new[] { "bea", "aki" }, round.Standings().Select(e => e.PlayerId).ToArray());
        }

        [Test]
        public void Ranking_TieOnScoreAndTime_PlayerIdOrdinalDecides()
        {
            var round = new TournamentRound("r", Config(), 9, 10000);
            Assert.AreEqual(SubmissionStatus.Accepted, round.Submit(PerfectSubmission("cy", 9, 200)));
            Assert.AreEqual(SubmissionStatus.Accepted, round.Submit(PerfectSubmission("aki", 9, 200)));

            Assert.AreEqual(new[] { "aki", "cy" }, round.Standings().Select(e => e.PlayerId).ToArray());
        }

        [Test]
        public void Simulator_SameInputs_GiveSameStandings()
        {
            var bots = new List<BotProfile>
            {
                new BotProfile("alpha", 0.9, 0.4),
                new BotProfile("bravo", 0.6, 0.6),
                new BotProfile("charlie", 0.3, 0.8)
            };

            TournamentRound first = TournamentSimulator.Run(Config(), "r", 2024, bots, 10000, 100, out _);
            TournamentRound second = TournamentSimulator.Run(Config(), "r", 2024, bots, 10000, 100, out _);

            CollectionAssert.AreEqual(
                first.Standings().Select(e => (e.PlayerId, e.Score)).ToList(),
                second.Standings().Select(e => (e.PlayerId, e.Score)).ToList());
        }

        [Test]
        public void Simulator_EveryBotSubmissionVerifies_AndSkillOrdersTheBoard()
        {
            var bots = new List<BotProfile>
            {
                new BotProfile("weak", 0.2, 0.9),
                new BotProfile("strong", 0.95, 0.3)
            };

            TournamentRound round = TournamentSimulator.Run(Config(), "r", 77, bots, 10000, 100, out IReadOnlyList<BotOutcome> outcomes);

            Assert.IsTrue(outcomes.All(o => o.Status == SubmissionStatus.Accepted), "all simulated runs should verify");
            Assert.AreEqual("strong", round.Standings()[0].PlayerId);
        }

        [Test]
        public void Simulator_ZeroHitBot_ScoresZero()
        {
            var bots = new List<BotProfile> { new BotProfile("idle", 0, 0.5) };

            TournamentRound round = TournamentSimulator.Run(Config(), "r", 5, bots, 10000, 100, out _);

            Assert.AreEqual(0, round.Standings()[0].Score);
            Assert.AreEqual(0, round.Standings()[0].Hits);
        }
    }
}
