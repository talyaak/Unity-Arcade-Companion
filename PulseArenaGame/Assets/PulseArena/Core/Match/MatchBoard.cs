using System;
using System.Collections.Generic;

namespace PulseArena.Core
{
    public enum MatchState
    {
        Playing,
        Won,
        Lost
    }

    public enum MatchOutcome
    {
        /// Unknown tile, a tile already in the tray, or the level is over. Nothing changed.
        Ignored,

        /// Covered by a tile on top of it in the same cell. Nothing changed.
        Blocked,

        /// The tile moved into the tray and no triple formed.
        Moved,

        /// The tile completed a triple, which left the tray. The level continues.
        Cleared,

        /// The last tile left the board and the tray is empty.
        Won,

        /// The tray reached capacity without a triple.
        Lost
    }

    public enum MatchEventKind
    {
        Moved,
        Cleared,
        Won,
        Lost
    }

    /// One thing that happened during a tap, for the view to play back. Core decides; the view never decides.
    public readonly struct MatchEvent
    {
        public MatchEvent(MatchEventKind kind, int tileId, int symbol, int points)
        {
            Kind = kind;
            TileId = tileId;
            Symbol = symbol;
            Points = points;
        }

        public MatchEventKind Kind { get; }
        public int TileId { get; }
        public int Symbol { get; }
        public int Points { get; }
    }

    public sealed class MatchStep
    {
        public MatchStep(MatchOutcome outcome, IReadOnlyList<MatchEvent> events)
        {
            Outcome = outcome;
            Events = events ?? new MatchEvent[0];
        }

        public MatchOutcome Outcome { get; }
        public IReadOnlyList<MatchEvent> Events { get; }
    }

    /// One level in progress: the tiles still on the board, the tray, the score and the state.
    /// A tile is free only if no remaining tile sits on top of it in the same cell. Views ask IsFree or TapTile
    /// and never re-derive blocking, so what is drawn and what is allowed cannot drift apart.
    public sealed class MatchBoard
    {
        public const int MatchSize = 3;

        private readonly MatchRules _rules;
        private readonly Dictionary<int, MatchTile> _onBoard = new Dictionary<int, MatchTile>();
        private readonly List<MatchTile> _tray = new List<MatchTile>();
        private double _lastTime = double.NegativeInfinity;
        private double _lastClearTime = double.NegativeInfinity;
        private int _combo;

        public MatchBoard(IEnumerable<MatchTile> tiles, MatchRules rules)
        {
            if (tiles == null)
            {
                throw new ArgumentNullException(nameof(tiles));
            }

            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            List<string> ruleErrors = rules.Validate();
            if (ruleErrors.Count > 0)
            {
                throw new ArgumentException("Invalid match rules: " + string.Join(" ", ruleErrors), nameof(rules));
            }

            var symbolCounts = new Dictionary<int, int>();
            foreach (MatchTile tile in tiles)
            {
                if (tile == null)
                {
                    throw new ArgumentException("Tiles cannot be null.", nameof(tiles));
                }

                if (_onBoard.ContainsKey(tile.Id))
                {
                    throw new ArgumentException($"Duplicate tile id {tile.Id}.", nameof(tiles));
                }

                _onBoard.Add(tile.Id, tile);
                symbolCounts.TryGetValue(tile.Symbol, out int count);
                symbolCounts[tile.Symbol] = count + 1;
            }

            foreach (KeyValuePair<int, int> pair in symbolCounts)
            {
                if (pair.Value % MatchSize != 0)
                {
                    throw new ArgumentException(
                        $"Symbol {pair.Key} has {pair.Value} tiles; each symbol needs a multiple of {MatchSize}.", nameof(tiles));
                }
            }

            TotalTiles = _onBoard.Count;
        }

        public MatchState State { get; private set; } = MatchState.Playing;
        public int Score { get; private set; }
        public int Combo => _combo;
        public int BestCombo { get; private set; }
        public int TilesCleared { get; private set; }
        public int TotalTiles { get; }
        public int RemainingOnBoard => _onBoard.Count;
        public IReadOnlyList<MatchTile> Tray => _tray;

