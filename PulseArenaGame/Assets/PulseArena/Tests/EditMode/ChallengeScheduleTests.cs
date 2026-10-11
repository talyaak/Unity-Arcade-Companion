using System.Collections.Generic;
using NUnit.Framework;
using PulseArena.Core;

namespace PulseArena.Tests
{
    public class ChallengeScheduleTests
    {
        private static ChallengeConfig Config() => new ChallengeConfig
        {
            Version = "test",
            CountdownSeconds = 0,
            DurationSeconds = 30,
            LaneCount = 3,
            MinSpawnInterval = 0.5,
            MaxSpawnInterval = 1.5,
            TargetLifetime = 1.0
        };

        [Test]
        public void SameSeed_ProducesIdenticalSchedule()
        {
            List<TargetSpec> a = ChallengeSchedule.Generate(Config(), new SeededRandom(5));
            List<TargetSpec> b = ChallengeSchedule.Generate(Config(), new SeededRandom(5));

            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].Lane, b[i].Lane);
                Assert.AreEqual(a[i].SpawnTime, b[i].SpawnTime);
            }
        }

        [Test]
        public void Targets_AreOrderedAndResolveInsideRun()
        {
            var config = Config();
            List<TargetSpec> targets = ChallengeSchedule.Generate(config, new SeededRandom(11));

            Assert.That(targets.Count, Is.GreaterThan(0));
            for (int i = 0; i < targets.Count; i++)
            {
                Assert.That(targets[i].ExpiresAt, Is.LessThanOrEqualTo(config.DurationSeconds));
                Assert.That(targets[i].Lane, Is.InRange(0, config.LaneCount - 1));
                if (i > 0)
                {
                    Assert.That(targets[i].SpawnTime, Is.GreaterThan(targets[i - 1].SpawnTime));
                }
            }
        }

        [Test]
        public void SameLane_NeverOverlaps()
        {
            var config = Config();
            List<TargetSpec> targets = ChallengeSchedule.Generate(config, new SeededRandom(3));

            var lastExpiry = new double[config.LaneCount];
            foreach (TargetSpec target in targets)
            {
                Assert.That(target.SpawnTime, Is.GreaterThanOrEqualTo(lastExpiry[target.Lane]));
                lastExpiry[target.Lane] = target.ExpiresAt;
            }
        }
    }
}
