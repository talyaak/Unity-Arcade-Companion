using PulseArena.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace PulseArena.Game
{
    /// Match HUD in UI Toolkit (D6). It shows Core's state only: the stage, the score, the combo, a bar for the tiles cleared,
    /// the tray as seven slots, and the result (Level cleared or Tray full) with its buttons. Points are world-space popups
    /// (D6), so the result card shows results only. The look is in MatchHud.uss (T10). It keeps no game state, and it writes a
    /// value only when that value changes.
    public sealed class MatchHud : MonoBehaviour
    {
        private const float SlideSeconds = 0.3f;     // the result card slides in over 0.3 s (juice standard)
        private const float SlideDistance = 160f;
        private const int WarmUpFrames = 2;
        private const int ComboForFullBar = 5;       // the combo bar fills at this combo and stays full above it

        // Every character the HUD draws. Drawn once before the first tap (see Start), so no tap pays for a new glyph.
        private const string WarmUpText = "Stage Score Combo Tray Cleared Level cleared Tray full Retry Next stage "
            + "Reduce motion: on off 0123456789 / abcdefghijklmnopqrstuvwxyz ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        [SerializeField] private MatchController _match;           // the grid game
        [SerializeField] private PhysicsMatchController _physics;  // physics mode (D14); set in its own scene instead of _match
        private IMatchStatus _source;                              // whichever of the two is set, read through the interface
        [SerializeField] private StyleSheet _style;

        private VisualElement _root;
        private VisualElement[] _pips = new VisualElement[0];
        private Label _stage;
        private Label _score;
        private Label _combo;
        private VisualElement _comboFill;
        private Label _cleared;
        private VisualElement _clearedFill;
        private Label _tray;
        private VisualElement _result;
        private Label _banner;
        private Label _detail;
        private Button _retry;
        private Button _next;
        private Button _motion;

        private MatchBoard _shownBoard;
        private int _totalTiles;
        private int _shownStage = -1;
        private int _shownScore = -1;
        private int _shownCombo = -1;
        private int _shownTray = -1;
        private int _shownCleared = -1;
        private MatchState _shownState = MatchState.Playing;
        private float _slideT = 1f;
        private bool _shownReduce;
        private bool _shownLandscape;
        private int _warmFrames;

        // Verification fields (game time). The verification reads them; the game does not.
        public float SlideStartedAt { get; private set; } = -1f;
        public float SlideEndedAt { get; private set; } = -1f;

        // Start runs after UIDocument has its panel, so the root element exists here.
        private void Start()
        {
            _source = _physics != null ? (IMatchStatus)_physics : _match;
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            _root = root;
            root.pickingMode = PickingMode.Ignore;   // taps on the board pass through the HUD to the tiles
            if (_style != null)
            {
                root.styleSheets.Add(_style);
            }

            root.AddToClassList("hud-root");

            // Top bar: the stage badge, the score, and Reduce Motion at the right, clear of the board and the popups.
            VisualElement topBar = MakeBox("hud-bar");
            VisualElement badge = MakeBox("hud-badge");
            badge.Add(MakeLabel("Stage", "hud-badge-label"));
            _stage = MakeLabel(string.Empty, "hud-badge-value");
            badge.Add(_stage);
            topBar.Add(badge);

            VisualElement score = MakeBox("hud-score");
            score.Add(MakeLabel("Score", "hud-kicker"));
            _score = MakeLabel(string.Empty, "hud-value");
            score.Add(_score);
            topBar.Add(score);

            _motion = MakeButton("Reduce motion: off", "hud-pill");
            _motion.clicked += OnToggleMotion;
            topBar.Add(_motion);
            root.Add(topBar);

            // Progress: the tiles cleared out of the level's total, and the combo (full at ComboForFullBar).
            VisualElement panel = MakeBox("hud-panel");
            VisualElement clearedRow = MakeBox("hud-row");
            clearedRow.Add(MakeLabel("Cleared", "hud-kicker"));
            _cleared = MakeLabel(string.Empty, "hud-kicker-value");
            clearedRow.Add(_cleared);
            panel.Add(clearedRow);
            VisualElement clearedTrack = MakeBox("hud-track");
            _clearedFill = MakeBox("hud-fill");
            clearedTrack.Add(_clearedFill);
            panel.Add(clearedTrack);

            VisualElement comboRow = MakeBox("hud-row", "hud-row-gap");
            comboRow.Add(MakeLabel("Combo", "hud-kicker"));
            _combo = MakeLabel(string.Empty, "hud-kicker-value");
            comboRow.Add(_combo);
            panel.Add(comboRow);
            VisualElement comboTrack = MakeBox("hud-track", "hud-track-small");
            _comboFill = MakeBox("hud-fill", "hud-fill-combo");
            comboTrack.Add(_comboFill);
            panel.Add(comboTrack);
            root.Add(panel);

            root.Add(MakeSpacer(1f));

            // The result card: the banner, the score, and the buttons. It slides in when the level ends.
            _result = MakeBox("hud-card");
            _result.pickingMode = PickingMode.Ignore;   // the buttons inside still take taps
            _result.style.opacity = 0f;
            _banner = MakeLabel(string.Empty, "hud-banner");
            _result.Add(_banner);
            _detail = MakeLabel(string.Empty, "hud-detail");
            _result.Add(_detail);
            VisualElement buttons = MakeBox("hud-buttons");
            _retry = MakeButton("Retry", "hud-button", "hud-button-secondary");
            _next = MakeButton("Next stage", "hud-button", "hud-button-primary");
            _retry.clicked += OnRetry;
            _next.clicked += OnNextStage;
            buttons.Add(_retry);
            buttons.Add(_next);
            _result.Add(buttons);
            root.Add(_result);

            root.Add(MakeSpacer(1.3f));

            // The tray: the count, and one slot per tile the tray holds. A slot lights as a tile arrives, and turns warm near full.
            VisualElement tray = MakeBox("hud-tray");
            VisualElement trayRow = MakeBox("hud-row");
            trayRow.Add(MakeLabel("Tray", "hud-kicker"));
            _tray = MakeLabel(string.Empty, "hud-kicker-value");
            trayRow.Add(_tray);
            tray.Add(trayRow);
            VisualElement pipRow = MakeBox("hud-pips");
            int capacity = _source != null ? _source.TrayCapacity : 0;
            _pips = new VisualElement[capacity];
            for (int i = 0; i < capacity; i++)
            {
                _pips[i] = MakeBox("hud-pip");
                pipRow.Add(_pips[i]);
            }

            tray.Add(pipRow);
            root.Add(tray);

            // Pre-warm. UI Toolkit builds the glyphs for a text the first time that text is laid out, and that build is the
            // expensive frame (the standalone spikes, T3 notes). So every label and button draws WarmUpText for two frames,
            // before the first tap. Update puts the real text back when the warm-up ends.
            _stage.text = WarmUpText;
            _score.text = WarmUpText;
            _cleared.text = WarmUpText;
            _combo.text = WarmUpText;
            _tray.text = WarmUpText;
            _banner.text = WarmUpText;
            _detail.text = WarmUpText;
            _motion.text = WarmUpText;
            _retry.text = WarmUpText;
            _next.text = WarmUpText;
            _retry.style.display = DisplayStyle.Flex;
            _next.style.display = DisplayStyle.Flex;
            _warmFrames = WarmUpFrames;
        }

        private void Update()
        {
            if (_source == null || _source.Board == null || _stage == null)
            {
                return;
            }

            if (_warmFrames > 0)
            {
                _warmFrames--;
                if (_warmFrames == 0)
                {
                    RestoreAfterWarmUp();
                }

                return;
            }

            // A compact look in landscape, so the top and bottom blocks stay inside the bands the camera leaves for them.
            bool landscape = Screen.width > Screen.height;
            if (landscape != _shownLandscape)
            {
                _shownLandscape = landscape;
                _root.EnableInClassList("hud-landscape", landscape);
            }

            MatchBoard board = _source.Board;
            if (board != _shownBoard)
            {
                // A new level (Retry, Next stage, or the first one): its total can differ even when the values match,
                // so every value is written again.
                _shownBoard = board;
                _totalTiles = board.TotalTiles;
                _shownStage = -1;
                _shownScore = -1;
                _shownCombo = -1;
                _shownCleared = -1;
                _shownTray = -1;
            }

            int stage = _source.StageNumber;
            int score = board.Score;
            int combo = board.Combo;
            int tray = board.Tray.Count;
            int cleared = board.TilesCleared;   // Core's count of tiles that left through a triple (tiles in the tray are not cleared)

            if (stage != _shownStage)
            {
                _shownStage = stage;
                _stage.text = stage.ToString();
            }

            if (score != _shownScore)
            {
                _shownScore = score;
                _score.text = score.ToString();
            }

            if (combo != _shownCombo)
            {
                _shownCombo = combo;
                _combo.text = combo.ToString();
                float comboFill = combo >= ComboForFullBar ? 100f : 100f * combo / ComboForFullBar;
                _comboFill.style.width = Length.Percent(comboFill);
            }

            if (cleared != _shownCleared)
            {
                _shownCleared = cleared;
                _cleared.text = cleared + " / " + _totalTiles;
                float clearedFill = _totalTiles > 0 ? 100f * cleared / _totalTiles : 0f;
                _clearedFill.style.width = Length.Percent(clearedFill);
            }

            if (tray != _shownTray)
            {
                _shownTray = tray;
                _tray.text = tray + " / " + _source.TrayCapacity;
                RefreshPips(tray);
            }

            MatchState state = board.State;
            if (state != _shownState)
            {
                _shownState = state;
                _retry.style.display = state == MatchState.Lost ? DisplayStyle.Flex : DisplayStyle.None;
                _next.style.display = state == MatchState.Won ? DisplayStyle.Flex : DisplayStyle.None;
                _banner.text = state == MatchState.Won ? "Level cleared"
                    : state == MatchState.Lost ? "Tray full"
                    : string.Empty;
                _banner.EnableInClassList("hud-banner-won", state == MatchState.Won);
                _banner.EnableInClassList("hud-banner-lost", state == MatchState.Lost);
                _detail.text = state == MatchState.Playing ? string.Empty : "Score " + score;
                _slideT = state == MatchState.Playing ? 1f : 0f;
                if (state != MatchState.Playing)
                {
                    SlideStartedAt = Time.time;
                    SlideEndedAt = -1f;
                }
                else
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

            bool reduce = _source.ReduceMotion;
            if (reduce != _shownReduce)
            {
                _shownReduce = reduce;
                _motion.text = reduce ? "Reduce motion: on" : "Reduce motion: off";
            }
        }

        // The tray's slots: a slot is lit when a tile is in it, and turns warm when the tray is one short of full or full.
        private void RefreshPips(int tray)
        {
            int warmFrom = _pips.Length - 1;
            for (int i = 0; i < _pips.Length; i++)
            {
                bool lit = i < tray;
                _pips[i].EnableInClassList("hud-pip-on", lit);
                _pips[i].EnableInClassList("hud-pip-warn", lit && tray >= warmFrom);
            }
        }

        // The warm-up text is replaced by the real text on the next Update, because the shown values are still unset.
        private void RestoreAfterWarmUp()
        {
            _retry.text = "Retry";
            _next.text = "Next stage";
            _banner.text = string.Empty;
            _detail.text = string.Empty;
            _shownReduce = _source.ReduceMotion;
            _motion.text = _shownReduce ? "Reduce motion: on" : "Reduce motion: off";
            _retry.style.display = DisplayStyle.None;
            _next.style.display = DisplayStyle.None;
        }

        private void OnRetry()
        {
            _source.Retry();
        }

        private void OnNextStage()
        {
            _source.NextStage();
        }

        private void OnToggleMotion()
        {
            _source.ToggleReduceMotion();
        }

        private static VisualElement MakeBox(params string[] classes)
        {
            var box = new VisualElement();
            box.pickingMode = PickingMode.Ignore;
            foreach (string name in classes)
            {
                box.AddToClassList(name);
            }

            return box;
        }

        private static Label MakeLabel(string text, params string[] classes)
        {
            var label = new Label();
            label.text = text;
            label.pickingMode = PickingMode.Ignore;
            foreach (string name in classes)
            {
                label.AddToClassList(name);
            }

            return label;
        }

        private static Button MakeButton(string text, params string[] classes)
        {
            var button = new Button { text = text };
            foreach (string name in classes)
            {
                button.AddToClassList(name);
            }

            return button;
        }

        private static VisualElement MakeSpacer(float grow)
        {
            var spacer = new VisualElement();
            spacer.style.flexGrow = grow;
            spacer.pickingMode = PickingMode.Ignore;
            return spacer;
        }
    }
}
