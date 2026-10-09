using System.Collections.Generic;

namespace PulseArena.Core
{
    /// Plain scoring parameters. No UnityEngine types, so the rules can be tested without the engine.
    public sealed class ScoringRules
    {
        public int BasePoints = 100;

        /// Consecutive hits needed to raise the multiplier by one step.
        public int ComboStep = 5;

        public int MaxMultiplier = 4;

        /// Points removed per missed target. The score never drops below zero.
        public int MissPenalty = 50;

        public int MultiplierFor(int combo)
        {
            int step = 1 + combo / ComboStep;
            return step < MaxMultiplier ? step : MaxMultiplier;
        }

        public List<string> Validate()
        {
            var errors = new List<string>();
            if (BasePoints < 0) errors.Add("BasePoints must be >= 0.");
            if (ComboStep < 1) errors.Add("ComboStep must be >= 1.");
            if (MaxMultiplier < 1) errors.Add("MaxMultiplier must be >= 1.");
            if (MissPenalty < 0) errors.Add("MissPenalty must be >= 0.");
            return errors;
        }
    }
}
