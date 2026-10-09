using System.Collections.Generic;
using PulseArena.Core;
using UnityEngine;

namespace PulseArena.Game
{
    /// Thin adapter between Unity and ChallengeSession. It forwards frame time and taps and holds no game rules.
    /// Pausing freezes the run: frames are not advanced and taps are ignored until resume.
    public sealed class ChallengeController : MonoBehaviour
    {
        [SerializeField] private ChallengeConfigAsset _config;

        /// Caps the frame delta fed to the session. Protects against time jumps after a stall.
        [SerializeField] private float _maxFrameSeconds = 0.1f;

        private ChallengeSession _session;
        private readonly List<TapRecord> _tapLog = new List<TapRecord>();
        private bool _pausedByApp;
        private bool _pausedByFocus;

        public ChallengeSession Session => _session;

        /// Accepted taps (Hit or Empty) for the current run, for ReplayVerifier.Verify.
        public IReadOnlyList<TapRecord> TapLog => _tapLog;

        public bool IsPaused => _pausedByApp || _pausedByFocus;

        private void Awake()
        {
            if (_config == null)
            {
                Debug.LogError("ChallengeController needs a ChallengeConfigAsset.", this);
                enabled = false;
                return;
            }

            _session = new ChallengeSession(_config.ToConfig());
        }

        /// Starts a run. The seed is kept in the result so the same run can be replayed and checked.
        public bool StartRun(ulong seed)
        {
            if (_session == null || !_session.Begin(seed))
            {
                return false;
            }

            _tapLog.Clear();
            return true;
        }

        public TapResult Tap(int lane)
        {
            if (_session == null || IsPaused)
            {
                return TapResult.Ignored;
            }

            TapResult result = _session.TapLane(lane);
            if (result.Kind != TapKind.Ignored)
            {
                _tapLog.Add(new TapRecord(_session.PlayElapsed, lane));
            }

            return result;
        }

        private void Update()
        {
            if (_session == null || IsPaused)
            {
                return;
            }

            _session.Advance(FrameTime.Clamp(Time.deltaTime, _maxFrameSeconds));
        }

        // Mobile backgrounding and desktop focus loss both pause the run, so the player cannot lose time off-screen.
        private void OnApplicationPause(bool paused)
        {
            _pausedByApp = paused;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            _pausedByFocus = !hasFocus;
        }
    }
}
