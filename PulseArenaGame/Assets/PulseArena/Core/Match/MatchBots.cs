using System;
using System.Collections.Generic;

namespace PulseArena.Core
{
    /// Picks the next tile to tap from the same information a player has. Returns -1 when nothing is free.
    public delegate int MatchPolicy(MatchBoard board, SeededRandom random);

    /// Simple players for difficulty checks. Uniform is the baseline. Greedy prefers the symbol already in the tray,
    /// which is how a careful player finishes a triple.
    public static class MatchPolicies
    {
        public static readonly MatchPolicy Uniform = PickUniform;
        public static readonly MatchPolicy Greedy = PickGreedy;

        private static int PickUniform(MatchBoard board, SeededRandom random)
        {
            IReadOnlyList<int> free = board.FreeTileIds();
            return free.Count == 0 ? -1 : free[random.NextInt(free.Count)];
        }

        private static int PickGreedy(MatchBoard board, SeededRandom random)
        {
            IReadOnlyList<int> free = board.FreeTileIds();
            if (free.Count == 0)
            {
                return -1;
            }

            int best = -1;
            var candidates = new List<int>();
            foreach (int id in free)
            {
                board.TryGetOnBoard(id, out MatchTile tile);
                int inTray = board.TrayCount(tile.Symbol);
                if (inTray > best)
                {
                    best = inTray;
                    candidates.Clear();
                }

                if (inTray == best)
                {
                    candidates.Add(id);
                }
            }

            return candidates[random.NextInt(candidates.Count)];
        }
    }

    public static class MatchSimulator
    {
        /// Fraction of seeds the policy wins. Seeds run from firstSeed to firstSeed + seedCount - 1.
        /// Taps are spaced 0.5 s apart, a fixed pace that sets the combo window's effect. It is a simplification.
        public static double WinRate(MatchLevelSpec spec, MatchRules rules, MatchPolicy policy, int seedCount, ulong firstSeed = 1)
        {
            if (seedCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(seedCount), "Must be at least 1.");
            }

            int wins = 0;
            for (int i = 0; i < seedCount; i++)
            {
                ulong seed = firstSeed + (ulong)i;
                MatchLevel level = MatchLevelGenerator.Generate(spec, seed);
                var board = new MatchBoard(level.Tiles, rules);
                var random = new SeededRandom(seed * 2654435761UL + 1);

                double now = 0;
                while (board.State == MatchState.Playing)
                {
                    int tile = policy(board, random);
                    if (tile < 0)
                    {
                        break;
                    }

                    now += 0.5;
                    board.TapTile(tile, now);
                }

                if (board.State == MatchState.Won)
                {
                    wins++;
                }
            }

            return (double)wins / seedCount;
        }
    }
}
