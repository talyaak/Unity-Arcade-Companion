using System;
using System.Collections.Generic;

namespace PulseArena.Core
{
    /// One accepted tap (Hit or Empty) with the play time it happened at.
    public readonly struct TapRecord
    {
        public TapRecord(double playTime, int lane)
        {
            PlayTime = playTime;
            Lane = lane;
        }

        public double PlayTime { get; }
        public int Lane { get; }
    }

    /// Recomputes a run from its seed, config and tap log, so a claimed result can be checked.
    /// This checks consistency only. It does not stop a modified client from logging taps that never happened;
    /// that needs server-side validation (see docs).
    public static class ReplayVerifier
    {
        public static ChallengeResult Verify(ChallengeConfig config, ulong seed, IReadOnlyList<TapRecord> taps)
        {
            if (taps == null)
            {
                throw new ArgumentNullException(nameof(taps));
            }

            var session = new ChallengeSession(config);
            if (!session.Begin(seed))
            {
                throw new InvalidOperationException("Could not start the session for replay.");
            }

            session.Advance(config.CountdownSeconds);

            double previous = 0;
            foreach (TapRecord tap in taps)
            {
                if (!(tap.PlayTime >= previous) || tap.PlayTime > config.DurationSeconds)
                {
                    throw new ArgumentException(
                        $"Tap at {tap.PlayTime} is out of order or outside the run (duration {config.DurationSeconds}).",
                        nameof(taps));
                }

                // Rounding can leave PlayElapsed a hair past the tap time. Never move time backwards.
                double step = tap.PlayTime - session.PlayElapsed;
                if (step > 0)
                {
                    session.Advance(step);
                }
                session.TapLane(tap.Lane);
                previous = tap.PlayTime;
            }

            session.Advance(config.DurationSeconds - session.PlayElapsed);
            if (session.Result == null)
            {
                throw new InvalidOperationException("Replay did not reach the end of the run.");
            }

            return session.Result;
        }
    }
}