        public bool TryGetOnBoard(int tileId, out MatchTile tile)
        {
            return _onBoard.TryGetValue(tileId, out tile);
        }

        public int TrayCount(int symbol)
        {
            int count = 0;
            foreach (MatchTile tile in _tray)
            {
                if (tile.Symbol == symbol)
                {
                    count++;
                }
            }

            return count;
        }

        public bool IsFree(int tileId)
        {
            return _onBoard.TryGetValue(tileId, out MatchTile tile) && IsFree(tile);
        }

        /// Ids of free tiles on the board, ascending, so the same state always gives the same list.
        public IReadOnlyList<int> FreeTileIds()
        {
            var ids = new List<int>();
            foreach (MatchTile tile in _onBoard.Values)
            {
                if (IsFree(tile))
                {
                    ids.Add(tile.Id);
                }
            }

            ids.Sort();
            return ids;
        }

        /// Taps a tile at a given play time. Times must never go backwards, so a replay log can be checked.
        /// Order inside one tap: move into the tray, resolve a triple, then check the tray for overflow.
        /// A tile that completes a triple in the last slot clears instead of losing.
        public MatchStep TapTile(int tileId, double now)
        {
            if (!(now >= _lastTime))
            {
                throw new ArgumentOutOfRangeException(nameof(now), "Time must be a number that never goes backwards.");
            }

            _lastTime = now;

            if (State != MatchState.Playing || !_onBoard.TryGetValue(tileId, out MatchTile tile))
            {
                return new MatchStep(MatchOutcome.Ignored, null);
            }

            if (!IsFree(tile))
            {
                return new MatchStep(MatchOutcome.Blocked, null);
            }

            _onBoard.Remove(tileId);
            InsertIntoTray(tile);

            var events = new List<MatchEvent> { new MatchEvent(MatchEventKind.Moved, tile.Id, tile.Symbol, 0) };
            MatchOutcome outcome = MatchOutcome.Moved;

            if (TrayCount(tile.Symbol) == MatchSize)
            {
                int points = ResolveClear(tile.Symbol, now);
                events.Add(new MatchEvent(MatchEventKind.Cleared, tile.Id, tile.Symbol, points));
                outcome = MatchOutcome.Cleared;
            }

            if (_onBoard.Count == 0 && _tray.Count == 0)
            {
                State = MatchState.Won;
                events.Add(new MatchEvent(MatchEventKind.Won, tile.Id, tile.Symbol, 0));
                outcome = MatchOutcome.Won;
            }
            else if (_tray.Count >= _rules.TrayCapacity)
            {
                State = MatchState.Lost;
                events.Add(new MatchEvent(MatchEventKind.Lost, tile.Id, tile.Symbol, 0));
                outcome = MatchOutcome.Lost;
            }

            return new MatchStep(outcome, events);
        }

        private bool IsFree(MatchTile tile)
        {
            foreach (MatchTile other in _onBoard.Values)
            {
                if (other.X == tile.X && other.Y == tile.Y && other.Layer > tile.Layer)
                {
                    return false;
                }
            }

            return true;
        }

        // Groups equal symbols: a new tile goes after the last tile of its symbol, or at the end.
        private void InsertIntoTray(MatchTile tile)
        {
            int last = -1;
            for (int i = 0; i < _tray.Count; i++)
            {
                if (_tray[i].Symbol == tile.Symbol)
                {
                    last = i;
                }
            }

            if (last < 0)
            {
                _tray.Add(tile);
            }
            else
            {
                _tray.Insert(last + 1, tile);
            }
        }

        private int ResolveClear(int symbol, double now)
        {
            _tray.RemoveAll(t => t.Symbol == symbol);
            TilesCleared += MatchSize;

            _combo = now - _lastClearTime <= _rules.ComboWindowSeconds ? _combo + 1 : 1;
            _lastClearTime = now;
            if (_combo > BestCombo)
            {
                BestCombo = _combo;
            }

            int points = _rules.ClearPoints * Math.Min(_combo, _rules.MaxComboMultiplier);
            Score += points;
            return points;
        }
    }
}
