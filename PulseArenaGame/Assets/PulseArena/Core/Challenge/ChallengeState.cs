namespace PulseArena.Core
{
    public enum ChallengeState
    {
        Idle,
        Countdown,
        Playing,
        Results
    }

    public enum ChallengeCommand
    {
        Begin,
        CountdownFinished,
        TimeUp,
        Quit
    }

    /// The whole transition table in one pure function. Invalid commands return false and leave the
    /// state unchanged, so UI code cannot push a run into an impossible state.
    public static class ChallengeStateMachine
    {
        public static bool TryGetNext(ChallengeState current, ChallengeCommand command, out ChallengeState next)
        {
            switch (current)
            {
                case ChallengeState.Idle:
                    if (command == ChallengeCommand.Begin) { next = ChallengeState.Countdown; return true; }
                    break;

                case ChallengeState.Countdown:
                    if (command == ChallengeCommand.CountdownFinished) { next = ChallengeState.Playing; return true; }
                    if (command == ChallengeCommand.Quit) { next = ChallengeState.Idle; return true; }
                    break;

                case ChallengeState.Playing:
                    if (command == ChallengeCommand.TimeUp) { next = ChallengeState.Results; return true; }
                    if (command == ChallengeCommand.Quit) { next = ChallengeState.Idle; return true; }
                    break;

                case ChallengeState.Results:
                    if (command == ChallengeCommand.Begin) { next = ChallengeState.Countdown; return true; }
                    if (command == ChallengeCommand.Quit) { next = ChallengeState.Idle; return true; }
                    break;
            }

            next = current;
            return false;
        }
    }
}
