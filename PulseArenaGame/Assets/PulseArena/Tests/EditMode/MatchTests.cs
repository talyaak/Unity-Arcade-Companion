using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PulseArena.Core;

namespace PulseArena.Tests
{
    public class MatchTests
    {
        private static MatchRules Rules(int capacity = 7) => new MatchRules { TrayCapacity = capacity };

        // Symbols 0..symbols-1, three tiles each. Symbol s uses ids 3s..3s+2. Every tile has its own cell, so all are free.
        private static List<MatchTile> FlatBoard(int symbols)
        {
            var tiles = new List<MatchTile>();
            for (int id = 0; id < symbols * 3; id++)
            {
                tiles.Add(new MatchTile(id, id / 3, id, 0, 0));
            }

            return tiles;
        }

        private static MatchBoard Board(int symbols, MatchRules rules = null) => new MatchBoard(FlatBoard(symbols), rules ?? Rules());

        // Symbol 0 stacked in one cell (ids 0 on top, then 1, then 2). Symbol 1 flat in three other cells.
        private static MatchBoard StackedBoard()
        {
            return new MatchBoard(new[]
            {
                new MatchTile(0, 0, 0, 0, 2),
                new MatchTile(1, 0, 0, 0, 1),
                new MatchTile(2, 0, 0, 0, 0),
                new MatchTile(3, 1, 1, 0, 0),
                new MatchTile(4, 1, 2, 0, 0),
                new MatchTile(5, 1, 3, 0, 0)
            }, Rules());
        }

        private static void Clear(MatchBoard board, int symbol, double time)
        {
            for (int k = 0; k < MatchBoard.MatchSize; k++)
            {
                board.TapTile(symbol * MatchBoard.MatchSize + k, time);
            }
        }

        // ---- Free rule and blocking ----

        [Test]
        public void Free_TopTileIsFree_CoveredTilesAreBlocked_UntilTheTopIsGone()
        {
            MatchBoard board = StackedBoard();

            Assert.IsTrue(board.IsFree(0));
            Assert.IsFalse(board.IsFree(1));
            Assert.IsFalse(board.IsFree(2));

            Assert.AreEqual(MatchOutcome.Moved, board.TapTile(0, 1).Outcome);
            Assert.IsTrue(board.IsFree(1));
        }

        [Test]
        public void BlockedTap_ChangesNothing()
        {
            MatchBoard board = StackedBoard();

            MatchStep step = board.TapTile(2, 1);

            Assert.AreEqual(MatchOutcome.Blocked, step.Outcome);
            Assert.AreEqual(0, board.Tray.Count);
            Assert.AreEqual(6, board.RemainingOnBoard);
            Assert.AreEqual(MatchState.Playing, board.State);
            Assert.AreEqual(0, board.Score);
        }

        [Test]
        public void UnknownOrTrayTile_IsIgnored()
        {
            MatchBoard board = StackedBoard();
            board.TapTile(0, 1);

            Assert.AreEqual(MatchOutcome.Ignored, board.TapTile(0, 2).Outcome, "already in the tray");
            Assert.AreEqual(MatchOutcome.Ignored, board.TapTile(99, 2).Outcome, "unknown id");
        }

        // ---- Triples, scoring, overflow, win ----

        [Test]
        public void ThreeOfAKind_Clears_AndScores()
        {
            MatchBoard board = Board(2);
            board.TapTile(0, 1);
            board.TapTile(1, 1.1);

            MatchStep step = board.TapTile(2, 1.2);

            Assert.AreEqual(MatchOutcome.Cleared, step.Outcome);
            Assert.AreEqual(0, board.Tray.Count);
            Assert.AreEqual(100, board.Score);
            Assert.AreEqual(3, board.TilesCleared);
            Assert.AreEqual(MatchState.Playing, board.State);
        }

        [Test]
        public void SeventhTileWithoutTriple_Loses_AndLaterTapsAreIgnored()
        {
            MatchBoard board = Board(7);   // 21 tiles, all free

            for (int symbol = 0; symbol < 6; symbol++)
            {
                Assert.AreEqual(MatchOutcome.Moved, board.TapTile(symbol * 3, symbol).Outcome);
            }

            Assert.AreEqual(MatchOutcome.Lost, board.TapTile(18, 6).Outcome);
            Assert.AreEqual(MatchState.Lost, board.State);
            Assert.AreEqual(MatchOutcome.Ignored, board.TapTile(1, 7).Outcome);
        }

