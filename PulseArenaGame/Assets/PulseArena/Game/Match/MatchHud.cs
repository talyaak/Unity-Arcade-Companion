using PulseArena.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace PulseArena.Game
{
    /// Match HUD in UI Toolkit: stage, score, combo, tray count, one message, and the Retry and Next stage buttons.
    /// It shows what MatchController and Core report and sends button presses back. It keeps no game state, and a label
    /// is written only when its value changes.
    public sealed class MatchHud : MonoBehaviour
    {
        private const float Padding = 16f;
        private static readonly Color Ink = new Color(0.96f, 0.96f, 0.98f, 1f);
        private static readonly Color ButtonFill = new Color(0.25f, 0.75f, 0.72f, 1f);
        private static readonly Color ButtonInk = new Color(0.07f, 0.08f, 0.1f, 1f);

        [SerializeField] private MatchController _match;

        private Label _status;
        private Label _message;
        private Button _retry;
        private Button _next;

        private int _shownStage = -1;
        private int _shownScore = -1;
        private int _shownCombo = -1;
        private int _shownTray = -1;
        private int _shownLeft = -1;
        private MatchState _shownState = MatchState.Playing;
        private string _shownMessage = string.Empty;

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

            _status = AddLabel(root, 28f);
            root.Add(Spacer(1f));

            _message = AddLabel(root, 48f);
            _message.style.unityTextAlign = TextAnchor.MiddleCenter;
            _message.style.marginBottom = Padding;

            var buttons = new VisualElement();
            buttons.style.flexDirection = FlexDirection.Row;
            buttons.style.justifyContent = Justify.Center;
            _retry = AddButton(buttons, "Retry");
            _next = AddButton(buttons, "Next stage");
            _retry.clicked += OnRetry;
            _next.clicked += OnNextStage;
            root.Add(buttons);

            root.Add(Spacer(2f));
        }

        private void Update()
        {
            if (_match == null || _match.Board == null || _status == null)
            {
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
            }

            string message = state == MatchState.Won ? "Level cleared"
                : state == MatchState.Lost ? "Tray full"
                : _match.PopupText;
            if (message != _shownMessage)
            {
                _shownMessage = message;
                _message.text = message;
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

        private static Label AddLabel(VisualElement parent, float size)
        {
            var label = new Label();
            label.style.fontSize = size;
            label.style.color = Ink;
            label.pickingMode = PickingMode.Ignore;
            parent.Add(label);
            return label;
        }

        private static Button AddButton(VisualElement parent, string text)
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
            button.style.display = DisplayStyle.None;
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
