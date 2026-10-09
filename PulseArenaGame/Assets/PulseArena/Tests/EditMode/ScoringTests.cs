using NUnit.Framework;
using PulseArena.Core;

namespace PulseArena.Tests
{
    public class ScoringTests
    {
        private static ScoringRules Rules() => new ScoringRules
        {
            BasePoints = 100,
            ComboStep = 5,
            MaxMultiplier = 3,
            MissPenalty = 50
        };

        [TestCase(0, 1)]
        [TestCase(4, 1)]
        [TestCase(5, 2)]
        [TestCase(9, 2)]
        [TestCase(10, 3)]
        [TestCase(500, 3)] // capped at MaxMultiplier
        public void MultiplierFor_StepsAtComboBoundaries(int combo, int expected)
        {
            Assert.AreEqual(expected, Rules().MultiplierFor(combo));
        }

        [Test]
        public void Validate_ReportsEveryProblem()
        {
            var rules = new ScoringRules { BasePoints = -1, ComboStep = 0, MaxMultiplier = 0, MissPenalty = -5 };

            Assert.AreEqual(4, rules.Validate().Count);
        }

        [Test]
        public void Hit_UsesMultiplierBeforeComboIncreases()
        {
            var tracker = new ScoreTracker(Rules());

            // Hits 1-5 use multiplier 1. The 6th hit is the first to use multiplier 2.
            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(100, tracker.RegisterHit().Points);
            }

            HitOutcome sixth = tracker.RegisterHit();
            Assert.AreEqual(2, sixth.Multiplier);
            Assert.AreEqual(200, sixth.Points);
            Assert.AreEqual(700, tracker.Score);
            Assert.AreEqual(6, tracker.Combo);
        }

        [Test]
        public void Miss_ResetsComboButKeepsBestCombo()
        {
            var tracker = new ScoreTracker(Rules());
            for (int i = 0; i < 3; i++) tracker.RegisterHit();

            tracker.RegisterMiss();

            Assert.AreEqual(0, tracker.Combo);
            Assert.AreEqual(3, tracker.BestCombo);
            Assert.AreEqual(1, tracker.Misses);
        }

        [Test]
        public void Miss_PenalizesScoreButNeverBelowZero()
        {
            var tracker = new ScoreTracker(Rules());
            tracker.RegisterHit(); // score 100

            tracker.RegisterMiss(); // 100 - 50
            Assert.AreEqual(50, tracker.Score);

            tracker.RegisterMiss();
            tracker.RegisterMiss(); // would be negative
            Assert.AreEqual(0, tracker.Score);
        }
    }
}
