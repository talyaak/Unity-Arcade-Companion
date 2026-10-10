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
        private const float TraySpacing = 1.0f;
        private const float TrayY = 0.5f;
        private const float TrayZ = -4.6f;
        private const float MaxFrameSeconds = 0.1f;
        private const float PopupSeconds = 0.9f;
        private const float DimmedBrightness = 0.45f;
        private const float TileRadius = 0.5f;
        private const float TileHalfHeight = 1f;      // cylinders and capsules are 2 units tall
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

        public MatchBoard Board => _board;

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
        private void HandlePointer()
        {
            Pointer pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame || Camera.main == null)
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

        // Keeps the board, tray and popup room on screen at any aspect ratio. The content's bounding sphere must fit inside
        // the narrower of the vertical and horizontal fields of view. Only the distance changes; the camera's pitch stays.
        private void FitCamera()
        {
            if (_camera == null || _spec == null)
            {
                return;
            }

            _fitWidth = Screen.width;
            _fitHeight = Screen.height;

            float halfBoardX = (_spec.GridWidth - 1) * 0.5f * CellSpacing + TileRadius;
            float halfBoardZ = (_spec.GridHeight - 1) * 0.5f * CellSpacing + TileRadius;
            float halfTrayX = (_rules.TrayCapacity - 1) * 0.5f * TraySpacing + TileRadius;
            float halfX = Mathf.Max(halfBoardX, halfTrayX);
            float minZ = Mathf.Min(-halfBoardZ, TrayZ - TileRadius);
            float maxZ = Mathf.Max(halfBoardZ, TrayZ + TileRadius);
            float maxY = _maxStack * TileHeight + TileHalfHeight + PopupHeadroom;

            var min = new Vector3(-halfX, -TileHalfHeight, minZ);
            var max = new Vector3(halfX, maxY, maxZ);
            Vector3 center = (min + max) * 0.5f;

            // Place the camera on its own view axis so that all eight corners of the content box fit in the frustum.
            // For a corner at (right, up, ahead) from the content centre, the camera distance d must satisfy
            // |right| / (d + ahead) <= tan(halfH) and |up| / (d + ahead) <= tan(halfV).
            float tanV = Mathf.Tan(_camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            float tanH = tanV * _camera.aspect;
            Vector3 right = _camera.transform.right;
            Vector3 up = _camera.transform.up;
            Vector3 forward = _camera.transform.forward;
            float distance = 0f;
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3(
                    (i & 1) == 0 ? min.x : max.x,
                    (i & 2) == 0 ? min.y : max.y,
                    (i & 4) == 0 ? min.z : max.z) - center;
                float ahead = Dot(corner, forward);
                float needH = Mathf.Abs(Dot(corner, right)) / tanH - ahead;
                float needV = Mathf.Abs(Dot(corner, up)) / tanV - ahead;
                distance = Mathf.Max(distance, Mathf.Max(needH, needV));
            }

            _camera.transform.position = center - forward * (distance * FramingMargin);
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

        private void OnGUI()
        {
            if (_board == null)
            {
                return;
            }

            float width = Screen.width;
            float height = Screen.height;
            GUI.Label(new Rect(16f, 16f, width - 32f, 40f),
                $"Stage {_stageIndex + 1}   Score {_board.Score}   Combo {_board.Combo}   " +
                $"Tray {_board.Tray.Count}/{_rules.TrayCapacity}   Left {_board.RemainingOnBoard}");

            if (_popupLeft > 0f)
            {
                GUI.Label(new Rect(16f, 56f, width - 32f, 40f), _popup);
            }

            var center = new Rect((width - 240f) / 2f, height * 0.8f, 240f, 60f);
            if (_board.State == MatchState.Won)
            {
                GUI.Label(new Rect(16f, height * 0.5f, width - 32f, 40f), "Level cleared");
                if (GUI.Button(center, "Next stage"))
                {
                    StartLevel((_stageIndex + 1) % MatchStages.All.Count, (ulong)DateTime.UtcNow.Ticks);
                }
            }
            else if (_board.State == MatchState.Lost)
            {
                GUI.Label(new Rect(16f, height * 0.5f, width - 32f, 40f), "Tray full");
                if (GUI.Button(center, "Retry"))
                {
                    StartLevel(_stageIndex, (ulong)DateTime.UtcNow.Ticks);
                }
            }
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
