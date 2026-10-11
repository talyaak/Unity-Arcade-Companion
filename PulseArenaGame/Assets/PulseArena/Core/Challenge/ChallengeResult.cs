namespace PulseArena.Core
{
    /// Immutable summary of a finished run. Hits + misses equals the number of scheduled targets.
    public sealed class ChallengeResult
    {
        public ChallengeResult(int score, int hits, int misses, int bestCombo, ulong seed, string configVersion)
        {
            Score = score;
            Hits = hits;
            Misses = misses;
            BestCombo = bestCombo;
            Seed = seed;
            ConfigVersion = configVersion;
        }

        public int Score { get; }
        public int Hits { get; }
        public int Misses { get; }
        public int BestCombo { get; }
        public ulong Seed { get; }
        public string ConfigVersion { get; }

        public double Accuracy => Hits + Misses == 0 ? 0 : (double)Hits / (Hits + Misses);
    }
}
