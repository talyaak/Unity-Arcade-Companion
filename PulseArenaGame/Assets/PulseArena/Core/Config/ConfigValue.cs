using System;

namespace PulseArena.Core
{
    public static class ConfigValue
    {
        /// Converts an Inspector float to the double a literal would give, e.g. 1.2f -> 1.2.
        /// Unity serializes float fields, so tuning is stored as float. Without this rounding, a value like 1.2f
        /// becomes 1.2000000476837158, and a run played with the asset would not match ChallengeConfig.Defaults().
        public static double FromFloat(float value)
        {
            return Math.Round(value, 6);
        }
    }
}
