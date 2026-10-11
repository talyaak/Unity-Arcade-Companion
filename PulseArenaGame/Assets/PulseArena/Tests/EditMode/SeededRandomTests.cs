using NUnit.Framework;
using PulseArena.Core;

namespace PulseArena.Tests
{
    public class SeededRandomTests
    {
        [Test]
        public void SameSeed_ProducesSameSequence()
        {
            var a = new SeededRandom(1234);
            var b = new SeededRandom(1234);

            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(a.NextUInt64(), b.NextUInt64());
            }
        }

        [Test]
        public void DifferentSeeds_ProduceDifferentFirstValues()
        {
            Assert.AreNotEqual(new SeededRandom(1).NextUInt64(), new SeededRandom(2).NextUInt64());
        }

        // Regression lock: these values must not change across platforms or Unity backends (Mono/IL2CPP).
        [Test]
        public void KnownSeed_ProducesFixedFirstValue()
        {
            Assert.AreEqual(0xE220A8397B1DCDAFUL, new SeededRandom(0).NextUInt64());
        }

        [Test]
        public void NextInt_StaysWithinRange()
        {
            var random = new SeededRandom(99);
            for (int i = 0; i < 1000; i++)
            {
                int value = random.NextInt(7);
                Assert.That(value, Is.InRange(0, 6));
            }
        }

        [Test]
        public void NextInt_RejectsNonPositiveBound()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new SeededRandom(1).NextInt(0));
        }

        [Test]
        public void NextDouble_IsWithinUnitInterval()
        {
            var random = new SeededRandom(7);
            for (int i = 0; i < 1000; i++)
            {
                double value = random.NextDouble();
                Assert.That(value, Is.GreaterThanOrEqualTo(0.0).And.LessThan(1.0));
            }
        }
    }
}
