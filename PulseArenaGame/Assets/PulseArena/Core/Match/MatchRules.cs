using System.Collections.Generic;

namespace PulseArena.Core
{
    /// Tuning for the match game. Plain data. Validate reports every problem at once, like ChallengeConfig.
    /// These are this game's own starting values, not taken from any other game.
    public sealed class MatchRules
    {
        /// Tiles the tray holds. Filling it without a triple loses the level.
        public int TrayCapacity = 7;

        /// Clears closer together than this (seconds) extend the combo.
        public double ComboWindowSeconds = 3.0;

        /// Points for one cleared triple, before the combo multiplier.
        public int ClearPoints = 100;

        /// Upper bound for the combo multiplier.
        public int MaxComboMultiplier = 4;

        public List<string> Validate()
        {
            var errors = new List<string>();
            if (TrayCapacity < MatchBoard.MatchSize)
            {
                errors.Add($"TrayCapacity must be at least {MatchBoard.MatchSize} so a triple can fit.");
            }

            if (!(ComboWindowSeconds > 0) || double.IsInfinity(ComboWindowSeconds))
            {
                errors.Add("ComboWindowSeconds must be a positive, finite number.");
            }

            if (ClearPoints <= 0)
            {
                errors.Add("ClearPoints must be positive.");
            }

            if (MaxComboMultiplier < 1)
            {
                errors.Add("MaxComboMultiplier must be at least 1.");
            }

            return errors;
        }
    }
}
