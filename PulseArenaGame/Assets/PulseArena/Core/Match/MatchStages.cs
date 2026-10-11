using System.Collections.Generic;

namespace PulseArena.Core
{
    /// Level shapes in play order. These are starting values. Tune them from simulator win rates, not by feel.
    public static class MatchStages
    {
        public static readonly IReadOnlyList<MatchLevelSpec> All = new List<MatchLevelSpec>
        {
            new MatchLevelSpec(symbolCount: 3, tripleCount: 4, gridWidth: 3, gridHeight: 3),
            new MatchLevelSpec(symbolCount: 4, tripleCount: 7, gridWidth: 4, gridHeight: 4),
            new MatchLevelSpec(symbolCount: 5, tripleCount: 10, gridWidth: 5, gridHeight: 5),
            new MatchLevelSpec(symbolCount: 6, tripleCount: 14, gridWidth: 5, gridHeight: 5),
            new MatchLevelSpec(symbolCount: 7, tripleCount: 18, gridWidth: 6, gridHeight: 6)
        };
    }
}
