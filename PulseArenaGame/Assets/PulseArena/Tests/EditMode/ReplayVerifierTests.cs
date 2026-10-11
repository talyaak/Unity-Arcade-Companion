using System;
using System.Collections.Generic;
using NUnit.Framework;
using PulseArena.Core;

namespace PulseArena.Tests
{
    public class ReplayVerifierTests
    {
        private static ChallengeConfig Config() => new ChallengeConfig
        {
            Version = "replay-test",
            CountdownSeconds = 2,
            DurationSeconds = 20,
            LaneCount = 3,
            MinSpawnInterval = 0.4,
            MaxSpawnInterval = 1.2,
            TargetLifetime = 0.9
        };

        // Plays a random run live, logging accepted taps the way a controller would, then returns both results.
        private static (ChallengeResult live, List<TapRecord> taps) PlayLiveRandomly(ulong seed, ulong inputSeed)
        {
            var session = new ChallengeSession(Config());
            session.Begin(seed);
            var input = new SeededRandom(inputSeed);
            var taps = new List<TapRecord>();

            // Tap at random moments in small random steps, so tap times are not all on target spawns.
            while (session.State != ChallengeState.Results)
            {
                session.Advance(0.05 + input.NextDouble() * 0.3);
                if (session.State == ChallengeState.Playing && input.NextDouble() < 0.4)
                {
                    int lane = input.NextInt(3);
                    TapResult result = session.TapLane(lane);
                    if (result.Kind != TapKind.Ignored)
                    {
                        taps.Add(new TapRecord(session.PlayElapsed, lane));
                    }
                }
            }

            return (session.Result, taps);
        }

        [Test]
        public void Verify_MatchesLiveRun_ForRandomInputs()
        {
            for (ulong inputSeed = 1; inputSeed <= 25; inputSeed++)
            {
                (ChallengeResult live, List<TapRecord> taps) = PlayLiveRandomly(seed: 500 + inputSeed, inputSeed: inputSeed);

                ChallengeResult replayed = ReplayVerifier.Verify(Config(), 500 + inputSeed, taps);

                Assert.AreEqual(live.Score, replayed.Score, $"score, input seed {inputSeed}");
                Assert.AreEqual(live.Hits, replayed.Hits, $"hits, input seed {inputSeed}");
                Assert.AreEqual(live.Misses, replayed.Misses, $"misses, input seed {inputSeed}");
                Assert.AreEqual(live.BestCombo, replayed.BestCombo, $"best combo, input seed {inputSeed}");
            }
        }

        [Test]
        public void Verify_DetectsChangedScore_WhenALogEntryIsRemoved()
        {
            (ChallengeResult live, List<TapRecord> taps) = PlayLiveRandomly(seed: 77, inputSeed: 4);
            Assert.That(taps.Count, Is.GreaterThan(3), "need enough taps for the tamper check to mean something");

            // Empty taps change nothing, so try each entry and require that at least one removal changes the score.
            bool anyChanged = false;
            for (int i = 0; i < taps.Count && !anyChanged; i++)
            {
                var tampered = new List<TapRecord>(taps);
                tampered.RemoveAt(i);
                anyChanged = ReplayVerifier.Verify(Config(), 77, tampered).Score != live.Score;
            }

            Assert.IsTrue(anyChanged);
        }

        [Test]
        public void Verify_RejectsOutOfOrderTaps()
        {
            var taps = new List<TapRecord> { new TapRecord(5, 0), new TapRecord(3, 1) };

            Assert.Throws<ArgumentException>(() => ReplayVerifier.Verify(Config(), 1, taps));
        }

        [Test]
        public void Verify_RejectsTapsOutsideRun()
        {
            var taps = new List<TapRecord> { new TapRecord(21, 0) };

            Assert.Throws<ArgumentException>(() => ReplayVerifier.Verify(Config(), 1, taps));
        }
    }
}
