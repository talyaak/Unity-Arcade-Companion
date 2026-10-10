using UnityEngine;

namespace PulseArena.Game
{
    /// Particles for clears and wins, all from one ParticleSystem. Its Max Particles of 40 is the hard cap on live particles
    /// (juice standard; D9). A clear bursts at most 12 particles per tile, and a win releases confetti in waves of 20, 60 in
    /// all. Bursts can wait for the hit-stop, so the pop and its particles start together.
    public sealed class MatchEffects : MonoBehaviour
    {
        public const int MaxLive = 40;
        public const int BurstPerTile = 12;
        public const int ConfettiWaveSize = 20;
        public const int ConfettiTotal = 60;

        private const float ParticleLifetime = 0.6f;
        private const float BurstSpeed = 2.2f;
        private const int PendingSlots = 8;

        private static readonly Color[] Colors =
        {
            new Color(0.95f, 0.45f, 0.35f, 1f),
            new Color(0.25f, 0.75f, 0.72f, 1f),
            new Color(0.97f, 0.80f, 0.35f, 1f),
            new Color(0.62f, 0.45f, 0.92f, 1f),
            new Color(0.55f, 0.85f, 0.45f, 1f),
            new Color(0.45f, 0.68f, 0.95f, 1f)
        };

        private readonly Vector3[] _pendingAt = new Vector3[PendingSlots];
        private readonly float[] _pendingIn = new float[PendingSlots];
        private int _pendingCount;
        private ParticleSystem _system;
        private Material _material;

        /// Particles alive right now. Read by the verification, not by the game.
        public int LiveCount => _system == null ? 0 : _system.particleCount;

        /// Highest live count seen in any frame. Read by the verification.
        public int LiveMax { get; private set; }

        /// Particles released by bursts so far. Read by the verification.
        public int BurstParticles { get; private set; }

        /// Number of bursts released so far (one per cleared tile). Read by the verification.
        public int BurstCount { get; private set; }

        /// Confetti particles released so far in this stage. Read by the verification.
        public int ConfettiEmitted { get; private set; }

        /// Confetti waves released so far in this stage, and the game time of the first and last. Read by the verification.
        public int ConfettiWaves { get; private set; }
        public float FirstWaveAt { get; private set; } = -1f;
        public float LastWaveAt { get; private set; } = -1f;

        private void Awake()
        {
            _system = gameObject.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = _system.main;
            main.loop = false;
            main.playOnAwake = false;
            main.maxParticles = MaxLive;
            main.startLifetime = ParticleLifetime;
            main.startSpeed = 0f;                   // each release sets its own velocity
            main.startSize = 0.12f;
            main.gravityModifier = 1.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = _system.emission;
            emission.rateOverTime = 0f;             // released by script only
            ParticleSystem.ShapeModule shape = _system.shape;
            shape.enabled = false;

            Shader shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            }

            _material = new Material(shader);
            ParticleSystemRenderer particleRenderer = GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.sharedMaterial = _material;

            _system.Play();
        }

        private void OnDestroy()
        {
            Destroy(_material);
        }

        /// A burst at a cleared tile, at most BurstPerTile particles. It starts after the delay (the hit-stop).
        public void Burst(Vector3 position, float delaySeconds)
        {
            if (delaySeconds <= 0f || _pendingCount >= PendingSlots)
            {
                EmitBurst(position);
                return;
            }

            _pendingAt[_pendingCount] = position;
            _pendingIn[_pendingCount] = delaySeconds;
            _pendingCount++;
        }

        /// One wave of confetti over the board. Returns how many were released; 0 once the stage's total is spent.
        public int ReleaseConfetti(float halfWidth, float halfDepth, float height)
        {
            int count = System.Math.Min(ConfettiWaveSize, ConfettiTotal - ConfettiEmitted);
            if (count <= 0)
            {
                return 0;
            }

            var emit = new ParticleSystem.EmitParams();
            for (int i = 0; i < count; i++)
            {
                emit.position = new Vector3(
                    UnityEngine.Random.Range(-halfWidth, halfWidth), height, UnityEngine.Random.Range(-halfDepth, halfDepth));
                emit.velocity = new Vector3(
                    UnityEngine.Random.Range(-0.4f, 0.4f), UnityEngine.Random.Range(0.2f, 0.8f), UnityEngine.Random.Range(-0.4f, 0.4f));
                emit.startColor = Colors[UnityEngine.Random.Range(0, Colors.Length)];
                _system.Emit(emit, 1);
            }

            ConfettiEmitted += count;
            ConfettiWaves++;
            if (FirstWaveAt < 0f)
            {
                FirstWaveAt = Time.time;
            }

            LastWaveAt = Time.time;
            return count;
        }

        private void Update()
        {
            int live = LiveCount;
            if (live > LiveMax)
            {
                LiveMax = live;
            }

            for (int i = _pendingCount - 1; i >= 0; i--)
            {
                _pendingIn[i] -= Time.deltaTime;
                if (_pendingIn[i] > 0f)
                {
                    continue;
                }

                EmitBurst(_pendingAt[i]);
                _pendingCount--;
                _pendingAt[i] = _pendingAt[_pendingCount];
                _pendingIn[i] = _pendingIn[_pendingCount];
            }
        }

        private void EmitBurst(Vector3 position)
        {
            var emit = new ParticleSystem.EmitParams();
            for (int i = 0; i < BurstPerTile; i++)
            {
                emit.position = position;
                emit.velocity = new Vector3(
                    UnityEngine.Random.Range(-0.5f, 0.5f) * BurstSpeed,
                    UnityEngine.Random.Range(0.5f, 1f) * BurstSpeed,
                    UnityEngine.Random.Range(-0.5f, 0.5f) * BurstSpeed);
                emit.startColor = Colors[UnityEngine.Random.Range(0, Colors.Length)];
                _system.Emit(emit, 1);
            }

            BurstParticles += BurstPerTile;
            BurstCount++;
        }
    }
}
