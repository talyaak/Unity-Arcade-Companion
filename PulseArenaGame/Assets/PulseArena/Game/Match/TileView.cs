using UnityEngine;

namespace PulseArena.Game
{
    /// Visual for one tile. MatchController sets its target and material from Core's answers. This script only animates
    /// presentation: an arc to the tray, a squash on tap, a shake and red flash on a blocked tap, a pop and shrink when its
    /// triple clears (after a hit-stop), and a drop and shrink on a loss. Nothing here changes Core state.
    /// The Seconds and Offset fields at the end record when things happened. The verification reads them; the game does not.
    public sealed class TileView : MonoBehaviour
    {
        private const float MoveSeconds = 0.25f;     // move to tray: 0.25 s, ease-out arrival
        private const float MoveArc = 0.3f;          // peak height of the arc above the straight path, in units
        private const float SquashSeconds = 0.12f;   // tap squash: 0.12 s, to 0.9 of scale and back
        private const float SquashDepth = 0.1f;
        private const float ShakeSeconds = 0.3f;     // blocked tap: 0.3 s horizontal shake
        private const float ShakeAmplitude = 0.12f;
        private const float PopSeconds = 0.08f;      // clear: pop to 1.15x, then shrink to zero over 0.2 s
        private const float PopScale = 1.15f;
        private const float ShrinkSeconds = 0.2f;
        private const float DropDistance = 0.5f;     // loss: drop 0.5 units while shrinking
        private const float DropSeconds = 0.5f;

        private Renderer _renderer;
        private Vector3 _from;
        private Vector3 _target;
        private Vector3 _position;
        private bool _arc;
        private float _moveT = 1f;
        private float _squashT = 1f;
        private float _shakeLeft;
        private float _flashLeft;
        private float _vanishDelay;
        private float _vanishT;
        private bool _vanishing;
        private float _dropDelay;
        private float _dropT = -1f;
        private float _baseScale = 1f;
        private Material _body;                  // the state's materials: what the tile shows when it is not flashing
        private Material _emblem;
        private Material _shownBody;             // what the renderer shows right now
        private Material _shownEmblem;
        private readonly Material[] _slots = new Material[2];   // reused, so a tap does not allocate an array

        public int TileId { get; private set; }

        /// Where the tile is heading. The controller reads it to place the popup.
        public Vector3 Target => _target;

        // Verification fields (game time, from Time.time). Written here, read by the verification.
        public float MoveStartedAt { get; private set; }
        public float MoveFinishedAt { get; private set; }
        public float MaxArcOffset { get; private set; }
        public float ShakeStartedAt { get; private set; } = -1f;
        public float ShakeEndedAt { get; private set; } = -1f;
        public float MaxShakeOffset { get; private set; }
        public float FlashStartedAt { get; private set; } = -1f;
        public float FlashEndedAt { get; private set; } = -1f;
        public float VanishCalledAt { get; private set; } = -1f;
        public float VanishStartedAt { get; private set; } = -1f;
        public float DropStartedAt { get; private set; } = -1f;
        public float DropEndedAt { get; private set; } = -1f;
        public float MaxDropOffset { get; private set; }

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
        }

        public void Init(int tileId, Vector3 start)
        {
            TileId = tileId;
            _baseScale = transform.localScale.x;
            _from = start;
            _target = start;
            _position = start;
            transform.position = start;
        }

        /// Sets where the tile goes. A tray arrival arcs over the board. A stack settles straight down.
        public void SetTarget(Vector3 target, bool arc)
        {
            if ((target - _target).magnitude < 0.0001f)
            {
                return;
            }

            _from = _position;
            _target = target;
            _arc = arc;
            _moveT = 0f;
            MoveStartedAt = Time.time;
        }

        /// Assigns the shared materials for the tile's state: the slab's colour and the emblem's. Shared materials keep the
        /// renderer from making copies. A tile whose state did not change keeps what it shows, so a tap only touches the
        /// tiles it changed.
        public void SetMaterials(Material body, Material emblem)
        {
            _body = body;
            _emblem = emblem;
            if (_flashLeft <= 0f)
            {
                Show(body, emblem);
            }
        }

        private void Show(Material body, Material emblem)
        {
            if (body == _shownBody && emblem == _shownEmblem)
            {
                return;
            }

            _shownBody = body;
            _shownEmblem = emblem;
            _slots[0] = body;
            _slots[1] = emblem;
            _renderer.sharedMaterials = _slots;
        }

        public void Squash()
        {
            _squashT = 0f;
        }