        [Test]
        public void SeventhTileThatCompletesATriple_ClearsInsteadOfLosing()
        {
            MatchBoard board = Board(5);
            board.TapTile(0, 1);   // symbol 0
            board.TapTile(1, 1);   // symbol 0
            board.TapTile(3, 1);   // symbol 1
            board.TapTile(6, 1);   // symbol 2
            board.TapTile(9, 1);   // symbol 3
            board.TapTile(12, 1);  // symbol 4
            Assert.AreEqual(6, board.Tray.Count, "tray is one slot from full, with no triple yet");

            MatchStep step = board.TapTile(2, 1);   // third symbol-0 tile fills the last slot and completes a triple

            Assert.AreEqual(MatchOutcome.Cleared, step.Outcome);
            Assert.AreEqual(MatchState.Playing, board.State);
            Assert.AreEqual(4, board.Tray.Count);
        }

        [Test]
        public void LastTripleOnBoard_Wins()
        {
            MatchBoard board = Board(1);
            board.TapTile(0, 1);
            board.TapTile(1, 1);

            MatchStep step = board.TapTile(2, 1);

            Assert.AreEqual(MatchOutcome.Won, step.Outcome);
            Assert.AreEqual(MatchState.Won, board.State);
            Assert.AreEqual(100, board.Score);
        }

        [Test]
        public void Combo_ExtendsInsideWindow_AndResetsOutsideIt()
        {
            MatchBoard board = Board(4);
            Clear(board, 0, 1);
            Clear(board, 1, 2);    // 1 s after the last clear: inside the 3 s window
            Clear(board, 2, 10);   // 8 s later: outside the window

            Assert.AreEqual(100 + 200 + 100, board.Score);
            Assert.AreEqual(2, board.BestCombo);
            Assert.AreEqual(1, board.Combo);
        }

        [Test]
        public void ComboMultiplier_IsCapped()
        {
            MatchBoard board = Board(4, new MatchRules { MaxComboMultiplier = 2 });
            Clear(board, 0, 1);
            Clear(board, 1, 1.5);
            Clear(board, 2, 2);   // combo 3, capped at x2

            Assert.AreEqual(100 + 200 + 200, board.Score);
        }

