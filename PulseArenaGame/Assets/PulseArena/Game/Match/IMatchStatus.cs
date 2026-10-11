using PulseArena.Core;

namespace PulseArena.Game
{
    /// What the match HUD reads from a mode's controller: the board, the stage, the tray size, Reduce Motion, and the three
    /// buttons. The grid game (MatchController) and physics mode (PhysicsMatchController, D14) both implement it, so one HUD
    /// serves both modes with the same look.
    public interface IMatchStatus
    {
        MatchBoard Board { get; }
        int StageNumber { get; }
        int TrayCapacity { get; }
        bool ReduceMotion { get; }
        void Retry();
        void NextStage();
        void ToggleReduceMotion();
    }
}
