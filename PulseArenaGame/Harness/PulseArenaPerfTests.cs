using System;
using NUnit.Framework;
using PulseArena.Core;

namespace PulseArena.Tests
{
    // dotnet-only. Checks that the engine-free play loop does not allocate per frame.
    // This is a proxy for the Unity hot path, not a measurement of Unity's frame time or GC on a device.
    public class PulseArenaPerfTests
    {
        private const double FrameSeconds = 1.0 / 60.0;

        [Test]
        public void PlayLoop_AllocatesNothing_PerFrame()
        {
            var session = new ChallengeSession(ChallengeConfig.Defaults());
            session.Begin(42);
            session.Advance(session.CountdownRemaining + 0.0001);

            int hits = 0;
            int next = 0;

            // Stop one frame before time-up, so the result object (allocated on finish) is outside the measured window.
            long before = GC.GetAllocatedBytesForCurrentThread();
            while (session.PlayRemaining > 2 * FrameSeconds)
            {
                session.Advance(FrameSeconds);

                // Tap each target shortly after it spawns, using the schedule by index (no enumerator).
                while (next < session.Schedule.Count && session.Schedule[next].SpawnTime + 0.05 <= session.PlayElapsed)
                {
                    TargetSpec target = session.Schedule[next];
                    if (session.HasLiveTarget(target.Lane) && session.TapLane(target.Lane).Kind == TapKind.Hit)
                    {
                        hits++;
                    }

                    next++;
                }
            }
            long after = GC.GetAllocatedBytesForCurrentThread();

            Assert.Greater(hits, 0, "the loop must actually play the run");
            Assert.AreEqual(0, after - before, "per-frame play loop allocated memory");
        }
    }
}
