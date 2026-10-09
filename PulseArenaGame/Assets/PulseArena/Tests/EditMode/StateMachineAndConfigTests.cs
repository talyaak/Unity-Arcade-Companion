using NUnit.Framework;
using PulseArena.Core;

namespace PulseArena.Tests
{
    public class StateMachineAndConfigTests
    {
        [TestCase(ChallengeState.Idle, ChallengeCommand.Begin, ChallengeState.Countdown)]
        [TestCase(ChallengeState.Countdown, ChallengeCommand.CountdownFinished, ChallengeState.Playing)]
        [TestCase(ChallengeState.Countdown, ChallengeCommand.Quit, ChallengeState.Idle)]
        [TestCase(ChallengeState.Playing, ChallengeCommand.TimeUp, ChallengeState.Results)]
        [TestCase(ChallengeState.Playing, ChallengeCommand.Quit, ChallengeState.Idle)]
        [TestCase(ChallengeState.Results, ChallengeCommand.Begin, ChallengeState.Countdown)]
        [TestCase(ChallengeState.Results, ChallengeCommand.Quit, ChallengeState.Idle)]
        public void ValidTransitions_Apply(ChallengeState from, ChallengeCommand command, ChallengeState expected)
        {
            Assert.IsTrue(ChallengeStateMachine.TryGetNext(from, command, out ChallengeState next));
            Assert.AreEqual(expected, next);
        }

        [TestCase(ChallengeState.Idle, ChallengeCommand.TimeUp)]
        [TestCase(ChallengeState.Idle, ChallengeCommand.CountdownFinished)]
        [TestCase(ChallengeState.Countdown, ChallengeCommand.TimeUp)]
        [TestCase(ChallengeState.Playing, ChallengeCommand.Begin)]
        [TestCase(ChallengeState.Results, ChallengeCommand.TimeUp)]
        public void InvalidTransitions_AreRejected(ChallengeState from, ChallengeCommand command)
        {
            Assert.IsFalse(ChallengeStateMachine.TryGetNext(from, command, out ChallengeState next));
            Assert.AreEqual(from, next);
        }

        [Test]
        public void Defaults_AreValid()
        {
            Assert.IsEmpty(ChallengeConfig.Defaults().Validate());
        }

        [Test]
        public void Validate_ReportsAllProblemsAtOnce()
        {
            var config = ChallengeConfig.Defaults();
            config.DurationSeconds = 0;
            config.LaneCount = 0;
            config.MaxSpawnInterval = 0.1; // below MinSpawnInterval (0.6)

            Assert.That(config.Validate().Count, Is.GreaterThanOrEqualTo(3));
        }

        [Test]
        public void Validate_RejectsNaN()
        {
            var config = ChallengeConfig.Defaults();
            config.TargetLifetime = double.NaN;

            Assert.IsNotEmpty(config.Validate());
        }

        [Test]
        public void Validate_RejectsLifetimeLongerThanRun()
        {
            var config = ChallengeConfig.Defaults();
            config.TargetLifetime = config.DurationSeconds + 1;

            Assert.IsNotEmpty(config.Validate());
        }
    }
}
