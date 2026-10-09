using System;

namespace PulseArena.Core
{
    public readonly struct HitOutcome
    {
        public HitOutcome(int points, int multiplier, int comboAfter)
        {
            Points = points;
            Multiplier = multiplier;
            ComboAfter = comboAfter;
        }

        public int Points { get; }
        public int Multiplier { get; }
        public int ComboAfter { get; }
    }

    /// Owns score and combo state for one run. Changed only through RegisterHit and RegisterMiss.
    public sealed class ScoreTracker
    {
        private readonly ScoringRules _rules;

        public ScoreTracker(ScoringRules rules)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        }

        public int Score { get; private set; }
        public int Combo { get; private set; }
        public int BestCombo { get; private set; }
        public int Hits { get; private set; }
        public int Misses { get; private set; }

        /// The multiplier is read before the combo increases, so the hit that reaches a new step
        /// is still paid at the old multiplier. Keep this rule in sync with the tests.
        public HitOutcome RegisterHit()
        {
            int multiplier = _rules.MultiplierFor(Combo);
            int points = _rules.BasePoints * multiplier;

            Score += points;
            Combo++;
            Hits++;
            if (Combo > BestCombo)
            {
                BestCombo = Combo;
            }

            return new HitOutcome(points, multiplier, Combo);
        }

        public void RegisterMiss()
        {
            Misses++;
            Combo = 0;
            Score = Math.Max(0, Score - _rules.MissPenalty);
        }
    }
}
