using System;
using System.Collections.Generic;

namespace PulseArena.Core
{
    /// The size of a level. Symbol kinds and triples are drawn from the seed; the spec only sets the shape.
    public sealed class MatchLevelSpec
    {
        public MatchLevelSpec(int symbolCount, int tripleCount, int gridWidth, int gridHeight)
        {
            SymbolCount = symbolCount;
            TripleCount = tripleCount;
            GridWidth = gridWidth;
            GridHeight = gridHeight;
        }

        public int SymbolCount { get; }
        public int TripleCount { get; }
        public int GridWidth { get; }
        public int GridHeight { get; }
    }

    public sealed class MatchLevel
    {
        public MatchLevel(IReadOnlyList<MatchTile> tiles, IReadOnlyList<int> intendedOrder)
        {
            Tiles = tiles ?? throw new ArgumentNullException(nameof(tiles));
            IntendedOrder = intendedOrder ?? throw new ArgumentNullException(nameof(intendedOrder));
        }

        public IReadOnlyList<MatchTile> Tiles { get; }

        /// Tile ids in an order that clears the level. Tests use it to prove the level is winnable.
        /// Never shown to players.
        public IReadOnlyList<int> IntendedOrder { get; }
    }

    /// Builds a level that is winnable by construction. Tile i is cleared i-th in triples, and earlier-cleared tiles
    /// sit on top of their cell. Tapping in clear order therefore always frees the next tile.
    public static class MatchLevelGenerator
    {
        public static MatchLevel Generate(MatchLevelSpec spec, ulong seed)
        {
            if (spec == null)
            {
                throw new ArgumentNullException(nameof(spec));
            }

            if (spec.SymbolCount < 1 || spec.TripleCount < 1 || spec.GridWidth < 1 || spec.GridHeight < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(spec), "Symbol, triple and grid counts must be at least 1.");
            }

            var random = new SeededRandom(seed);
            int tileCount = spec.TripleCount * MatchBoard.MatchSize;

            // Ids are shuffled so the id order does not reveal the clear order.
            int[] ids = new int[tileCount];
            for (int i = 0; i < tileCount; i++)
            {
                ids[i] = i;
            }

            for (int i = tileCount - 1; i > 0; i--)
            {
                int j = random.NextInt(i + 1);
                (ids[i], ids[j]) = (ids[j], ids[i]);
            }

            // One symbol per triple, shared by its three tiles, so every symbol count is a multiple of three.
            // When there are enough triples, every symbol appears at least once, so a level never has fewer kinds than
            // its spec says (with three kinds the tray can never fill, so the level could not be lost).
            int[] tripleSymbols = new int[spec.TripleCount];
            for (int triple = 0; triple < spec.TripleCount; triple++)
            {
                tripleSymbols[triple] = triple < spec.SymbolCount ? triple : random.NextInt(spec.SymbolCount);
            }

            for (int i = tripleSymbols.Length - 1; i > 0; i--)
            {
                int j = random.NextInt(i + 1);
                (tripleSymbols[i], tripleSymbols[j]) = (tripleSymbols[j], tripleSymbols[i]);
            }

            var tiles = new List<MatchTile>(tileCount);
            var order = new List<int>(tileCount);
            for (int index = 0; index < tileCount; index++)
            {
                int symbol = tripleSymbols[index / MatchBoard.MatchSize];
                int x = random.NextInt(spec.GridWidth);
                int y = random.NextInt(spec.GridHeight);
                int id = ids[index];
                tiles.Add(new MatchTile(id, symbol, x, y, layer: tileCount - index));
                order.Add(id);
            }

            return new MatchLevel(tiles, order);
        }
    }
}
