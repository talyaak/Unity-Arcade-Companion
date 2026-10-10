using System;
using System.Collections.Generic;

namespace PulseArena.Core
{
    public readonly struct MatchTap
    {
        public MatchTap(double time, int tileId)
        {
            Time = time;
            TileId = tileId;
        }

        public double Time { get; }
        public int TileId { get; }
    }

    /// Re-runs a level from its spec, seed, rules and tap log. This checks consistency only. It cannot tell a
    /// human from a bot, or a logged tap that never happened from one that did.
    public static class MatchReplay
    {
        public static MatchBoard Verify(MatchLevelSpec spec, ulong seed, MatchRules rules, IReadOnlyList<MatchTap> taps)
        {
            if (taps == null)
            {
                throw new ArgumentNullException(nameof(taps));
            }

            MatchLevel level = MatchLevelGenerator.Generate(spec, seed);
            var board = new MatchBoard(level.Tiles, rules);
            foreach (MatchTap tap in taps)
            {
                MatchStep step = board.TapTile(tap.TileId, tap.Time);
                if (step.Outcome == MatchOutcome.Ignored || step.Outcome == MatchOutcome.Blocked)
                {
                    throw new ArgumentException(
                        $"Tap on tile {tap.TileId} at {tap.Time} was {step.Outcome}; the log is not a legal run.", nameof(taps));
                }
            }

            return board;
        }
    }
}
