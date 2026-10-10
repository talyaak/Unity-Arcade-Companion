using System;
using System.Collections.Generic;
using PulseArena.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PulseArena.Game
{
    /// Runs one match level in the scene. It builds 3D tiles from Core's level, sends taps to Core, and plays back what Core
    /// reports. It never decides blocking or matches. Core is the only source of truth for what is free and what clears.
    public sealed class MatchController : MonoBehaviour
    {
        private const float CellSpacing = 1.15f;
        private const float TileHeight = 0.95f;
        private const float TraySpacing = 1.5f;      // wider than the board pitch: the tops of tall primitives look about 1.6x wider from the camera
        private const float TrayY = 0.5f;
        private const float TrayZ = -4.6f;
        private const float MaxFrameSeconds = 0.1f;
        private const float PopupSeconds = 0.9f;
        private const float DimmedBrightness = 0.45f;
        private const float TileScale = 0.9f;         // tile footprint in units (ASSET_PIPELINE section 3)
        private const float TileRadius = TileScale * 0.5f;
        private const float TileHalfHeight = TileScale; // cylinders and capsules are 2 units tall at scale 1
        private const float PopupHeadroom = 1f;       // room above the stacks for the world-space "+points" popup (D6)
        private const float FramingMargin = 1.05f;

        // Original palette and shapes for this game. Symbols beyond these lists cycle through them.
        private static readonly Color[] Palette =
        {
            new Color(0.95f, 0.45f, 0.35f, 1f),
            new Color(0.25f, 0.75f, 0.72f, 1f),
            new Color(0.97f, 0.80f, 0.35f, 1f),
            new Color(0.62f, 0.45f, 0.92f, 1f),
            new Color(0.55f, 0.85f, 0.45f, 1f),
            new Color(0.45f, 0.68f, 0.95f, 1f),
            new Color(0.95f, 0.55f, 0.75f, 1f)
        };

        private static readonly PrimitiveType[] Shapes =
        {
            PrimitiveType.Cube,
            PrimitiveType.Sphere,
            PrimitiveType.Cylinder,
            PrimitiveType.Capsule
        };

        [SerializeField] private int _stageIndex;

        /// Base material for tiles, created by Pulse Arena > Build Match Scene. Colour variants are copies of it.
        [SerializeField] private Material _tileMaterial;

        private readonly MatchRules _rules = new MatchRules();
        private readonly Dictionary<int, TileView> _views = new Dictionary<int, TileView>();
        private readonly Dictionary<(int, bool), Material> _materials = new Dictionary<(int, bool), Material>();
        private MatchLevelSpec _spec;
        private MatchBoard _board;
        private double _clock;
        private string _popup = string.Empty;
        private float _popupLeft;
        private Camera _camera;
        private int _maxStack;
        private int _fitWidth;
        private int _fitHeight;
        private bool _pointerWasDown;
        private readonly List<Vector3> _fitPoints = new List<Vector3>();

        public MatchBoard Board => _board;
        public int StageNumber => _stageIndex + 1;
        public int TrayCapacity => _rules.TrayCapacity;

        /// The popup text while it is showing, otherwise empty.
        public string PopupText => _popupLeft > 0f ? _popup : string.Empty;

        /// Restarts the current stage with a new seed. Called by the HUD's Retry button.
        public void Retry()
        {
            StartLevel(_stageIndex, (ulong)DateTime.UtcNow.Ticks);
        }

        /// Starts the next stage, wrapping after the last one. Called by the HUD's Next stage button.
        public void NextStage()
        {
            StartLevel((_stageIndex + 1) % MatchStages.All.Count, (ulong)DateTime.UtcNow.Ticks);
        }

        private void Start()
        {
            if (_tileMaterial == null)
            {
                Debug.LogError("MatchController needs a Tile Material. Run Pulse Arena > Build Match Scene.", this);
                enabled = false;
                return;
            }

            _camera = Camera.main;
            StartLevel(_stageIndex, (ulong)DateTime.UtcNow.Ticks);
        }

        public void StartLevel(int stageIndex, ulong seed)
        {
            if (stageIndex < 0 || stageIndex >= MatchStages.All.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(stageIndex));
            }

            ClearViews();
            _stageIndex = stageIndex;
            _spec = MatchStages.All[stageIndex];
            _clock = 0;
            _popup = string.Empty;
            _popupLeft = 0f;

            MatchLevel level = MatchLevelGenerator.Generate(_spec, seed);
            _board = new MatchBoard(level.Tiles, _rules);
            foreach (MatchTile tile in level.Tiles)
            {
                CreateView(tile);
            }

            _maxStack = MaxStackHeight(level.Tiles);
            BuildFitPoints(level.Tiles);
            LayoutBoard();
            RefreshTints();
            FitCamera();
        }

        /// Sends one tap to Core. Returns Core's answer, or null when there is no level.
        public MatchStep Tap(int tileId)
        {
            if (_board == null)
            {
                return null;
            }

            // Note the tray's tiles of this symbol before the tap, so a clear can animate all three.
            var sameSymbolInTray = new List<int>();
            if (_board.TryGetOnBoard(tileId, out MatchTile tapped))
            {
                foreach (MatchTile inTray in _board.Tray)
                {
                    if (inTray.Symbol == tapped.Symbol)
                    {
                        sameSymbolInTray.Add(inTray.Id);
                    }
                }
            }

            MatchStep step = _board.TapTile(tileId, _clock);

            if (step.Outcome == MatchOutcome.Blocked && _views.TryGetValue(tileId, out TileView blocked))
            {
                blocked.Shake();
            }

            if (step.Outcome == MatchOutcome.Moved || step.Outcome == MatchOutcome.Cleared
                || step.Outcome == MatchOutcome.Won || step.Outcome == MatchOutcome.Lost)
            {
                LayoutBoard();
            }

            foreach (MatchEvent e in step.Events)
            {
                if (e.Kind == MatchEventKind.Cleared)
                {
                    sameSymbolInTray.Add(tileId);
                    foreach (int id in sameSymbolInTray)
                    {
                        if (_views.TryGetValue(id, out TileView vanishing))
                        {
                            vanishing.Vanish();
                            _views.Remove(id);
                        }
                    }

                    _popup = $"+{e.Points}";
                    _popupLeft = PopupSeconds;
                }
            }

            RefreshTints();
            return step;
        }

        private void Update()
        {
            if (_board != null && _board.State == MatchState.Playing)
            {
                _clock += FrameTime.Clamp(Time.deltaTime, MaxFrameSeconds);
            }

            if (_popupLeft > 0f)
            {
                _popupLeft -= Time.deltaTime;
            }

            if (_board != null && (Screen.width != _fitWidth || Screen.height != _fitHeight))
            {
                FitCamera();
            }

            HandlePointer();
        }

        // The Input System's Pointer covers mouse, pen and touch, so the same code works in the Editor and on a phone.
        // A tap is the first frame in which the press is down. The edge comes from isPressed, because during the T1 Editor
        // checks Update never saw wasPressedThisFrame, while isPressed changed as expected.
        private void HandlePointer()
        {
            Pointer pointer = Pointer.current;
            if (pointer == null)
            {
                return;
            }

            bool down = pointer.press.isPressed;
            bool pressedNow = down && !_pointerWasDown;
            _pointerWasDown = down;
            if (!pressedNow || Camera.main == null)
            {
                return;
            }

            Vector2 screen = pointer.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(new Vector3(screen.x, screen.y, 0f));
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                TileView view = hit.collider.GetComponent<TileView>();
                if (view != null)
                {
                    Tap(view.TileId);
                }
            }
        }

        // Keeps the board, tray and popup room on screen at any aspect ratio. The camera fits the real content points, not a
        // box around them, so the board takes as much of the frame as the content allows. Only the distance changes.
        private void FitCamera()
        {
            if (_camera == null || _fitPoints.Count == 0)
            {
                return;
            }

            _fitWidth = Screen.width;
            _fitHeight = Screen.height;

            Vector3 min = _fitPoints[0];
            Vector3 max = _fitPoints[0];
            foreach (Vector3 p in _fitPoints)
            {
                min = new Vector3(Mathf.Min(min.x, p.x), Mathf.Min(min.y, p.y), Mathf.Min(min.z, p.z));
                max = new Vector3(Mathf.Max(max.x, p.x), Mathf.Max(max.y, p.y), Mathf.Max(max.z, p.z));
            }

            Vector3 center = (min + max) * 0.5f;
            float tanV = Mathf.Tan(_camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            float tanH = tanV * _camera.aspect;
            Vector3 right = _camera.transform.right;
            Vector3 up = _camera.transform.up;
            Vector3 forward = _camera.transform.forward;

            // The camera sits at distance d behind the centre on its own view axis. A point at offset rel from the centre is
            // inside the frustum when its sideways and vertical offsets, divided by the tangent of the half field, do not
            // exceed its depth (its forward offset plus d). Take the smallest d that holds for every point.
            float distance = 0f;
            foreach (Vector3 p in _fitPoints)
            {
                Vector3 rel = p - center;
                float ahead = Dot(rel, forward);
                distance = Mathf.Max(distance, Mathf.Abs(Dot(rel, right)) / tanH - ahead);
                distance = Mathf.Max(distance, Mathf.Abs(Dot(rel, up)) / tanV - ahead);
            }

            _camera.transform.position = center - forward * (distance * FramingMargin);
        }

        // Points the camera must keep on screen: every tile's corners at its layout position, every tray slot, and a point
        // above the tallest stack for the popup. Filled once per level, so a window resize refits without allocating.
        private void BuildFitPoints(IEnumerable<MatchTile> tiles)
        {
            _fitPoints.Clear();
            var perCell = new Dictionary<(int, int), int>();
            foreach (MatchTile tile in tiles)
            {
                var cell = (tile.X, tile.Y);
                perCell.TryGetValue(cell, out int height);
                perCell[cell] = height + 1;
                AddBox(BoardPosition(tile.X, tile.Y, height));
            }

            for (int i = 0; i < _rules.TrayCapacity; i++)
            {
                AddBox(TraySlot(i));
            }

            _fitPoints.Add(new Vector3(0f, _maxStack * TileHeight + TileHalfHeight + PopupHeadroom, 0f));
        }

        private void AddBox(Vector3 center)
        {
            for (int i = 0; i < 8; i++)
            {
                _fitPoints.Add(new Vector3(
                    center.x + ((i & 1) == 0 ? -TileRadius : TileRadius),
                    center.y + ((i & 2) == 0 ? -TileHalfHeight : TileHalfHeight),
                    center.z + ((i & 4) == 0 ? -TileRadius : TileRadius)));
            }
        }

        private static float Dot(Vector3 a, Vector3 b)
        {
            return a.x * b.x + a.y * b.y + a.z * b.z;
        }

        private static int MaxStackHeight(IEnumerable<MatchTile> tiles)
        {
            var perCell = new Dictionary<(int, int), int>();
            int max = 0;
            foreach (MatchTile tile in tiles)
            {
                var cell = (tile.X, tile.Y);
                perCell.TryGetValue(cell, out int count);
                count++;
                perCell[cell] = count;
                if (count > max) { max = count; }
            }

            return max;
        }

        private void OnDestroy()
        {
            foreach (Material material in _materials.Values)
            {
                Destroy(material);
            }

            _materials.Clear();
        }

        private void CreateView(MatchTile tile)
        {
            GameObject go = GameObject.CreatePrimitive(Shapes[tile.Symbol % Shapes.Length]);
            go.name = "Tile " + tile.Id;
            go.transform.localScale = new Vector3(TileScale, TileScale, TileScale);
            TileView view = go.AddComponent<TileView>();
            view.Init(tile.Id, BoardPosition(tile.X, tile.Y, 0));
            view.SetMaterial(MaterialFor(tile.Symbol, dimmed: false));
            _views.Add(tile.Id, view);
        }

        private void ClearViews()
        {
            foreach (TileView view in _views.Values)
            {
                Destroy(view.gameObject);
            }

            _views.Clear();
        }

        // Board tiles go to their stack position, and tray tiles go to their slots. Heights come from the tiles that
        // remain in each cell, so a stack settles when the tiles above it leave.
        private void LayoutBoard()
        {
            var stacks = new Dictionary<(int, int), List<MatchTile>>();
            foreach (KeyValuePair<int, TileView> pair in _views)
            {
                if (!_board.TryGetOnBoard(pair.Key, out MatchTile tile))
                {
                    continue;
                }

                var cell = (tile.X, tile.Y);
                if (!stacks.TryGetValue(cell, out List<MatchTile> stack))
                {
                    stack = new List<MatchTile>();
                    stacks[cell] = stack;
                }

                stack.Add(tile);
            }

            foreach (List<MatchTile> stack in stacks.Values)
            {
                stack.Sort((a, b) => a.Layer.CompareTo(b.Layer));
                for (int height = 0; height < stack.Count; height++)
                {
                    _views[stack[height].Id].SetTarget(BoardPosition(stack[height].X, stack[height].Y, height));
                }
            }

            for (int i = 0; i < _board.Tray.Count; i++)
            {
                _views[_board.Tray[i].Id].SetTarget(TraySlot(i));
            }
        }

        // Tiles on top get the bright material. Covered tiles get the dimmed one, so the picture shows what is tappable.
        // Tray tiles were free when they were tapped, so they keep their bright material.
        private void RefreshTints()
        {
            foreach (KeyValuePair<int, TileView> pair in _views)
            {
                if (!_board.TryGetOnBoard(pair.Key, out MatchTile tile))
                {
                    continue;
                }

                pair.Value.SetMaterial(MaterialFor(tile.Symbol, dimmed: !_board.IsFree(tile.Id)));
            }
        }

        // One material per palette colour and dimmed state, created once and shared by every tile that uses it.
        private Material MaterialFor(int symbol, bool dimmed)
        {
            int paletteIndex = symbol % Palette.Length;
            var key = (paletteIndex, dimmed);
            if (_materials.TryGetValue(key, out Material material))
            {
                return material;
            }

            Color color = Palette[paletteIndex];
            if (dimmed)
            {
                color = new Color(color.r * DimmedBrightness, color.g * DimmedBrightness, color.b * DimmedBrightness, 1f);
            }

            material = new Material(_tileMaterial);
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);   // URP and HDRP lit shaders
            }
            else
            {
                material.color = color;                   // Built-in pipeline
            }

            _materials.Add(key, material);
            return material;
        }

        private Vector3 BoardPosition(int x, int y, int height)
        {
            float centerX = (_spec.GridWidth - 1) * 0.5f;
            float centerZ = (_spec.GridHeight - 1) * 0.5f;
            return new Vector3((x - centerX) * CellSpacing, TileHeight * 0.5f + height * TileHeight, (y - centerZ) * CellSpacing);
        }

        private Vector3 TraySlot(int index)
        {
            float offset = (_rules.TrayCapacity - 1) * 0.5f;
            return new Vector3((index - offset) * TraySpacing, TrayY, TrayZ);
        }
    }
}