        [Test]
        public void Time_MustNotGoBackwards_AndMustBeANumber()
        {
            MatchBoard board = Board(2);
            board.TapTile(0, 2);

            Assert.Throws<ArgumentOutOfRangeException>(() => board.TapTile(1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => board.TapTile(1, double.NaN));
        }

        [Test]
        public void Constructor_RejectsSymbolCountsThatAreNotTriples()
        {
            var tiles = new[] { new MatchTile(0, 0, 0, 0, 0), new MatchTile(1, 0, 1, 0, 0) };

            Assert.Throws<ArgumentException>(() => new MatchBoard(tiles, Rules()));
        }

        [Test]
        public void Constructor_RejectsRulesThatCannotHoldATriple()
        {
            Assert.Throws<ArgumentException>(() => new MatchBoard(FlatBoard(1), Rules(capacity: 2)));
        }

        // ---- Generator ----

        [Test]
        public void Generate_IsDeterministic_ForSpecAndSeed()
        {
            MatchLevelSpec spec = MatchStages.All[2];
            MatchLevel a = MatchLevelGenerator.Generate(spec, 77);
            MatchLevel b = MatchLevelGenerator.Generate(spec, 77);

            CollectionAssert.AreEqual(a.IntendedOrder, b.IntendedOrder);
            for (int i = 0; i < a.Tiles.Count; i++)
            {
                Assert.AreEqual(a.Tiles[i].Id, b.Tiles[i].Id);
                Assert.AreEqual(a.Tiles[i].Symbol, b.Tiles[i].Symbol);
                Assert.AreEqual(a.Tiles[i].X, b.Tiles[i].X);
                Assert.AreEqual(a.Tiles[i].Y, b.Tiles[i].Y);
                Assert.AreEqual(a.Tiles[i].Layer, b.Tiles[i].Layer);
            }
        }

        [Test]
        public void Generate_InvariantsHold_ForEveryStageAndSeed()
        {
            foreach (MatchLevelSpec spec in MatchStages.All)
            {
                for (ulong seed = 1; seed <= 20; seed++)
                {
                    MatchLevel level = MatchLevelGenerator.Generate(spec, seed);
                    int expected = spec.TripleCount * MatchBoard.MatchSize;

                    Assert.AreEqual(expected, level.Tiles.Count);
                    Assert.AreEqual(expected, level.Tiles.Select(t => t.Id).Distinct().Count(), "ids unique");
                    Assert.AreEqual(expected, level.Tiles.Select(t => t.Layer).Distinct().Count(), "layers unique");
                    CollectionAssert.AreEquivalent(level.Tiles.Select(t => t.Id), level.IntendedOrder);
                    Assert.IsTrue(level.Tiles.All(t => t.X >= 0 && t.X < spec.GridWidth && t.Y >= 0 && t.Y < spec.GridHeight),
                        "cells inside the grid");
                    foreach (IGrouping<int, MatchTile> group in level.Tiles.GroupBy(t => t.Symbol))
                    {
                        Assert.AreEqual(0, group.Count() % MatchBoard.MatchSize, "symbol counts are triples");
                    }
                }
            }
        }

        [Test]
        public void Generate_EveryStageUsesEverySymbol_ForEverySeed()
        {
            foreach (MatchLevelSpec spec in MatchStages.All)
            {
                for (ulong seed = 1; seed <= 300; seed++)
                {
                    MatchLevel level = MatchLevelGenerator.Generate(spec, seed);

                    Assert.AreEqual(spec.SymbolCount, level.Tiles.Select(t => t.Symbol).Distinct().Count(),
                        $"symbols {spec.SymbolCount} seed {seed}");
                }
            }
        }

        [Test]
        public void EveryStage_WithFourOrMoreSymbols_CanBeLost_ButStageOneCannot()
        {
            // A tray of 7 can hold two of each symbol without a triple, so it fills only with 4 or more symbols.
            foreach (MatchLevelSpec spec in MatchStages.All)
            {
                bool canFill = spec.SymbolCount * (MatchBoard.MatchSize - 1) >= Rules().TrayCapacity;
                Assert.AreEqual(spec.SymbolCount >= 4, canFill, $"symbols {spec.SymbolCount}");
            }
        }

        [Test]
        public void IntendedOrder_WinsEveryStage_AndTrayNeverHoldsMoreThanTwo()
        {
            foreach (MatchLevelSpec spec in MatchStages.All)
            {
                for (ulong seed = 1; seed <= 100; seed++)
                {
                    MatchLevel level = MatchLevelGenerator.Generate(spec, seed);
                    var board = new MatchBoard(level.Tiles, Rules());
                    double now = 0;

                    foreach (int id in level.IntendedOrder)
                    {
                        now += 0.5;
                        MatchStep step = board.TapTile(id, now);
                        Assert.That(step.Outcome,
                            Is.EqualTo(MatchOutcome.Moved).Or.EqualTo(MatchOutcome.Cleared).Or.EqualTo(MatchOutcome.Won),
                            $"stage {spec.SymbolCount} seed {seed}: tile {id} should always be free in clear order");
                        Assert.That(board.Tray.Count, Is.LessThanOrEqualTo(2), $"seed {seed}");
                    }

                    Assert.AreEqual(MatchState.Won, board.State, $"seed {seed}");
                }
            }
        }

        // ---- Physics mode: the view reports the free set ----

        private static MatchBoard LooseBoard(int symbols)
        {
            var tiles = new List<MatchTile>();
            for (int id = 0; id < symbols * 3; id++)
            {
                tiles.Add(MatchTile.Loose(id, id / 3));
            }

            return new MatchBoard(tiles, Rules());
        }

        [Test]
        public void Reported_TileNotInTheReport_IsBlocked_AndNothingChanges()
        {
            MatchBoard board = LooseBoard(2);
            board.ReportFreeTiles(new[] { 0, 1 });

            Assert.IsTrue(board.UsesReportedFreeSet);
            Assert.IsFalse(board.IsFree(2));
            Assert.AreEqual(MatchOutcome.Blocked, board.TapTile(2, 1).Outcome);
            Assert.AreEqual(0, board.Tray.Count);
            Assert.AreEqual(6, board.RemainingOnBoard);
        }

        [Test]
        public void Reported_FreeTiles_PlayThroughTripleAndWin()
        {
            MatchBoard board = LooseBoard(1);
            board.ReportFreeTiles(new[] { 0, 1, 2 });

            board.TapTile(0, 1);
            board.TapTile(1, 1.1);
            MatchStep step = board.TapTile(2, 1.2);

            Assert.AreEqual(MatchOutcome.Won, step.Outcome);
            Assert.AreEqual(100, board.Score);
        }

        [Test]
        public void Reported_NewReportReplacesTheOldOne_AndUnknownIdsAreIgnored()
        {
            MatchBoard board = LooseBoard(2);
            board.ReportFreeTiles(new[] { 0, 99 });
            Assert.IsFalse(board.IsFree(99));
            Assert.IsTrue(board.IsFree(0));

            board.ReportFreeTiles(new[] { 3 });

            Assert.IsFalse(board.IsFree(0));
            Assert.IsTrue(board.IsFree(3));
            CollectionAssert.AreEqual(new[] { 3 }, board.FreeTileIds());
        }

        [Test]
        public void Reported_NotCalled_GridRuleStillDecides()
        {
            MatchBoard board = StackedBoard();

            Assert.IsFalse(board.UsesReportedFreeSet);
            Assert.IsFalse(board.IsFree(2));
        }

        [Test]
        public void Reported_TilesTakenEarlierStayGone_WhenTheReportStillNamesThem()
        {
            MatchBoard board = LooseBoard(2);
            board.ReportFreeTiles(new[] { 0, 1, 2, 3 });
            board.TapTile(0, 1);

            board.ReportFreeTiles(new[] { 0, 1, 2, 3 });   // the view lags by a frame and still lists tile 0

            Assert.IsFalse(board.IsFree(0));
            Assert.AreEqual(MatchOutcome.Ignored, board.TapTile(0, 2).Outcome);
        }
        // ---- Replay ----

        [Test]
        public void Replay_OfIntendedSolution_IsWon()
        {
            MatchLevelSpec spec = MatchStages.All[1];
            MatchLevel level = MatchLevelGenerator.Generate(spec, 5);
            var taps = new List<MatchTap>();
            double now = 0;
            foreach (int id in level.IntendedOrder)
            {
                now += 0.5;
                taps.Add(new MatchTap(now, id));
            }

            MatchBoard board = MatchReplay.Verify(spec, 5, Rules(), taps);

            Assert.AreEqual(MatchState.Won, board.State);
            Assert.Greater(board.Score, 0);
        }

        [Test]
        public void Replay_OfTamperedLog_IsRejectedOrNotWon()
        {
            MatchLevelSpec spec = MatchStages.All[1];
            MatchLevel level = MatchLevelGenerator.Generate(spec, 5);
            var taps = new List<MatchTap>();
            double now = 0;
            foreach (int id in level.IntendedOrder.Skip(1))   // first tap dropped
            {
                now += 0.5;
                taps.Add(new MatchTap(now, id));
            }

            try
            {
                MatchBoard board = MatchReplay.Verify(spec, 5, Rules(), taps);
                Assert.AreNotEqual(MatchState.Won, board.State);
            }
            catch (ArgumentException)
            {
                // Rejected as an illegal run. That is the expected outcome for some tampered logs.
            }
        }

        // ---- Difficulty checks (simulator) ----

        [Test]
        public void Greedy_WinsAtLeastAsOftenAsUniform_OnEveryStage()
        {
            foreach (MatchLevelSpec spec in MatchStages.All)
            {
                double uniform = MatchSimulator.WinRate(spec, Rules(), MatchPolicies.Uniform, 200);
                double greedy = MatchSimulator.WinRate(spec, Rules(), MatchPolicies.Greedy, 200);

                TestContext.Out.WriteLine($"stage symbols={spec.SymbolCount} triples={spec.TripleCount} " +
                                          $"grid={spec.GridWidth}x{spec.GridHeight}: uniform {uniform:P0}, greedy {greedy:P0}");
                Assert.GreaterOrEqual(greedy, uniform, $"symbols {spec.SymbolCount}");
            }
        }

        // Target bands per stage (docs/DECISIONS.md, D7, re-measured in D15 after every level got every symbol). Measured on seeds 1..200, then widened by 8 points each way
        // (about 2.3 standard errors at 200 seeds). Order: greedy min, greedy max, uniform min, uniform max.
        // Change a band only with a DECISIONS entry. A stage-size change that breaks a band must update both.
        private static readonly double[][] PacingBands =
        {
            new[] { 0.92, 1.00, 0.92, 1.00 },
            new[] { 0.89, 1.00, 0.46, 0.62 },
            new[] { 0.86, 1.00, 0.00, 0.15 },
            new[] { 0.77, 0.93, 0.00, 0.08 },
            new[] { 0.68, 0.84, 0.00, 0.08 }
        };

        [Test]
        public void Pacing_EveryStage_StaysInsideItsTargetBand()
        {
            Assert.AreEqual(PacingBands.Length, MatchStages.All.Count, "one band per stage");
            for (int i = 0; i < MatchStages.All.Count; i++)
            {
                double[] band = PacingBands[i];
                double greedy = MatchSimulator.WinRate(MatchStages.All[i], Rules(), MatchPolicies.Greedy, 200);
                double uniform = MatchSimulator.WinRate(MatchStages.All[i], Rules(), MatchPolicies.Uniform, 200);

                Assert.That(greedy, Is.InRange(band[0], band[1]), $"stage {i + 1} greedy");
                Assert.That(uniform, Is.InRange(band[2], band[3]), $"stage {i + 1} uniform");
            }
        }

        [Test]
        public void Pacing_GreedyWinRate_NeverRisesFromOneStageToTheNext()
        {
            double previous = 1.0;
            for (int i = 0; i < MatchStages.All.Count; i++)
            {
                double greedy = MatchSimulator.WinRate(MatchStages.All[i], Rules(), MatchPolicies.Greedy, 200);
                Assert.LessOrEqual(greedy, previous + 1e-9, $"stage {i + 1} is easier than stage {i}");
                previous = greedy;
            }
        }
    }
}