        public void Shake()
        {
            _shakeLeft = ShakeSeconds;
            ShakeStartedAt = Time.time;
            ShakeEndedAt = -1f;
            MaxShakeOffset = 0f;
        }

        /// Blocked tap or loss: the tile shows the shared red for a moment (slab and emblem), then its own materials again.
        public void Flash(Material body, Material emblem, float seconds)
        {
            _flashLeft = seconds;
            Show(body, emblem);
            FlashStartedAt = Time.time;
            FlashEndedAt = -1f;
        }

        /// Clear: the tile holds for the hit-stop, then pops to 1.15x and shrinks to nothing.
        public void Vanish(float hitStopSeconds)
        {
            _vanishing = true;
            _vanishDelay = hitStopSeconds;
            _vanishT = 0f;
            VanishCalledAt = Time.time;
        }

        /// Loss: after the flash, the tile drops and shrinks away.
        public void Drop(float afterSeconds)
        {
            _dropDelay = afterSeconds;
            _dropT = 0f;
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            if (_vanishing)
            {
                float cleared;
                if (_vanishDelay > 0f)
                {
                    _vanishDelay -= dt;
                    cleared = _baseScale;
                }
                else
                {
                    if (_vanishT <= 0f)
                    {
                        VanishStartedAt = Time.time;
                    }

                    _vanishT += dt;
                    if (_vanishT < PopSeconds)
                    {
                        float p = _vanishT / PopSeconds;
                        cleared = _baseScale * Mathf.Lerp(1f, PopScale, 1f - (1f - p) * (1f - p));   // ease-out pop
                    }
                    else
                    {
                        float s = (_vanishT - PopSeconds) / ShrinkSeconds;
                        if (s >= 1f)
                        {
                            Destroy(gameObject);
                            return;
                        }

                        cleared = _baseScale * PopScale * (1f - s * s);   // ease-in shrink
                    }
                }

                transform.localScale = new Vector3(cleared, cleared, cleared);
                transform.position = _position;
                return;
            }

            if (_moveT < 1f)
            {
                _moveT = Mathf.Min(1f, _moveT + dt / MoveSeconds);
                float e = 1f - (1f - _moveT) * (1f - _moveT);   // ease-out arrival
                _position = Vector3.Lerp(_from, _target, e);
                if (_arc)
                {
                    float arcOffset = MoveArc * Mathf.Sin(Mathf.PI * _moveT);
                    _position.y += arcOffset;
                    if (arcOffset > MaxArcOffset)
                    {
                        MaxArcOffset = arcOffset;
                    }
                }

                if (_moveT >= 1f)
                {
                    MoveFinishedAt = Time.time;
                }
            }

            float scale = _baseScale;
            if (_squashT < 1f)
            {
                _squashT = Mathf.Min(1f, _squashT + dt / SquashSeconds);
                scale *= 1f - SquashDepth * Mathf.Sin(Mathf.PI * _squashT);
            }

            Vector3 shown = _position;
            if (_shakeLeft > 0f)
            {
                _shakeLeft -= dt;
                float offset = Mathf.Sin(_shakeLeft * 60f) * ShakeAmplitude;
                shown.x += offset;
                if (Mathf.Abs(offset) > MaxShakeOffset)
                {
                    MaxShakeOffset = Mathf.Abs(offset);
                }

                if (_shakeLeft <= 0f)
                {
                    ShakeEndedAt = Time.time;
                }
            }

            if (_dropT >= 0f)
            {
                if (_dropDelay > 0f)
                {
                    _dropDelay -= dt;
                }
                else
                {
                    if (DropStartedAt < 0f)
                    {
                        DropStartedAt = Time.time;
                    }

                    _dropT = Mathf.Min(1f, _dropT + dt / DropSeconds);
                    float u = _dropT * _dropT;   // ease-in departure
                    shown.y -= DropDistance * u;
                    if (DropDistance * u > MaxDropOffset)
                    {
                        MaxDropOffset = DropDistance * u;
                    }

                    scale *= 1f - u;             // the fade is a shrink: no transparent material is used
                    if (_dropT >= 1f && DropEndedAt < 0f)
                    {
                        DropEndedAt = Time.time;
                    }
                }
            }

            if (_flashLeft > 0f)
            {
                _flashLeft -= dt;
                if (_flashLeft <= 0f)
                {
                    Show(_body, _emblem);
                    FlashEndedAt = Time.time;
                }
            }

            transform.localScale = new Vector3(scale, scale, scale);
            transform.position = shown;
        }
    }
}
