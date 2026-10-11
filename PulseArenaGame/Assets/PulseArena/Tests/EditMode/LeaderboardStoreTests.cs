using System.Collections.Generic;
using NUnit.Framework;
using PulseArena.Core;

namespace PulseArena.Tests
{
    public class LeaderboardStoreTests
    {
        [Test]
        public void Combo_BreaksScoreTies_BeforeTime()
        {
            var higherCombo = new LeaderboardEntry("zed", 500, 5, 6, 1, 300);
            var laterButLowerCombo = new LeaderboardEntry("aki", 500, 4, 6, 1, 100);

            var sorted = new List<LeaderboardEntry> { laterButLowerCombo, higherCombo };
            sorted.Sort(LeaderboardOrder.Instance);

            Assert.AreEqual("zed", sorted[0].PlayerId, "combo outranks an earlier submission when scores tie");
        }

        [Test]
        public void SaveThenLoad_RoundTripsAndSorts()
        {
            var store = new LeaderboardStore(new InMemoryKeyValueStore());
            var entries = new List<LeaderboardEntry>
            {
                new LeaderboardEntry("low", 100, 1, 1, 9, 5),
                new LeaderboardEntry("high", 900, 7, 9, 1, 6)
            };

            store.Save("r1", entries);
            IReadOnlyList<LeaderboardEntry> loaded = store.Load("r1", out int skipped);

            Assert.AreEqual(0, skipped);
            Assert.AreEqual("high", loaded[0].PlayerId);
            Assert.AreEqual(7, loaded[0].BestCombo);
            Assert.AreEqual(9, loaded[1].Misses);
        }

        [Test]
        public void Load_SkipsCorruptLines_AndKeepsTheRest()
        {
            var kv = new InMemoryKeyValueStore();
            kv.Set(LeaderboardStore.KeyFor("r"), "good\t10\t1\t2\t0\t1\nbroken line\nalso\tbad\tnumbers\t1\t2\t3\n\t5\t1\t1\t0\t2");
            var store = new LeaderboardStore(kv);

            IReadOnlyList<LeaderboardEntry> loaded = store.Load("r", out int skipped);

            Assert.AreEqual(1, loaded.Count);
            Assert.AreEqual("good", loaded[0].PlayerId);
            Assert.AreEqual(3, skipped);
        }

        [Test]
        public void Load_MissingRound_IsEmpty()
        {
            var store = new LeaderboardStore(new InMemoryKeyValueStore());

            Assert.IsEmpty(store.Load("nope", out int skipped));
            Assert.AreEqual(0, skipped);
        }

        [Test]
        public void Save_RejectsTabsInPlayerId()
        {
            var store = new LeaderboardStore(new InMemoryKeyValueStore());
            var bad = new List<LeaderboardEntry> { new LeaderboardEntry("a\tb", 1, 1, 1, 0, 1) };

            Assert.Throws<System.ArgumentException>(() => store.Save("r", bad));
        }
    }
}
