using PulseArena.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace PulseArena.Game
{
    /// Match HUD in UI Toolkit: stage, score, combo, tray count, the result (Level cleared or Tray full) with its buttons,
    /// and the Reduce Motion toggle. Points are world-space popups (D6), so the message shows results only. It keeps no game
    /// state, and a label is written only when its value changes.
    public sealed class MatchHud : MonoBehaviour
    {
        private const float Padding = 16f;
        private const float SlideSeconds = 0.3f;     // the result panel slides in over 0.3 s (juice standard)
        private const float SlideDistance = 160f;
        private const int WarmUpFrames = 2;
        // Every character the HUD draws. Drawn once before the first tap (see Start), so no tap pays for a new glyph.
        private const string WarmUpText = "Stage Score Combo Tray Left Level cleared Tray full Retry Next stage Reduce motion: on off 0123456789 /";
        private static readonly Color Ink = new Color(0.96f, 0.96f, 0.98f, 1f);
        private static readonly Color ButtonFill = new Color(0.25f, 0.75f, 0.72f, 1f);
        private static readonly Color ButtonInk = new Color(0.07f, 0.08f, 0.1f, 1f);

        [SerializeField] private MatchController _match;

        private Label _status;
        private VisualElement _result;
        private Label _message;
        private Button _retry;
        private Button _next;
        private Button _motion;

        private int _shownStage = -1;
        private int _shownScore = -1;
        private int _shownCombo = -1;
        private int _shownTray = -1;
        private int _shownLeft = -1;
        private MatchState _shownState = MatchState.Playing;
        private float _slideT = 1f;
        private bool _shownReduce;
        private int _warmFrames;

        // Verification fields (game time). The verification reads them; the game does not.
        public float SlideStartedAt { get; private set; } = -1f;
        public float SlideEndedAt { get; private set; } = -1f;

        // Start runs after UIDocument has its panel, so the root element exists here.
        private void Start()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            root.pickingMode = PickingMode.Ignore;   // taps on the board pass through the HUD to the tiles
            root.style.flexDirection = FlexDirection.Column;
            root.style.paddingLeft = Padding;
            root.style.paddingRight = Padding;
            root.style.paddingTop = Padding;
            root.style.paddingBottom = Padding;

            // Top row: the status on the left, Reduce Motion in the top-right corner, clear of the board and the popups.
            var topRow = new VisualElement();
            topRow.style.flexDirection = FlexDirection.Row;
            topRow.style.alignItems = Align.Center;
            _status = AddLabel(topRow, 28f);
            _status.style.flexGrow = 1f;
            _motion = AddButton(topRow, "Reduce motion: off", visible: true);
            _motion.clicked += OnToggleMotion;
            root.Add(topRow);
            root.Add(Spacer(1f));

            _result = new VisualElement();
            _result.pickingMode = PickingMode.Ignore;
            _result.style.opacity = 0f;
            _message = AddLabel(_result, 48f);
            _message.style.unityTextAlign = TextAnchor.MiddleCenter;
            _message.style.marginBottom = Padding;

            var buttons = new VisualElement();
            buttons.style.flexDirection = FlexDirection.Row;
            buttons.style.justifyContent = Justify.Center;
            _retry = AddButton(buttons, "Retry", visible: false);
            _next = AddButton(buttons, "Next stage", visible: false);
            _retry.clicked += OnRetry;
            _next.clicked += OnNextStage;
            _result.Add(buttons);
            root.Add(_result);

            root.Add(Spacer(2f));

            // Pre-warm. UI Toolkit builds the glyphs for a text the first time that text is laid out, and that build is the
            // expensive frame (the standalone spikes, T3 notes). So every label and button draws WarmUpText for two frames,
            // before the first tap. Update puts the real text back when the warm-up ends.
            _status.text = WarmUpText;
            _message.text = WarmUpText;
            _motion.text = WarmUpText;
            _retry.text = WarmUpText;
            _next.text = WarmUpText;
            _retry.style.display = DisplayStyle.Flex;
            _next.style.display = DisplayStyle.Flex;
            _warmFrames = WarmUpFrames;
        }

        private void Update()
        {
            if (_match == null || _match.Board == null || _status == null)
            {
                return;
            }

            if (_warmFrames > 0)
            {
                _warmFrames--;
                if (_warmFrames == 0)
                {
                    _message.text = string.Empty;
                    _retry.text = "Retry";
                    _next.text = "Next stage";
                    _motion.text = _match.ReduceMotion ? "Reduce motion: on" : "Reduce motion: off";
                    _retry.style.display = DisplayStyle.None;
                    _next.style.display = DisplayStyle.None;
                }

                return;
            }

            MatchBoard board = _match.Board;
            int tray = board.Tray.Count;
            int left = board.RemainingOnBoard;
            int stage = _match.StageNumber;
            if (stage != _shownStage || board.Score != _shownScore || board.Combo != _shownCombo
                || tray != _shownTray || left != _shownLeft)
            {
                _shownStage = stage;
                _shownScore = board.Score;
                _shownCombo = board.Combo;
                _shownTray = tray;
                _shownLeft = left;
                _status.text = $"Stage {stage}   Score {board.Score}   Combo {board.Combo}   " +
                    $"Tray {tray}/{_match.TrayCapacity}   Left {left}";
            }

            MatchState state = board.State;
            if (state != _shownState)
            {
                _shownState = state;
                _retry.style.display = state == MatchState.Lost ? DisplayStyle.Flex : DisplayStyle.None;
                _next.style.display = state == MatchState.Won ? DisplayStyle.Flex : DisplayStyle.None;
                _message.text = state == MatchState.Won ? "Level cleared"
                    : state == MatchState.Lost ? "Tray full"
                    : string.Empty;
                _slideT = state == MatchState.Playing ? 1f : 0f;
                if (state != MatchState.Playing)
                {
                    SlideStartedAt = Time.time;
                    SlideEndedAt = -1f;
                }
                if (state == MatchState.Playing)
                {
                    _result.style.opacity = 0f;
                }
            }

            if (_slideT < 1f)
            {
                _slideT = Mathf.Min(1f, _slideT + Time.deltaTime / SlideSeconds);
                float e = 1f - (1f - _slideT) * (1f - _slideT);   // ease-out arrival
                _result.style.marginTop = (1f - e) * SlideDistance;
                _result.style.opacity = e;
                if (_slideT >= 1f)
                {
                    SlideEndedAt = Time.time;
                }
            }

            bool reduce = _match.ReduceMotion;
            if (reduce != _shownReduce)
            {
                _shownReduce = reduce;
                _motion.text = reduce ? "Reduce motion: on" : "Reduce motion: off";
            }
        }

        private void OnRetry()
        {
            _match.Retry();
        }

        private void OnNextStage()
        {
            _match.NextStage();
        }

        private void OnToggleMotion()
        {
            _match.ToggleReduceMotion();
        }

        private static Label AddLabel(VisualElement parent, float size)
        {
            var label = new Label();
            label.style.fontSize = size;
            label.style.color = Ink;
            label.pickingMode = PickingMode.Ignore;
            parent.Add(label);
            return label;
        }

        private static Button AddButton(VisualElement parent, string text, bool visible)
        {
            var button = new Button { text = text };
            button.style.fontSize = 32f;
            button.style.color = ButtonInk;
            button.style.backgroundColor = ButtonFill;
            button.style.paddingLeft = 28f;
            button.style.paddingRight = 28f;
            button.style.paddingTop = 16f;
            button.style.paddingBottom = 16f;
            button.style.marginLeft = 8f;
            button.style.marginRight = 8f;
            button.style.marginTop = 8f;
            button.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            parent.Add(button);
            return button;
        }

        private static VisualElement Spacer(float grow)
        {
            var spacer = new VisualElement();
            spacer.style.flexGrow = grow;
            spacer.pickingMode = PickingMode.Ignore;
            return spacer;
        }
    }
}
