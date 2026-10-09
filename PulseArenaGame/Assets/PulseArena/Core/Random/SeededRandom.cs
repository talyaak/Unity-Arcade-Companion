using System;

namespace PulseArena.Core
{
    /// Deterministic SplitMix64 generator. Unlike UnityEngine.Random (global state, backend-dependent),
    /// the same seed produces the same sequence on every platform, so runs can be replayed and tested.
    public sealed class SeededRandom
    {
        private ulong _state;

        public SeededRandom(ulong seed)
        {
            _state = seed;
        }

        public ulong NextUInt64()
        {
            unchecked
            {
                _state += 0x9E3779B97F4A7C15UL;
                ulong z = _state;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        /// Uniform integer in [0, maxExclusive). Modulo bias is negligible for the small ranges used here.
        public int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), "Must be positive.");
            }

            return (int)(NextUInt64() % (ulong)maxExclusive);
        }

        /// Uniform double in [0, 1), built from the top 53 bits so every value is exactly representable.
        public double NextDouble()
        {
            return (NextUInt64() >> 11) * (1.0 / 9007199254740992.0);
        }

        public double NextDouble(double min, double max)
        {
            return min + (max - min) * NextDouble();
        }
    }
}
