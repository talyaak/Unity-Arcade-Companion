using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace PulseArena.Core
{
    /// What the player has done across the stages: which stages are open, the best score on each, and the totals the
    /// campaign screen shows. Core decides every rule here; the view stores the string from Serialize and shows what this
    /// class says (D17 ruling 1). Stage numbers are 1-based, like the stage list the player sees.
    ///
    /// Rules: stage 1 is always open. Stage n opens once stage n-1 has been won. A stage that was won stays won; a
    /// later loss never takes anything away. There is no lock, no star and no currency.
    public sealed class MatchProgress
    {
        private const string Prefix = "pa-progress";
        private const int Version = 1;

        private readonly int[] _best;
        private readonly bool[] _won;

        public MatchProgress(int stageCount)
        {
            if (stageCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(stageCount), "There must be at least one stage.");
            }

            _best = new int[stageCount];
            _won = new bool[stageCount];
        }

        public MatchProgress() : this(MatchStages.All.Count)
        {
        }

        public int StageCount => _best.Length;

        /// Every win ever recorded, including repeats of a stage already won.
        public int TotalWins { get; private set; }

        public int TotalLosses { get; private set; }

        /// Stages won at least once.
        public int StagesWon
        {
            get
            {
                int count = 0;
                foreach (bool won in _won)
                {
                    if (won)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// Sum of the best score of every stage.
        public long TotalBestScore
        {
            get
            {
                long sum = 0;
                foreach (int score in _best)
                {
                    sum += score;
                }

                return sum;
            }
        }

        /// True once every stage has been won at least once.
        public bool CampaignComplete => StagesWon == StageCount;

        /// The first stage not yet won, or 0 when the campaign is complete. This is the stage "Continue" starts.
        public int NextStage
        {
            get
            {
                for (int i = 0; i < _won.Length; i++)
                {
                    if (!_won[i])
                    {
                        return i + 1;
                    }
                }

                return 0;
            }
        }

        public bool IsOpen(int stage)
        {
            CheckStage(stage);
            return stage == 1 || _won[stage - 2];
        }

        public bool HasWon(int stage)
        {
            CheckStage(stage);
            return _won[stage - 1];
        }

        /// Best score on a stage, 0 until it is won.
        public int BestScore(int stage)
        {
            CheckStage(stage);
            return _best[stage - 1];
        }

        /// Records a win. Returns true when the score beats the previous best, and also on the first win of a stage.
        /// A stage that is not open cannot be played, so recording a win on it is a caller bug and throws.
        public bool RecordWin(int stage, int score)
        {
            CheckStage(stage);
            if (score < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(score), "A score cannot be negative.");
            }

            if (!IsOpen(stage))
            {
                throw new InvalidOperationException($"Stage {stage} is not open.");
            }

            TotalWins++;
            bool first = !_won[stage - 1];
            _won[stage - 1] = true;
            if (first || score > _best[stage - 1])
            {
                _best[stage - 1] = score;
                return true;
            }

            return false;
        }

        public void RecordLoss(int stage)
        {
            CheckStage(stage);
            if (!IsOpen(stage))
            {
                throw new InvalidOperationException($"Stage {stage} is not open.");
            }

            TotalLosses++;
        }

        /// Forgets everything. The view asks the player to confirm before calling it.
        public void Reset()
        {
            Array.Clear(_best, 0, _best.Length);
            Array.Clear(_won, 0, _won.Length);
            TotalWins = 0;
            TotalLosses = 0;
        }

        /// A versioned one-line form: pa-progress;1;stages;won flags;best scores;wins;losses.
        public string Serialize()
        {
            var text = new StringBuilder();
            text.Append(Prefix).Append(';').Append(Version.ToString(CultureInfo.InvariantCulture)).Append(';');
            text.Append(StageCount.ToString(CultureInfo.InvariantCulture)).Append(';');
            foreach (bool won in _won)
            {
                text.Append(won ? '1' : '0');
            }

            text.Append(';');
            for (int i = 0; i < _best.Length; i++)
            {
                if (i > 0)
                {
                    text.Append(',');
                }

                text.Append(_best[i].ToString(CultureInfo.InvariantCulture));
            }

            text.Append(';').Append(TotalWins.ToString(CultureInfo.InvariantCulture));
            text.Append(';').Append(TotalLosses.ToString(CultureInfo.InvariantCulture));
            return text.ToString();
        }

        /// Reads the string from Serialize. Never throws. Returns false, with a fresh empty progress, for anything that
        /// is missing, from another version, for a different stage count, damaged, or that breaks the unlock rule
        /// (a stage won while the one before it was not). The caller then starts fresh rather than guessing.
        public static bool TryParse(string text, int stageCount, out MatchProgress progress)
        {
            progress = new MatchProgress(stageCount);
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            string[] parts = text.Split(';');
            if (parts.Length != 7 || parts[0] != Prefix)
            {
                return false;
            }

            if (!TryInt(parts[1], out int version) || version != Version || !TryInt(parts[2], out int count) || count != stageCount)
            {
                return false;
            }

            string flags = parts[3];
            string[] scores = parts[4].Split(',');
            if (flags.Length != stageCount || scores.Length != stageCount
                || !TryInt(parts[5], out int wins) || !TryInt(parts[6], out int losses) || wins < 0 || losses < 0)
            {
                return false;
            }

            var candidate = new MatchProgress(stageCount) { TotalWins = wins, TotalLosses = losses };
            for (int i = 0; i < stageCount; i++)
            {
                if (flags[i] != '0' && flags[i] != '1')
                {
                    return false;
                }

                if (!TryInt(scores[i], out int score) || score < 0)
                {
                    return false;
                }

                bool won = flags[i] == '1';
                if ((won && i > 0 && !candidate._won[i - 1]) || (!won && score != 0))
                {
                    return false;
                }

                candidate._won[i] = won;
                candidate._best[i] = score;
            }

            progress = candidate;
            return true;
        }

        public static bool TryParse(string text, out MatchProgress progress)
        {
            return TryParse(text, MatchStages.All.Count, out progress);
        }

        private static bool TryInt(string text, out int value)
        {
            return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value);
        }

        private void CheckStage(int stage)
        {
            if (stage < 1 || stage > StageCount)
            {
                throw new ArgumentOutOfRangeException(nameof(stage), $"Stage must be 1 to {StageCount}.");
            }
        }
    }
}
