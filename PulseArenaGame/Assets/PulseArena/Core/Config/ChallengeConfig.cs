using System.Collections.Generic;

namespace PulseArena.Core
{
    /// Tunable parameters for one challenge. `Version` is copied into every result so a score can be
    /// traced back to the configuration that produced it.
    public sealed class ChallengeConfig
    {
        public string Version = "dev-0";
        public double CountdownSeconds = 3.0;
        public double DurationSeconds = 30.0;
        public int LaneCount = 4;
        public double MinSpawnInterval = 0.6;
        public double MaxSpawnInterval = 1.1;
        public double TargetLifetime = 1.2;
        public ScoringRules Scoring = new ScoringRules();

        public static ChallengeConfig Defaults()
        {
            return new ChallengeConfig();
        }

        /// Returns every problem found, not just the first. Written as `!(x > 0)` so NaN is rejected too.
        public List<string> Validate()
        {
            var errors = new List<string>();

            if (!(CountdownSeconds >= 0)) errors.Add("CountdownSeconds must be >= 0.");
            if (!(DurationSeconds > 0)) errors.Add("DurationSeconds must be > 0.");
            if (LaneCount < 1) errors.Add("LaneCount must be >= 1.");
            if (!(MinSpawnInterval > 0)) errors.Add("MinSpawnInterval must be > 0.");
            if (!(MaxSpawnInterval >= MinSpawnInterval)) errors.Add("MaxSpawnInterval must be >= MinSpawnInterval.");
            if (!(TargetLifetime > 0)) errors.Add("TargetLifetime must be > 0.");
            if (TargetLifetime > DurationSeconds) errors.Add("TargetLifetime must not exceed DurationSeconds.");

            if (Scoring == null)
            {
                errors.Add("Scoring must be set.");
            }
            else
            {
                errors.AddRange(Scoring.Validate());
            }

            return errors;
        }
    }
}
