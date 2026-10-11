using System;
using NUnit.Framework;
using PulseArena.Core;

namespace PulseArena.Tests
{
    public class MatchProgressTests
    {
        [Test]
        public void New_OnlyStageOneIsOpen_AndNothingIsWon()
        {
            var progress = new MatchProgress();
            Assert.AreEqual(MatchStages.All.Count, progress.StageCount);
            Assert.IsTrue(progress.IsOpen(1));
            for (int stage = 2; stage <= progress.StageCount; stage++)
            {
                Assert.IsFalse(progress.IsOpen(stage), $"stage {stage}");
            }

            Assert.AreEqual(0, progress.StagesWon);
            Assert.AreEqual(1, progress.NextStage);
            Assert.IsFalse(progress.CampaignComplete);
            Assert.AreEqual(0, progress.TotalBestScore);
        }

        [Test]
        public void Win_OpensTheNextStage_AndOnlyTheNext()
        {
            var progress = new MatchProgress();
            progress.RecordWin(1, 400);
            Assert.IsTrue(progress.IsOpen(2));
            Assert.IsFalse(progress.IsOpen(3));
            Assert.AreEqual(2, progress.NextStage);
        }

        [Test]
        public void Loss_NeverOpensOrClosesAnything()
        {
            var progress = new MatchProgress();
            progress.RecordLoss(1);
            Assert.IsFalse(progress.IsOpen(2));
            progress.RecordWin(1, 400);
            progress.RecordLoss(1);
            Assert.IsTrue(progress.IsOpen(2));
            Assert.IsTrue(progress.HasWon(1));
            Assert.AreEqual(400, progress.BestScore(1));
            Assert.AreEqual(2, progress.TotalLosses);
            Assert.AreEqual(1, progress.TotalWins);
        }

        [Test]
        public void RecordWin_FirstWinIsAlwaysABest_EvenAtZero()
        {
            var progress = new MatchProgress();
            Assert.IsTrue(progress.RecordWin(1, 0));
            Assert.IsTrue(progress.HasWon(1));
        }

        [Test]
        public void RecordWin_FlagsOnlyAStrictlyBetterScore()
        {
            var progress = new MatchProgress();
            Assert.IsTrue(progress.RecordWin(1, 400));
            Assert.IsFalse(progress.RecordWin(1, 400), "a tie is not a new best");
            Assert.IsFalse(progress.RecordWin(1, 300));
            Assert.AreEqual(400, progress.BestScore(1));
            Assert.IsTrue(progress.RecordWin(1, 700));
            Assert.AreEqual(700, progress.BestScore(1));
            Assert.AreEqual(4, progress.TotalWins);
        }

        [Test]
        public void RecordWin_OnAClosedStage_Throws()
        {
            var progress = new MatchProgress();
            Assert.Throws<InvalidOperationException>(() => progress.RecordWin(2, 100));
            Assert.Throws<InvalidOperationException>(() => progress.RecordLoss(3));
            Assert.AreEqual(0, progress.TotalWins);
            Assert.AreEqual(0, progress.TotalLosses);
        }

