namespace PulseArena.Game
{
    /// The named cues from the juice standard (GAME_DESIGN, Audio). Each one has a hook in MatchController.
    public enum AudioCue
    {
        Tap,
        Move,
        Clear,
        Blocked,
        Win,
        Lose
    }

    /// Audio hooks. They are silent until a generated or licensed clip is chosen and logged in the asset ledger.
    public static class AudioCues
    {
        /// Placeholder: plays nothing. The call sites stay so clips can be added without touching the game logic.
        public static void Play(AudioCue cue)
        {
        }
    }
}
