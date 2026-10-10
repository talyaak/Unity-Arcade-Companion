using UnityEngine;

namespace PulseArena.Game
{
    /// Visual for one tile. MatchController sets its target and material from Core's answers. This script only animates:
    /// it eases toward the target, shakes on a blocked tap, and shrinks away when its triple clears.
    public sealed class TileView : MonoBehaviour
    {
        private const float ShakeSeconds = 0.3f;
        private const float ShakeAmplitude = 0.12f;
        private const float EaseRate = 12f;
        private const float VanishRate = 4f;

        private Vector3 _target;
        private bool _vanishing;
        private float _shakeLeft;

        public int TileId { get; private set; }

        public void Init(int tileId, Vector3 start)
        {
            TileId = tileId;
            _target = start;
            transform.position = start;
        }

        public void SetTarget(Vector3 target)
        {
            _target = target;
        }

        /// Assigns a shared material. Shared materials keep the tile's renderer from creating a copy.
        public void SetMaterial(Material material)
        {
            GetComponent<Renderer>().sharedMaterial = material;
        }

        public void Shake()
        {
            _shakeLeft = ShakeSeconds;
        }

        public void Vanish()
        {
            _vanishing = true;
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            if (_vanishing)
            {
                float size = Mathf.Max(0f, transform.localScale.x - dt * VanishRate);
                transform.localScale = new Vector3(size, size, size);
                if (size <= 0.01f)
                {
                    Destroy(gameObject);
                }

                return;
            }

            Vector3 position = Vector3.Lerp(transform.position, _target, Mathf.Clamp01(dt * EaseRate));
            if (_shakeLeft > 0f)
            {
                _shakeLeft -= dt;
                position.x += Mathf.Sin(_shakeLeft * 60f) * ShakeAmplitude;
            }

            transform.position = position;
        }
    }
}
