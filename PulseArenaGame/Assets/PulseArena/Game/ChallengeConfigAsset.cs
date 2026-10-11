using PulseArena.Core;
using UnityEngine;

namespace PulseArena.Game
{
    /// Designer-editable tuning. Holds no logic: ToConfig() copies the values into the engine-free ChallengeConfig,
    /// which validates them when a session is created. Float fields are rounded through ConfigValue.FromFloat.
    [CreateAssetMenu(fileName = "ChallengeConfig", menuName = "Pulse Arena/Challenge Config")]
    public sealed class ChallengeConfigAsset : ScriptableObject
    {
        [SerializeField] private string _version = "dev-0";
        [SerializeField] private float _countdownSeconds = 3f;
        [SerializeField] private float _durationSeconds = 30f;
        [SerializeField] private int _laneCount = 4;
        [SerializeField] private float _minSpawnInterval = 0.6f;
        [SerializeField] private float _maxSpawnInterval = 1.1f;
        [SerializeField] private float _targetLifetime = 1.2f;

        [Header("Scoring")]
        [SerializeField] private int _basePoints = 100;
        [SerializeField] private int _comboStep = 5;
        [SerializeField] private int _maxMultiplier = 4;
        [SerializeField] private int _missPenalty = 50;

        public ChallengeConfig ToConfig()
        {
            return new ChallengeConfig
            {
                Version = _version,
                CountdownSeconds = ConfigValue.FromFloat(_countdownSeconds),
                DurationSeconds = ConfigValue.FromFloat(_durationSeconds),
                LaneCount = _laneCount,
                MinSpawnInterval = ConfigValue.FromFloat(_minSpawnInterval),
                MaxSpawnInterval = ConfigValue.FromFloat(_maxSpawnInterval),
                TargetLifetime = ConfigValue.FromFloat(_targetLifetime),
                Scoring = new ScoringRules
                {
                    BasePoints = _basePoints,
                    ComboStep = _comboStep,
                    MaxMultiplier = _maxMultiplier,
                    MissPenalty = _missPenalty
                }
            };
        }
    }
}
