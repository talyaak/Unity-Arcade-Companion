using System;
using NUnit.Framework;
using PulseArena.Core;

namespace PulseArena.Tests
{
    public class FrameTimeTests
    {
        [Test]
        public void NormalDelta_PassesThrough()
        {
            Assert.AreEqual(0.016, FrameTime.Clamp(0.016, 0.1), 1e-12);
        }

        [Test]
        public void LargeDelta_IsCappedAtMaxStep()
        {
            Assert.AreEqual(0.1, FrameTime.Clamp(3.0, 0.1), 1e-12);
        }

        [Test]
        public void NegativeOrNaN_ReturnsZero()
        {
            Assert.AreEqual(0, FrameTime.Clamp(-1, 0.1));
            Assert.AreEqual(0, FrameTime.Clamp(double.NaN, 0.1));
        }

        [Test]
        public void NonPositiveMaxStep_IsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => FrameTime.Clamp(0.016, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => FrameTime.Clamp(0.016, double.NaN));
        }
    }
}
