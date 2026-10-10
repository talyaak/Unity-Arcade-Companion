using System;
using NUnit.Framework;
using PulseArena.Core;

namespace PulseArena.Tests
{
    public class ChallengeSessionTests
    {
        // Fixed spacing makes the timeline predictable: a target every 1.0s, lifetime 0.8s, on 3 lanes.
        private static ChallengeConfig FixedConfig() => new ChallengeConfig
        {
            Version = "test-fixed",
            CountdownSeconds = 2,
            DurationSeconds = 10,
            LaneCount = 3,
            MinSpawnInterval = 1.0,
            MaxSpawnInterval = 1.0,
            TargetLifetime = 0.8,
            Scoring = new ScoringRules { BasePoints = 100, ComboStep = 5, MaxMultiplier = 4, MissPenalty = 50 }
        };

        // Advances to just after `spawnTime` so the target is live (spawned, not yet expired).
        private static void AdvanceTo(ChallengeSession session, double time)
        {
            session.Advance(time - session.PlayElapsed);
        }

        [Test]
        public void Constructor_RejectsInvalidConfig()
        {
            var config = FixedConfig();
            config.LaneCount = 0;

            Assert.Throws<ArgumentException>(() => new ChallengeSession(config));
        }

        [Test]
        public void Begin_EntersCountdown_ThenPlaying()
        {
            var session = new ChallengeSession(FixedConfig());

            Assert.IsTrue(session.Begin(1));
            Assert.AreEqual(ChallengeState.Countdown, session.State);

            session.Advance(1.5);
            Assert.AreEqual(ChallengeState.Countdown, session.State);
            Assert.AreEqual(0.5, session.CountdownRemaining, 1e-9);

            session.Advance(0.5);
            Assert.AreEqual(ChallengeState.Playing, session.State);
        }

        [Test]
        public void CountdownOverflow_CarriesIntoPlayTime()
        {
            var session = new ChallengeSession(FixedConfig());
            session.Begin(1);

            session.Advance(2.25); // 2.0 countdown + 0.25 into play

            Assert.AreEqual(0.25, session.PlayElapsed, 1e-9);
        }

        [Test]
        public void TapsDuringCountdown_AreIgnored()
        {
            var session = new ChallengeSession(FixedConfig());
            session.Begin(1);

            Assert.AreEqual(TapKind.Ignored, session.TapLane(0).Kind);
            Assert.AreEqual(0, session.Scoreboard.Hits);
        }

        [Test]
        public void Begin_RejectedWhileRunning()
        {
            var session = new ChallengeSession(FixedConfig());
            session.Begin(1);

            Assert.IsFalse(session.Begin(2));
        }

        [Test]
        public void EmptyTap_HasNoPenalty()
        {
            var session = new ChallengeSession(FixedConfig());
            session.Begin(1);
            session.Advance(2);

            TapResult result = session.TapLane(0); // nothing spawned yet

            Assert.AreEqual(TapKind.Empty, result.Kind);
            Assert.AreEqual(0, session.Scoreboard.Score);
            Assert.AreEqual(0, session.Scoreboard.Misses);
        }

        [Test]
        public void PerfectPlayer_HitsEveryTarget()
        {
            var session = new ChallengeSession(FixedConfig());
            session.Begin(42);
            session.Advance(2); // countdown

            // Taps are placed a hair after each spawn so floating-point drift can't land them before it.
            foreach (TargetSpec target in session.Schedule)
            {
                AdvanceTo(session, target.SpawnTime + 0.001);
                Assert.AreEqual(TapKind.Hit, session.TapLane(target.Lane).Kind);
            }
            session.Advance(10);

            ChallengeResult result = session.Result;
            Assert.AreEqual(ChallengeState.Results, session.State);
            Assert.IsNotNull(result);
            Assert.AreEqual(session.Schedule.Count, result.Hits);
            Assert.AreEqual(0, result.Misses);
            Assert.AreEqual(session.Schedule.Count, result.BestCombo);
            Assert.AreEqual(1.0, result.Accuracy, 1e-9);
            Assert.AreEqual("test-fixed", result.ConfigVersion);
            Assert.AreEqual(42UL, result.Seed);
        }

        [Test]
        public void IgnoredTarget_ExpiresAsMiss_AndResetsCombo()
        {
            var session = new ChallengeSession(FixedConfig());
            session.Begin(42);
            session.Advance(2);

            // Hit the first two targets, then ignore the third.
            AdvanceTo(session, session.Schedule[0].SpawnTime + 0.001);
            session.TapLane(session.Schedule[0].Lane);
            AdvanceTo(session, session.Schedule[1].SpawnTime + 0.001);
            session.TapLane(session.Schedule[1].Lane);
            Assert.AreEqual(2, session.Scoreboard.Combo);

            AdvanceTo(session, session.Schedule[2].ExpiresAt + 0.001);

            Assert.AreEqual(1, session.Scoreboard.Misses);
            Assert.AreEqual(0, session.Scoreboard.Combo);
            Assert.AreEqual(2, session.Scoreboard.BestCombo);
        }

