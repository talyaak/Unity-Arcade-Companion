using NUnit.Framework;
using PulseArena.Core;

namespace PulseArena.Tests
{
    public class ConfigValueTests
    {
        [Test]
        public void FromFloat_MatchesDoubleLiteral()
        {
            Assert.AreEqual(1.2, ConfigValue.FromFloat(1.2f));
            Assert.AreEqual(0.6, ConfigValue.FromFloat(0.6f));
            Assert.AreEqual(1.1, ConfigValue.FromFloat(1.1f));
            Assert.AreEqual(30.0, ConfigValue.FromFloat(30f));
        }

        // The Game asset's serialized defaults must equal the code defaults, or Inspector-built runs drift from replays.
        [Test]
        public void AssetDefaults_RoundTripToCodeDefaults()
        {
            ChallengeConfig code = ChallengeConfig.Defaults();

            Assert.AreEqual(code.CountdownSeconds, ConfigValue.FromFloat(3f));
            Assert.AreEqual(code.DurationSeconds, ConfigValue.FromFloat(30f));
            Assert.AreEqual(code.MinSpawnInterval, ConfigValue.FromFloat(0.6f));
            Assert.AreEqual(code.MaxSpawnInterval, ConfigValue.FromFloat(1.1f));
            Assert.AreEqual(code.TargetLifetime, ConfigValue.FromFloat(1.2f));
        }
    }
}
