using UnityEngine;

namespace PulseArena.Game
{
    /// World-space "+points" popup (D6: the popup is 3D, not HUD). It rises 0.6 units over 0.9 s with ease-out, and fades
    /// over the same time with ease-in. One popup object is reused, so a newer clear replaces the one on screen.
    public sealed class MatchPopup : MonoBehaviour
    {
        private const float Seconds = 0.9f;
        private const float Rise = 0.6f;
        private const float Lift = 0.5f;   // starts this far above the tile it belongs to

        private static readonly Color Ink = new Color(1f, 0.95f, 0.6f, 1f);

        private TextMesh _text;
        private Vector3 _start;
        private float _t = 1f;             // 1 = finished, nothing shown

        // Verification fields (game time). The verification reads them; the game does not.
        public float ShownAt { get; private set; } = -1f;
        public float FinishedAt { get; private set; } = -1f;
        public float RiseSoFar { get; private set; }
        public float AlphaSoFar { get; private set; }

        private void Awake()
        {
            _text = gameObject.AddComponent<TextMesh>();
            _text.characterSize = 0.12f;
            _text.fontSize = 72;
            _text.anchor = TextAnchor.MiddleCenter;
            _text.alignment = TextAlignment.Center;
            _text.color = new Color(Ink.r, Ink.g, Ink.b, 0f);

            // Set every glyph the popups use once, now, so the first real popups do not stall the frame that shows them.
            _text.text = "+0123456789";
        }

        /// Shows the message above a position, facing the camera.
        public void Show(string message, Vector3 position, Quaternion facing)
        {
            _text.text = message;
            _start = position + new Vector3(0f, Lift, 0f);
            transform.position = _start;
            transform.rotation = facing;
            _t = 0f;
            ShownAt = Time.time;
            FinishedAt = -1f;
        }

        private void Update()
        {
            if (_t >= 1f)
            {
                return;
            }

            _t = Mathf.Min(1f, _t + Time.deltaTime / Seconds);
            float rise = 1f - (1f - _t) * (1f - _t);   // ease-out arrival
            transform.position = _start + new Vector3(0f, Rise * rise, 0f);
            RiseSoFar = Rise * rise;

            float alpha = 1f - _t * _t;                // ease-in departure: stays readable, then fades
            _text.color = new Color(Ink.r, Ink.g, Ink.b, alpha);
            AlphaSoFar = alpha;

            if (_t >= 1f)
            {
                FinishedAt = Time.time;
            }
        }
    }
}
