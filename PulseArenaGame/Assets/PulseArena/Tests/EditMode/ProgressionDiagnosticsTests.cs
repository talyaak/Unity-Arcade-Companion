using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PulseArena.Core;

namespace PulseArena.Tests
{
    public class ProgressionDiagnosticsTests
    {
        private static ChallengeResult Run(int hits, int bestCombo) => new ChallengeResult(0, hits, 0, bestCombo, 1, "v");

        [Test]
        public void XpForRun_CountsHitsAndBestCombo()
        {
            Assert.AreEqual(10 * 6 + 5 * 4, new ProgressionRules().XpForRun(Run(6, 4)));
        }

        [Test]
        public void LevelFor_ChangesAtThresholds()
        {
            var rules = new ProgressionRules();

            Assert.AreEqual(1, rules.LevelFor(0));
            Assert.AreEqual(1, rules.LevelFor(99));
            Assert.AreEqual(2, rules.LevelFor(100));
            Assert.AreEqual(3, rules.LevelFor(250));
            Assert.AreEqual(5, rules.LevelFor(700));
        }

        [Test]
        public void ApplyRun_UnlocksCosmeticsOnce()
        {
            var rules = new ProgressionRules();
            var profile = new PlayerProfile("p1");

            IReadOnlyList<Cosmetic> first = profile.ApplyRun(Run(20, 10), rules); // 200 + 50 = 250 XP -> level 3
            IReadOnlyList<Cosmetic> second = profile.ApplyRun(Run(1, 0), rules);

            CollectionAssert.AreEquivalent(new[] { "lane-teal", "ring-amber" }, first.Select(c => c.Id));
            Assert.AreEqual(0, second.Count, "already-unlocked cosmetics are not reported again");
            Assert.AreEqual(3, profile.Level);
        }

        [Test]
        public void Log_BoundsEventsButKeepsLifetimeCounts()
        {
            var log = new DiagnosticsLog(new FixedTimeSource(1000), capacity: 2);
            log.Record(DiagnosticLevel.Info, "a", "one");
            log.Record(DiagnosticLevel.Info, "a", "two");
            log.Record(DiagnosticLevel.Info, "a", "three");

            Assert.AreEqual(2, log.Events.Count);
            Assert.AreEqual("two", log.Events.First().Message);
            Assert.AreEqual(3, log.Count("a"));
        }

        [Test]
        public void Log_TruncatesLongMessages_AndExportsTimestamp()
        {
            var clock = new FixedTimeSource(42);
            var log = new DiagnosticsLog(clock, capacity: 5);
            log.Record(DiagnosticLevel.Error, "x", new string('z', 500));

            Assert.AreEqual(DiagnosticsLog.MaxMessageLength, log.Events.First().Message.Length);
            StringAssert.StartsWith("42 ERROR x ", log.ExportText());
        }

        [Test]
        public void Log_RejectsEmptyCode()
        {
            var log = new DiagnosticsLog(new FixedTimeSource(0), capacity: 5);

            Assert.Throws<System.ArgumentException>(() => log.Record(DiagnosticLevel.Info, "", "m"));
        }

        private static ChallengeConfig Config(string version) => new ChallengeConfig
        {
            Version = version,
            CountdownSeconds = 1,
            DurationSeconds = 12,
            LaneCount = 3,
            MinSpawnInterval = 0.5,
            MaxSpawnInterval = 1.0,
            TargetLifetime = 0.9
        };

        private static readonly List<BotProfile> Bots = new List<BotProfile> { new BotProfile("bot", 0.8, 0.5) };

        [Test]
        public void Registry_RejectsInvalidCandidate_AndKeepsActive()
        {
            var log = new DiagnosticsLog(new FixedTimeSource(0), 50);
            var registry = new ConfigRegistry(Config("v1"), log);
            var bad = Config("v2");
            bad.LaneCount = 0;

            IReadOnlyList<string> problems = registry.StageCandidate(bad, new ulong[] { 1 }, Bots);

            Assert.IsNotEmpty(problems);
            Assert.AreEqual("v1", registry.Active.Version);
            Assert.IsNull(registry.Candidate);
            Assert.AreEqual(1, log.Count("config.rejected"));
        }

        [Test]
        public void Registry_RejectsSameVersion()
        {
            var registry = new ConfigRegistry(Config("v1"), new DiagnosticsLog(new FixedTimeSource(0), 50));

            Assert.IsNotEmpty(registry.StageCandidate(Config("v1"), new ulong[] { 1 }, Bots));
        }

        [Test]
        public void Registry_StagePromote_ThenRollback_RestoresPrevious()
        {
            var log = new DiagnosticsLog(new FixedTimeSource(0), 50);
            var registry = new ConfigRegistry(Config("v1"), log);

            Assert.IsEmpty(registry.StageCandidate(Config("v2"), new ulong[] { 1, 2 }, Bots));
            Assert.IsTrue(registry.Promote());
            Assert.AreEqual("v2", registry.Active.Version);

            Assert.IsTrue(registry.Rollback());
            Assert.AreEqual("v1", registry.Active.Version);
            Assert.IsFalse(registry.Rollback(), "only one step of history is kept");
            Assert.AreEqual(1, log.Count("config.rolled_back"));
        }

        [Test]
        public void Registry_PromoteWithoutCandidate_IsRefused()
        {
            var registry = new ConfigRegistry(Config("v1"), new DiagnosticsLog(new FixedTimeSource(0), 50));

            Assert.IsFalse(registry.Promote());
        }

        [Test]
        public void Flags_FailClosedForUnknown_AndIgnoreUnknownOverrides()
        {
            var log = new DiagnosticsLog(new FixedTimeSource(0), 50);
            var flags = new FeatureFlags(new Dictionary<string, bool> { { "tournaments", false }, { "ghosts", true } }, log);

            IReadOnlyList<string> ignored = flags.ApplyOverrides(new Dictionary<string, bool>
            {
                { "tournaments", true },
                { "made_up", true }
            });

            Assert.IsTrue(flags.IsEnabled("tournaments"));
            Assert.IsTrue(flags.IsEnabled("ghosts"));
            Assert.IsFalse(flags.IsEnabled("never_defined"));
            CollectionAssert.AreEqual(new[] { "made_up" }, ignored.ToArray());
        }
    }
}
