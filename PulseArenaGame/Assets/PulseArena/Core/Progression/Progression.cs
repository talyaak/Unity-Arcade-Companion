using System;
using System.Collections.Generic;
using System.Linq;

namespace PulseArena.Core
{
    /// Tuning for XP and unlocks. Pure data plus pure functions, so balance changes are testable.
    public sealed class ProgressionRules
    {
        public int XpPerHit = 10;
        public int XpPerBestComboStep = 5;

        /// Total XP needed for level n+1 is LevelThresholds[n]. Level 1 starts at 0 XP.
        public int[] LevelThresholds = { 100, 250, 450, 700 };

        public List<Cosmetic> Cosmetics = new List<Cosmetic>
        {
            new Cosmetic("lane-teal", "Teal Lanes", 2),
            new Cosmetic("ring-amber", "Amber Ring", 3),
            new Cosmetic("grid-night", "Night Grid", 5)
        };

        public int XpForRun(ChallengeResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            return result.Hits * XpPerHit + result.BestCombo * XpPerBestComboStep;
        }

        public int LevelFor(int totalXp)
        {
            int level = 1;
            foreach (int threshold in LevelThresholds)
            {
                if (totalXp < threshold)
                {
                    break;
                }

                level++;
            }

            return level;
        }
    }

    /// A fictional cosmetic. Names and ids are original placeholders, not licensed or branded assets.
    public sealed class Cosmetic
    {
        public Cosmetic(string id, string name, int unlockLevel)
        {
            Id = id;
            Name = name;
            UnlockLevel = unlockLevel;
        }

        public string Id { get; }
        public string Name { get; }
        public int UnlockLevel { get; }
    }

    public sealed class PlayerProfile
    {
        public PlayerProfile(string playerId)
        {
            PlayerId = playerId ?? throw new ArgumentNullException(nameof(playerId));
        }

        public string PlayerId { get; }
        public int TotalXp { get; private set; }
        public int Level { get; private set; } = 1;
        public HashSet<string> Unlocked { get; } = new HashSet<string>(StringComparer.Ordinal);

        /// Adds one finished run. The caller must record each run once: applying the same result twice counts it twice.
        /// Returns the cosmetics unlocked by this run, in the order they are listed in the rules.
        public IReadOnlyList<Cosmetic> ApplyRun(ChallengeResult result, ProgressionRules rules)
        {
            TotalXp += rules.XpForRun(result);
            Level = rules.LevelFor(TotalXp);

            var newly = new List<Cosmetic>();
            foreach (Cosmetic cosmetic in rules.Cosmetics)
            {
                if (Level >= cosmetic.UnlockLevel && Unlocked.Add(cosmetic.Id))
                {
                    newly.Add(cosmetic);
                }
            }

            return newly;
        }
    }
}