        [Test]
        public void RunEnd_ResolvesEveryScheduledTarget()
        {
            var session = new ChallengeSession(FixedConfig());
            session.Begin(7);
            session.Advance(2);

            // Tap only on every other target, so the run mixes hits and misses.
            for (int i = 0; i < session.Schedule.Count; i++)
            {
                TargetSpec target = session.Schedule[i];
                AdvanceTo(session, target.SpawnTime + 0.001);
                if (i % 2 == 0)
                {
                    session.TapLane(target.Lane);
                }
            }
            session.Advance(10);

            Assert.AreEqual(session.Schedule.Count, session.Result.Hits + session.Result.Misses);
        }

        [Test]
        public void SameSeed_AndSameInputs_ProduceSameResult()
        {
            ChallengeResult first = PlayScripted(seed: 99);
            ChallengeResult second = PlayScripted(seed: 99);

            Assert.AreEqual(first.Score, second.Score);
            Assert.AreEqual(first.Hits, second.Hits);
            Assert.AreEqual(first.Misses, second.Misses);
        }

        [Test]
        public void Quit_ReturnsToIdle_AndReplayStartsFreshRun()
        {
            var session = new ChallengeSession(FixedConfig());
            session.Begin(1);
            Assert.IsTrue(session.Quit());
            Assert.AreEqual(ChallengeState.Idle, session.State);

            Assert.IsTrue(session.Begin(2));
            Assert.AreEqual(ChallengeState.Countdown, session.State);
        }

        [Test]
        public void Quit_ClearsLiveTargets()
        {
            var session = new ChallengeSession(FixedConfig());
            session.Begin(42);
            session.Advance(2);
            TargetSpec first = session.Schedule[0];
            AdvanceTo(session, first.SpawnTime + 0.001);
            Assert.IsTrue(session.HasLiveTarget(first.Lane), "precondition: a target is live");

            session.Quit();

            Assert.AreEqual(ChallengeState.Idle, session.State);
            for (int lane = 0; lane < 3; lane++)
            {
                Assert.IsFalse(session.HasLiveTarget(lane), $"lane {lane} still shows a target after quit");
            }
        }

        [Test]
        public void Advance_RejectsNegativeOrNaN()
        {
            var session = new ChallengeSession(FixedConfig());
            session.Begin(1);

            Assert.Throws<ArgumentOutOfRangeException>(() => session.Advance(-0.1));
            Assert.Throws<ArgumentOutOfRangeException>(() => session.Advance(double.NaN));
        }

        [Test]
        public void TapLane_OutOfRange_IsIgnored()
        {
            var session = new ChallengeSession(FixedConfig());
            session.Begin(1);
            session.Advance(2.5);

            Assert.AreEqual(TapKind.Ignored, session.TapLane(-1).Kind);
            Assert.AreEqual(TapKind.Ignored, session.TapLane(3).Kind);
        }

        [Test]
        public void ChunkedAdvance_GivesSameResultAsOneStep()
        {
            // Replay verification depends on this: the frame-time split must not change the outcome.
            ChallengeResult whole = PlayWithSteps(seed: 123, stepSeconds: 0);
            ChallengeResult chunked = PlayWithSteps(seed: 123, stepSeconds: 0.0137);

            Assert.AreEqual(whole.Score, chunked.Score);
            Assert.AreEqual(whole.Hits, chunked.Hits);
            Assert.AreEqual(whole.Misses, chunked.Misses);
            Assert.AreEqual(whole.BestCombo, chunked.BestCombo);
        }

        // Plays the scripted run, advancing in steps of stepSeconds between taps (0 means jump straight to each spawn).
        private static ChallengeResult PlayWithSteps(ulong seed, double stepSeconds)
        {
            var session = new ChallengeSession(FixedConfig());
            session.Begin(seed);
            session.Advance(2);

            for (int i = 0; i < session.Schedule.Count; i++)
            {
                TargetSpec target = session.Schedule[i];
                double goal = target.SpawnTime + 0.001;
                if (stepSeconds > 0)
                {
                    while (session.PlayElapsed + stepSeconds < goal)
                    {
                        session.Advance(stepSeconds);
                    }
                }
                AdvanceTo(session, goal);
                if (i % 2 == 0)
                {
                    session.TapLane(target.Lane);
                }
            }

            session.Advance(10);
            return session.Result;
        }

        // Plays a fixed script (hit even targets, ignore odd ones) and returns the result.
        private static ChallengeResult PlayScripted(ulong seed)
        {
            var session = new ChallengeSession(FixedConfig());
            session.Begin(seed);
            session.Advance(2);

            for (int i = 0; i < session.Schedule.Count; i++)
            {
                TargetSpec target = session.Schedule[i];
                AdvanceTo(session, target.SpawnTime + 0.001);
                if (i % 2 == 0)
                {
                    session.TapLane(target.Lane);
                }
            }

            session.Advance(10);
            return session.Result;
        }
    }
}
