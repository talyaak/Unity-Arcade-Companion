using PulseArena.Core;
using UnityEngine;

namespace PulseArena.Game
{
    /// Immediate-mode HUD and lane input for ChallengeController. Draws in OnGUI, which also receives touch on mobile.
    /// Holds no game rules: decisions go to the controller and session. The only logic here is layout and text.
    [RequireComponent(typeof(ChallengeController))]
    public sealed class ChallengeHud : MonoBehaviour
    {
        private const float Margin = 16f;
        private const float ButtonHeight = 64f;

        private ChallengeController _controller;

        // Replay check is computed once per finished run, not every frame.
        private ulong _checkedSeed;
        private bool _hasCheck;
        private string _checkText = string.Empty;

        private void Awake()
        {
            _controller = GetComponent<ChallengeController>();
        }

        private void OnGUI()
        {
            ChallengeSession session = _controller.Session;
            if (session == null)
            {
                return;
            }

            float width = Screen.width;
            float height = Screen.height;

            DrawStatus(session, width);
            DrawLanes(session, width, height);

            switch (session.State)
            {
                case ChallengeState.Idle:
                    if (GUI.Button(CenterRect(width, height, 240f, ButtonHeight), "Start"))
                    {
                        StartRun();
                    }
                    break;

                case ChallengeState.Countdown:
                case ChallengeState.Playing:
                    if (GUI.Button(new Rect(width - 120f - Margin, Margin + 56f, 120f, 48f), "Quit"))
                    {
                        _controller.Quit();
                    }
                    break;

                case ChallengeState.Results:
                    DrawResults(session, width, height);
                    break;
            }
        }

        private void DrawStatus(ChallengeSession session, float width)
        {
            string text;
            switch (session.State)
            {
                case ChallengeState.Countdown:
                    text = $"Starting in {System.Math.Ceiling(session.CountdownRemaining)}";
                    break;
                case ChallengeState.Playing:
                    int multiplier = _controller.RunConfig.Scoring.MultiplierFor(session.Scoreboard.Combo);
                    text = $"Time {session.PlayRemaining:0.0}s   Score {session.Scoreboard.Score}   " +
                           $"Combo {session.Scoreboard.Combo}   x{multiplier}";
                    if (_controller.IsPaused)
                    {
                        text += "   (paused)";
                    }
                    break;
                case ChallengeState.Results:
                    text = "Run finished";
                    break;
                default:
                    text = "Pulse Arena";
                    break;
            }

            GUI.Label(new Rect(Margin, Margin, width - 2f * Margin, 40f), text);
        }

        private void DrawLanes(ChallengeSession session, float width, float height)
        {
            int laneCount = _controller.RunConfig.LaneCount;
            float top = height * 0.45f;
            float laneHeight = height * 0.45f;
            float laneWidth = width / laneCount;

            for (int lane = 0; lane < laneCount; lane++)
            {
                var rect = new Rect(lane * laneWidth + 6f, top, laneWidth - 12f, laneHeight);

                if (session.HasLiveTarget(lane))
                {
                    GUI.color = new Color(0.2f, 0.9f, 1f, 1f);
                    GUI.DrawTexture(new Rect(rect.x + 8f, rect.y + 8f, rect.width - 16f, rect.height - 16f), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }

                // Button handles the tap. Taps outside Playing are ignored by the session, so no state check is needed here.
                if (GUI.Button(rect, string.Empty))
                {
                    _controller.Tap(lane);
                }
            }
        }

        private void DrawResults(ChallengeSession session, float width, float height)
        {
            ChallengeResult result = session.Result;
            if (result == null)
            {
                return;
            }

            if (!_hasCheck || _checkedSeed != _controller.LastSeed)
            {
                _checkText = ReplayCheck(result);
                _checkedSeed = _controller.LastSeed;
                _hasCheck = true;
            }

            string summary =
                $"Score {result.Score}\n" +
                $"Hits {result.Hits}   Misses {result.Misses}\n" +
                $"Best combo {result.BestCombo}   Accuracy {result.Accuracy:P0}\n" +
                $"Seed {result.Seed}   Config {result.ConfigVersion}\n" +
                _checkText;

            GUI.Label(new Rect(Margin, height * 0.12f, width - 2f * Margin, 140f), summary);

            if (GUI.Button(CenterRect(width, height, 240f, ButtonHeight), "Replay"))
            {
                StartRun();
            }

            if (GUI.Button(new Rect(Margin, height - ButtonHeight - Margin, 160f, 48f), "Menu"))
            {
                _controller.Quit();
            }
        }

        // Re-plays the logged taps through ReplayVerifier and compares with the live score.
        private string ReplayCheck(ChallengeResult live)
        {
            try
            {
                ChallengeResult replay = ReplayVerifier.Verify(_controller.RunConfig, _controller.LastSeed, _controller.TapLog);
                return replay.Score == live.Score
                    ? "Replay check: OK"
                    : $"Replay check: MISMATCH (live {live.Score}, replay {replay.Score})";
            }
            catch (System.ArgumentException)
            {
                return "Replay check: invalid tap log";
            }
        }

        private void StartRun()
        {
            _hasCheck = false;
            _controller.StartRun((ulong)System.DateTime.UtcNow.Ticks);
        }

        private static Rect CenterRect(float width, float height, float buttonWidth, float buttonHeight)
        {
            return new Rect((width - buttonWidth) / 2f, (height - buttonHeight) / 2f, buttonWidth, buttonHeight);
        }
    }
}