        [Test]
        public void BadArguments_Throw()
        {
            var progress = new MatchProgress();
            Assert.Throws<ArgumentOutOfRangeException>(() => progress.IsOpen(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => progress.BestScore(progress.StageCount + 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => progress.RecordWin(1, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MatchProgress(0));
        }

        [Test]
        public void Campaign_CompletesAfterEveryStageIsWon_AndRepeatsKeepTheBest()
        {
            var progress = new MatchProgress();
            for (int stage = 1; stage <= progress.StageCount; stage++)
            {
                Assert.IsFalse(progress.CampaignComplete);
                progress.RecordWin(stage, stage * 100);
            }

            Assert.IsTrue(progress.CampaignComplete);
            Assert.AreEqual(0, progress.NextStage);
            Assert.AreEqual(1500, progress.TotalBestScore);
            progress.RecordWin(3, 50);
            Assert.AreEqual(1500, progress.TotalBestScore);
            Assert.IsTrue(progress.CampaignComplete);
        }

        [Test]
        public void Reset_ForgetsEverything()
        {
            var progress = new MatchProgress();
            progress.RecordWin(1, 400);
            progress.RecordWin(2, 500);
            progress.RecordLoss(3);
            progress.Reset();
            Assert.IsFalse(progress.IsOpen(2));
            Assert.AreEqual(0, progress.BestScore(1));
            Assert.AreEqual(0, progress.TotalWins);
            Assert.AreEqual(0, progress.TotalLosses);
            Assert.AreEqual(new MatchProgress().Serialize(), progress.Serialize());
        }

        [Test]
        public void Serialize_RoundTrips()
        {
            var progress = new MatchProgress();
            progress.RecordWin(1, 400);
            progress.RecordLoss(2);
            progress.RecordWin(2, 900);
            progress.RecordWin(3, 1200);
            progress.RecordWin(1, 700);

            Assert.IsTrue(MatchProgress.TryParse(progress.Serialize(), out MatchProgress back));
            Assert.AreEqual(progress.Serialize(), back.Serialize());
            Assert.AreEqual(700, back.BestScore(1));
            Assert.AreEqual(1200, back.BestScore(3));
            Assert.IsTrue(back.IsOpen(4));
            Assert.IsFalse(back.IsOpen(5));
            Assert.AreEqual(4, back.TotalWins);
            Assert.AreEqual(1, back.TotalLosses);
        }

        [Test]
        public void Serialize_IsOneVersionedLine()
        {
            string text = new MatchProgress().Serialize();
            Assert.AreEqual("pa-progress;1;5;00000;0,0,0,0,0;0;0", text);
        }

        [Test]
        public void Serialize_ReadsTheSameUnderAnyCulture()
        {
            var original = System.Threading.Thread.CurrentThread.CurrentCulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("ar-SA");
                var progress = new MatchProgress();
                progress.RecordWin(1, 1234);
                string text = progress.Serialize();
                Assert.IsTrue(MatchProgress.TryParse(text, out MatchProgress back));
                Assert.AreEqual(1234, back.BestScore(1));
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = original;
            }
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("hello")]
        [TestCase("pa-progress;2;5;00000;0,0,0,0,0;0;0")]
        [TestCase("pa-progress;1;4;0000;0,0,0,0;0;0")]
        [TestCase("pa-progress;1;5;0000;0,0,0,0,0;0;0")]
        [TestCase("pa-progress;1;5;00000;0,0,0,0;0;0")]
        [TestCase("pa-progress;1;5;00000;0,0,0,0,0;0")]
        [TestCase("pa-progress;1;5;00000;0,0,0,0,0;0;0;9")]
        [TestCase("pa-progress;1;5;0x000;0,0,0,0,0;0;0")]
        [TestCase("pa-progress;1;5;10000;-5,0,0,0,0;0;0")]
        [TestCase("pa-progress;1;5;10000;99999999999,0,0,0,0;0;0")]
        [TestCase("pa-progress;1;5;00000;100,0,0,0,0;0;0")]
        [TestCase("pa-progress;1;5;01000;0,100,0,0,0;1;0")]
        [TestCase("pa-progress;1;5;10000;100,0,0,0,0;-1;0")]
        [TestCase("pa-progress;1;5;10000;1.5,0,0,0,0;1;0")]
        public void TryParse_RejectsBadInput_AndGivesAFreshProgress(string text)
        {
            Assert.IsFalse(MatchProgress.TryParse(text, out MatchProgress progress));
            Assert.IsNotNull(progress);
            Assert.AreEqual(new MatchProgress().Serialize(), progress.Serialize());
        }

        [Test]
        public void TryParse_AcceptsAnyStageCountItIsToldToExpect()
        {
            var progress = new MatchProgress(2);
            progress.RecordWin(1, 10);
            Assert.IsTrue(MatchProgress.TryParse(progress.Serialize(), 2, out MatchProgress back));
            Assert.AreEqual(10, back.BestScore(1));
            Assert.IsFalse(MatchProgress.TryParse(progress.Serialize(), 5, out _), "a save from a different stage list is not trusted");
        }
    }
}
