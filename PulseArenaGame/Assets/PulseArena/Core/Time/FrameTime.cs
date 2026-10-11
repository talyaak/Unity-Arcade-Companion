using System;

namespace PulseArena.Core
{
    public static class FrameTime
    {
        /// Limits one frame's delta to [0, maxStep]. After a stall (app pause, focus loss, debugger break)
        /// the raw delta can be seconds long, and this stops it from skipping game time.
        /// NaN and negative input return 0, because a bad frame delta should not move the run.
        public static double Clamp(double delta, double maxStep)
        {
            if (!(maxStep > 0))
            {
                throw new ArgumentOutOfRangeException(nameof(maxStep), "Must be a positive number.");
            }

            if (!(delta > 0))
            {
                return 0;
            }

            return delta < maxStep ? delta : maxStep;
        }
    }
}
