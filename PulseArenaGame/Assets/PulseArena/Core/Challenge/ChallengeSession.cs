using System;
using System.Collections.Generic;

namespace PulseArena.Core
{
    public enum TapKind
    {
        /// Tap arrived outside Playing, or for an invalid lane. Nothing changed.
        Ignored,

        /// Tap on a lane with no live target. No penalty: a deliberate design choice (see docs).
        Empty,

        Hit
    }

    public readonly struct TapResult
    {
        public TapResult(TapKind kind, int points, int multiplier)
        {
            Kind = kind;
            Points = points;
            Multiplier = multiplier;
        }

        public TapKind Kind { get; }
        public int Points { get; }
        public int Multiplier { get; }

        public static TapResult Ignored => new TapResult(TapKind.Ignored, 0, 0);
        public static TapResult Empty => new TapResult(TapKind.Empty, 0, 0);
    }

    /// Owns one challenge run: the state machine, the target timeline and the scoring.
    /// It has no UnityEngine dependency. A MonoBehaviour drives it with Advance(deltaTime) and forwards taps.
    public sealed class ChallengeSession
    {
        private readonly ChallengeConfig _config;
        private readonly int[] _laneTarget;

        private List<TargetSpec> _schedule = new List<TargetSpec>();
        private int _nextSpawnIndex;
        private double _countdownElapsed;
        private double _playElapsed;
        private ulong _seed;

        public ChallengeSession(ChallengeConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            List<string> errors = config.Validate();
            if (errors.Count > 0)
            {
                throw new ArgumentException("Invalid challenge config: " + string.Join(" ", errors), nameof(config));
            }

            _config = config;
            _laneTarget = new int[config.LaneCount];
            Scoreboard = new ScoreTracker(config.Scoring);
        }

        public ChallengeState State { get; private set; } = ChallengeState.Idle;

        /// Live score, combo and counters for the current run.
        public ScoreTracker Scoreboard { get; private set; }

        /// Set when the run reaches Results. Null otherwise.
        public ChallengeResult Result { get; private set; }

        public IReadOnlyList<TargetSpec> Schedule => _schedule;

        public double CountdownRemaining => Math.Max(0, _config.CountdownSeconds - _countdownElapsed);

        public double PlayElapsed => _playElapsed;

        public double PlayRemaining => Math.Max(0, _config.DurationSeconds - _playElapsed);

        public bool Begin(ulong seed)
        {
            if (!ChallengeStateMachine.TryGetNext(State, ChallengeCommand.Begin, out ChallengeState next))
            {
                return false;
            }

            _seed = seed;
            _schedule = ChallengeSchedule.Generate(_config, new SeededRandom(seed));
            _nextSpawnIndex = 0;
            _countdownElapsed = 0;
            _playElapsed = 0;
            Scoreboard = new ScoreTracker(_config.Scoring);
            Result = null;
            for (int lane = 0; lane < _laneTarget.Length; lane++)
            {
                _laneTarget[lane] = -1;
            }

            State = next;
            return true;
        }

        public bool Quit()
        {
            if (!ChallengeStateMachine.TryGetNext(State, ChallengeCommand.Quit, out ChallengeState next))
            {
                return false;
            }

            State = next;
            return true;
        }

        /// Advances the run by deltaSeconds. Countdown overflow carries into play time, so timing stays exact
        /// across the countdown boundary. Call this every frame from Update.
        public void Advance(double deltaSeconds)
        {
            if (!(deltaSeconds >= 0))
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Must be a non-negative number.");
            }

            if (State == ChallengeState.Countdown)
            {
                _countdownElapsed += deltaSeconds;
                if (_countdownElapsed < _config.CountdownSeconds)
                {
                    return;
                }

                double overflow = _countdownElapsed - _config.CountdownSeconds;
                TryTransition(ChallengeCommand.CountdownFinished);
                AdvancePlay(overflow);
            }
            else if (State == ChallengeState.Playing)
            {
                AdvancePlay(deltaSeconds);
            }
        }

        public TapResult TapLane(int lane)
        {
            if (State != ChallengeState.Playing || lane < 0 || lane >= _laneTarget.Length)
            {
                return TapResult.Ignored;
            }

            if (_laneTarget[lane] < 0)
            {
                return TapResult.Empty;
            }

            _laneTarget[lane] = -1;
            HitOutcome hit = Scoreboard.RegisterHit();
            return new TapResult(TapKind.Hit, hit.Points, hit.Multiplier);
        }

        public bool HasLiveTarget(int lane)
        {
            return lane >= 0 && lane < _laneTarget.Length && _laneTarget[lane] >= 0;
        }

        private void AdvancePlay(double delta)
        {
            _playElapsed = Math.Min(_playElapsed + delta, _config.DurationSeconds);
            ProcessEventsUpTo(_playElapsed);

            if (_playElapsed >= _config.DurationSeconds)
            {
                TryTransition(ChallengeCommand.TimeUp);
                Result = new ChallengeResult(
                    Scoreboard.Score,
                    Scoreboard.Hits,
                    Scoreboard.Misses,
                    Scoreboard.BestCombo,
                    _seed,
                    _config.Version);
            }
        }

        /// Processes spawns and expiries in time order up to `time`. On a tie, expiry runs first so a lane
        /// freed at time t can accept a spawn at time t.
        private void ProcessEventsUpTo(double time)
        {
            while (true)
            {
                int expiringLane = -1;
                double expiryTime = double.PositiveInfinity;
                for (int lane = 0; lane < _laneTarget.Length; lane++)
                {
                    int index = _laneTarget[lane];
                    if (index >= 0 && _schedule[index].ExpiresAt < expiryTime)
                    {
                        expiringLane = lane;
                        expiryTime = _schedule[index].ExpiresAt;
                    }
                }

                double spawnTime = _nextSpawnIndex < _schedule.Count
                    ? _schedule[_nextSpawnIndex].SpawnTime
                    : double.PositiveInfinity;

                if (expiringLane >= 0 && expiryTime <= time && expiryTime <= spawnTime)
                {
                    _laneTarget[expiringLane] = -1;
                    Scoreboard.RegisterMiss();
                }
                else if (spawnTime <= time)
                {
                    SpawnNext();
                }
                else
                {
                    break;
                }
            }
        }

        private void SpawnNext()
        {
            TargetSpec spec = _schedule[_nextSpawnIndex];
            if (_laneTarget[spec.Lane] >= 0)
            {
                // The schedule guarantees a free lane, so this indicates a bug in the schedule or ordering.
                throw new InvalidOperationException($"Lane {spec.Lane} is still occupied at spawn time {spec.SpawnTime}.");
            }

            _laneTarget[spec.Lane] = _nextSpawnIndex;
            _nextSpawnIndex++;
        }

        private void TryTransition(ChallengeCommand command)
        {
            if (ChallengeStateMachine.TryGetNext(State, command, out ChallengeState next))
            {
                State = next;
            }
        }
    }
}
