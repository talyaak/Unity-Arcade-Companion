namespace PulseArena.Core
{
    /// One tile. (X, Y) is its grid cell. Layer orders tiles stacked in one cell: a higher layer sits on top.
    public sealed class MatchTile
    {
        public MatchTile(int id, int symbol, int x, int y, int layer)
        {
            Id = id;
            Symbol = symbol;
            X = x;
            Y = y;
            Layer = layer;
        }

        /// A tile with no grid position, for physics mode where the view reports which tiles are free.
        public static MatchTile Loose(int id, int symbol)
        {
            return new MatchTile(id, symbol, 0, 0, id);
        }

        public int Id { get; }
        public int Symbol { get; }
        public int X { get; }
        public int Y { get; }
        public int Layer { get; }
    }
}

