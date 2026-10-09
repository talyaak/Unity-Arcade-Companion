using System.Collections.Generic;

namespace PulseArena.Core
{
    /// One target on the timeline. Times are seconds since play started (the countdown is excluded).
    public readonly struct TargetSpec
    {
        public TargetSpec(int lane, double spawnTime, double expiresAt)
        {
            Lane = lane;
            SpawnTime = spawnTime;
            ExpiresAt = expiresAt;
        }

        public int Lane { get; }
        public double SpawnTime { get; }
        public double ExpiresAt { get; }
    }

    /// Builds the full target timeline for a run from its seed, before play starts. Because the timeline is
    /// fixed, the same seed and the same taps always produce the same result: this is what makes replays
    /// and tests possible.
    public static class ChallengeSchedule
    {
        public static List<TargetSpec> Generate(ChallengeConfig config, SeededRandom random)
        {
            var targets = new List<TargetSpec>();
            var laneFreeAt = new double[config.LaneCount];
            double time = 0;

            while (true)
            {
                time += random.NextDouble(config.MinSpawnInterval, config.MaxSpawnInterval);
                double expiresAt = time + config.TargetLifetime;

                // Every target must resolve inside the run, so no miss is left pending at the end.
                if (expiresAt > config.DurationSeconds)
                {
                    break;
                }

                int lane = random.NextInt(config.LaneCount);
                if (laneFreeAt[lane] > time)
                {
                    lane = FindFreeLane(laneFreeAt, time);
                    if (lane < 0)
                    {
                        continue;
                    }
                }

                laneFreeAt[lane] = expiresAt;
                targets.Add(new TargetSpec(lane, time, expiresAt));
            }

            return targets;
        }

        private static int FindFreeLane(double[] laneFreeAt, double time)
        {
            for (int lane = 0; lane < laneFreeAt.Length; lane++)
            {
                if (laneFreeAt[lane] <= time)
                {
                    return lane;
                }
            }

            return -1;
        }
    }
}
