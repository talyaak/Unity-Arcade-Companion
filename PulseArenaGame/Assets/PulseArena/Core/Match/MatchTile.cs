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

        public int Id { get; }
        public int Symbol { get; }
        public int X { get; }
        public int Y { get; }
        public int Layer { get; }
    }
}
