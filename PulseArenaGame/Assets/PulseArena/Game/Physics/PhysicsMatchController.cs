using System;
using System.Collections.Generic;
using PulseArena.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PulseArena.Game
{
    /// Physics mode (D14), in its own scene. The tiles are rigid bodies under gravity. This script reports which tiles are
    /// free and shows Core's answers. It never decides a tap: Core does, and a tap on a tile that is not in the latest report
    /// is Blocked. Core also decides the tray, triples, score, and win or lose. The pile is not deterministic, so two runs of
    /// the same level differ in layout; the symbols and counts still come from the seed.
    ///
    /// Free rule (stated in D14): a tile is free when no other tile touches the thin box just above its top face, its linear
    /// speed is under SettledSpeed (0.15 units per second), and its spin is under SettledSpin (1 radian per second).
    public sealed class PhysicsMatchController : MonoBehaviour, IMatchStatus
    {
        public const float SettledSpeed = 0.15f;
        public const float SettledSpin = 1f;
        public const int MaxBodies = 60;

        private const float TileScale = 0.9f;                    // the same tile size as the grid game
        private const float SlabThickness = 0.6f;                // TileMeshes: a slab is 0.6 units thick
        private const float TopOffset = SlabThickness * 0.5f * TileScale;   // from a tile's centre to its top face
        private const float RestGap = 0.12f;                     // the thin box above the top face that counts as "resting on it"
        private const float RestHalfWidth = 0.4f * TileScale;    // narrower than the tile, so a neighbour beside it does not count
        private const float TileRadius = TileScale * 0.5f;
        private const float TileHalfHeight = TileScale * 0.36f;  // the slab's top and the emblem's top, 0.36 units above centre
        private const float CellPitch = 1.15f;                   // spawn cells, the same pitch as the grid game
        private const float WavePitch = 1.0f;                    // height between spawn waves: a tile of one wave cannot touch one of the next
        private const float SpawnDrop = 1.2f;                    // the first wave starts this far above its resting height (was 0.3, a flat grid)
        private const float SpawnJitter = 0.15f;                 // up to this much extra height per tile, so a wave does not drop in step
        private const float SpawnSpacing = 1.3f;                 // centre to centre inside a wave: two tiles with any turn cannot overlap
        private const float SpawnInset = 0.66f;                  // spawn centres stay this far inside the plate edge (a tile's footprint at any turn)
        private const int SpawnTries = 60;                       // candidate places per tile before the wave is full
        private const float WallMargin = 1.5f;                   // the walls stand this far above the highest spawn
        private const float FloorMargin = 0.6f;
        private const float WallHeight = 4f;
        private const float WallThickness = 0.4f;
        private const float PlateThickness = 0.2f;
        private const float WellThickness = 0.2f;
        private const float PlateMargin = 0.3f;
        private const float TraySpacing = 1.05f;                 // the same tray as the grid game
        private const float TrayY = 0.5f;
        private const float TrayZ = -4.6f;
        private const float MaxFrameSeconds = 0.1f;
        private const float DimmedBrightness = 0.45f;
        private const float HitStopSeconds = 0.06f;
        private const float BlockedFlashSeconds = 0.15f;
        private const float LossFlashSeconds = 0.3f;
        private const float NudgeSeconds = 0.15f;
        private const float NudgeAmplitude = 0.05f;
        private const int NudgeCombo = 3;
        private const float ConfettiWaveSeconds = 0.35f;
        private const float FramingMargin = 1.10f;
        private const float HudTopReserve = 0.2f;
        private const float HudBottomReserve = 0.1f;
        private const float HeadroomFraction = 0.2f;
        private const int FreeEvery = 3;                         // frames between free-set checks
        private const float PickDistance = 100f;                 // how far a tap ray reaches
        private const float BodyGloss = 0.45f;                   // Standard shader smoothness, as in the grid game
        private const float EmblemGloss = 0.55f;
        private const float PlateGloss = 0.2f;
        private const float WellGloss = 0.1f;

        private static readonly (int, bool) RedKey = (-1, false);
        private static readonly (int, bool) RedEmblemKey = (-3, false);
        private static readonly (int, bool) PlateKey = (-4, false);
        private static readonly (int, bool) WellKey = (-5, false);
        private static readonly Color RedColor = new Color(0.9f, 0.15f, 0.12f, 1f);
        private static readonly Color EmblemColor = new Color(0.10f, 0.12f, 0.16f, 1f);
        private static readonly Color PlateColor = new Color(0.13f, 0.19f, 0.25f, 1f);
        private static readonly Color WellColor = new Color(0.05f, 0.08f, 0.11f, 1f);

        // The grid game's palette, so both modes show the same symbol colours.
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

        [SerializeField] private int _stageIndex;

        /// Base material for tiles, created by Pulse Arena > Build Physics Scene. Colour variants are copies of it.
        [SerializeField] private Material _tileMaterial;

        private readonly MatchRules _rules = new MatchRules();
        private readonly Dictionary<int, TileView> _views = new Dictionary<int, TileView>();
        private readonly Dictionary<int, Rigidbody> _bodies = new Dictionary<int, Rigidbody>();
        private readonly Dictionary<(int, bool), Material> _materials = new Dictionary<(int, bool), Material>();
        private readonly HashSet<int> _reported = new HashSet<int>();      // the free set Core was last given
        private readonly List<int> _freeNow = new List<int>();
        private readonly List<Vector3> _fitPoints = new List<Vector3>();
        private readonly Collider[] _overlaps = new Collider[16];
        private readonly RaycastHit[] _pickHits = new RaycastHit[16];   // reused by every tap, so a tap does not allocate
        private readonly GameObject[] _walls = new GameObject[4];
        private MatchLevelSpec _spec;
        private MatchBoard _board;
        private double _clock;
        private Camera _camera;
        private Vector3 _cameraBase;
        private Vector3 _cameraForward;
        private int _fitWidth;
        private int _fitHeight;
        private Mesh[] _tileMeshes;
        private Mesh _slabMesh;
        private GameObject _floor;
        private GameObject _floorBody;
        private GameObject _trayWell;
        private Vector3 _floorHalf;
        private Vector3 _wellHalf;
        private readonly List<Vector3> _spawnPoints = new List<Vector3>();
        private readonly List<float> _spawnTurns = new List<float>();
        private int _spawnWaves;
        private float _spawnTopFace;   // the top face of the highest tile at its spawn point
        private int _frame;
        private bool _pointerWasDown;
        private bool _reduceMotion;
        private MatchEffects _effects;
        private MatchPopup _popupView;
        private float _nudgeLeft;
        private int _wavesLeft;
        private float _waveTimer;

        public MatchBoard Board => _board;
        public int StageNumber => _stageIndex + 1;
        public int TrayCapacity => _rules.TrayCapacity;

        /// Reduce Motion: no camera nudge, no hit-stop, no confetti, and no wobble on a blocked tap. Moves and colours stay.
        public bool ReduceMotion => _reduceMotion;

        /// Particles for clears and wins. Read by the verification.
        public MatchEffects Effects => _effects;

        // Verification reads (the game does not use these).
        public int BodyCount => _bodies.Count;
        public int SpawnWaves => _spawnWaves;
        public float SpawnTopFace => _spawnTopFace;
        public int ReportedFreeCount => _reported.Count;
        public bool IsReportedFree(int tileId) => _reported.Contains(tileId);
        public Rigidbody BodyOf(int tileId) => _bodies.TryGetValue(tileId, out Rigidbody body) ? body : null;

        public void ToggleReduceMotion()
        {
            _reduceMotion = !_reduceMotion;
        }

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
                Debug.LogError("PhysicsMatchController needs a Tile Material. Run Pulse Arena > Build Physics Scene.", this);
                enabled = false;
                return;
            }

            _camera = Camera.main;
            _effects = new GameObject("Match effects").AddComponent<MatchEffects>();
            _popupView = new GameObject("Match popup").AddComponent<MatchPopup>();

            // The shared materials and the meshes are made once, here, as in the grid game.
            for (int symbol = 0; symbol < Palette.Length; symbol++)
            {
                MaterialFor(symbol, dimmed: false);
                MaterialFor(symbol, dimmed: true);
            }

            _ = RedMaterial;
            _ = RedEmblemMaterial;
            _ = EmblemMaterial(dimmed: false);
            _ = EmblemMaterial(dimmed: true);
            _tileMeshes = TileMeshes.CreateTiles();
            _slabMesh = TileMeshes.CreateSlab();

            // The floor under the pile (a box collider, so the tiles rest on it), four walls to keep the pile in, and the tray well.
            _floor = CreateSlab("Floor", ColorMaterial(PlateKey, PlateColor, PlateGloss));
            _floorBody = new GameObject("Floor collider");     // a thick, invisible box under the plate, so no tile can fall through
            _floorBody.AddComponent<BoxCollider>();
            for (int w = 0; w < _walls.Length; w++)
            {
                _walls[w] = new GameObject("Wall " + w);
                _walls[w].AddComponent<BoxCollider>();
            }

            _trayWell = CreateSlab("Tray well", ColorMaterial(WellKey, WellColor, WellGloss));
            LayoutTrayWell();
            StartLevel(_stageIndex, (ulong)DateTime.UtcNow.Ticks);
        }

        private void Update()
        {
            if (_board != null && _board.State == MatchState.Playing)
            {
                _clock += FrameTime.Clamp(Time.deltaTime, MaxFrameSeconds);
            }

            if (_board != null && (Screen.width != _fitWidth || Screen.height != _fitHeight))
            {
                FitCamera();
            }

            _frame++;
            if (_board != null && _frame % FreeEvery == 0)
            {
                UpdateFreeSet();
            }

            UpdateCameraNudge();
            UpdateConfetti();
            HandlePointer();
        }

        /// Starts a level: the seed gives the symbols, and the pile is dropped from a spawn layout (not deterministic).
        public void StartLevel(int stageIndex, ulong seed)
        {
            if (stageIndex < 0 || stageIndex >= MatchStages.All.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(stageIndex));
            }

            ClearTiles();
            _stageIndex = stageIndex;
            _spec = MatchStages.All[stageIndex];
            _clock = 0;
            _nudgeLeft = 0f;
            _wavesLeft = 0;

            // Core's tiles come from the grid generator, so the symbol counts are the same multiples of three. Each tile is
            // loose: it has no grid cell, because the free set comes from the physics.
            MatchLevel level = MatchLevelGenerator.Generate(_spec, seed);
            var loose = new List<MatchTile>(level.Tiles.Count);
            foreach (MatchTile tile in level.Tiles)
            {
                loose.Add(MatchTile.Loose(tile.Id, tile.Symbol));
            }

            _board = new MatchBoard(loose, _rules);

            // Nothing is free until the pile settles. This empty report also switches the board to the reported free set.
            _board.ReportFreeTiles(Array.Empty<int>());
            var random = new System.Random(unchecked((int)seed));
            int[] order = ShuffledOrder(loose.Count, random);
            PlanSpawn(loose.Count, random);
            LayoutFloor();
            SpawnPile(loose, order);
            BuildFitPoints();
            FitCamera();
            RefreshTints();
        }

        /// Sends one tap to Core and shows its answer. Returns Core's step, or null when there is no level.
        public MatchStep Tap(int tileId)
        {
            if (_board == null)
            {
                return null;
            }

            // The tray's tiles of this symbol before the tap, so a clear can animate all three (as in the grid game).
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

            int trayBefore = _board.Tray.Count;
            MatchStep step = _board.TapTile(tileId, _clock);
            AudioCues.Play(AudioCue.Tap);

            if (step.Outcome == MatchOutcome.Blocked)
            {
                if (_views.TryGetValue(tileId, out TileView blocked))
                {
                    blocked.Flash(RedMaterial, RedEmblemMaterial, BlockedFlashSeconds);
                    if (!_reduceMotion && _bodies.TryGetValue(tileId, out Rigidbody body))
                    {
                        body.AddTorque(new Vector3(0f, 0.05f, 0f));   // a small wobble; physics, not the grid's shake
                    }
                }

                AudioCues.Play(AudioCue.Blocked);
            }
            else if (step.Outcome == MatchOutcome.Moved || step.Outcome == MatchOutcome.Cleared
                || step.Outcome == MatchOutcome.Won || step.Outcome == MatchOutcome.Lost)
            {
                // The tapped tile leaves the pile. Its body goes kinematic and off, and its view takes the position back.
                LeavePile(tileId);
                LayoutTray();
                if (step.Outcome == MatchOutcome.Moved && _views.TryGetValue(tileId, out TileView moved))
                {
                    moved.Squash();
                    AudioCues.Play(AudioCue.Move);
                }
            }

            foreach (MatchEvent e in step.Events)
            {
                if (e.Kind == MatchEventKind.Cleared)
                {
                    sameSymbolInTray.Add(tileId);
                    float hitStop = _reduceMotion ? 0f : HitStopSeconds;
                    Vector3 popupAt = TraySlot(Math.Min(trayBefore, _rules.TrayCapacity - 1));
                    foreach (int id in sameSymbolInTray)
                    {
                        if (_views.TryGetValue(id, out TileView vanishing))
                        {
                            vanishing.Vanish(hitStop);
                            _effects.Burst(vanishing.transform.position, hitStop);
                            _views.Remove(id);
                        }

                        // The view's GameObject is destroyed when its pop ends, and its body with it. The reference is dropped
                        // here, so a later ClearTiles (Retry, Next stage) does not touch a destroyed body.
                        _bodies.Remove(id);
                    }

                    _popupView.Show($"+{e.Points}", popupAt, FacingCamera());
                    AudioCues.Play(AudioCue.Clear);
                    if (!_reduceMotion && _board.Combo >= NudgeCombo)
                    {
                        _nudgeLeft = NudgeSeconds;
                    }
                }
            }

            if (step.Outcome == MatchOutcome.Won)
            {
                AudioCues.Play(AudioCue.Win);
                if (!_reduceMotion)
                {
                    _wavesLeft = MatchEffects.ConfettiTotal / MatchEffects.ConfettiWaveSize;
                    _waveTimer = 0f;
                }
            }
            else if (step.Outcome == MatchOutcome.Lost)
            {
                AudioCues.Play(AudioCue.Lose);
                foreach (MatchTile inTray in _board.Tray)
                {
                    if (_views.TryGetValue(inTray.Id, out TileView lost))
                    {
                        lost.Flash(RedMaterial, RedEmblemMaterial, LossFlashSeconds);
                        lost.Drop(LossFlashSeconds);
                    }
                }
            }

            RefreshTints();
            return step;
        }

        // Reports the free set to Core when it changes (D14). Only tiles that are still in the pile are checked.
        private void UpdateFreeSet()
        {
            _freeNow.Clear();
            foreach (KeyValuePair<int, Rigidbody> pair in _bodies)
            {
                if (!_board.TryGetOnBoard(pair.Key, out MatchTile _))
                {
                    continue;   // tapped or cleared: not in the pile any more
                }

                if (IsFree(pair.Key, pair.Value))
                {
                    _freeNow.Add(pair.Key);
                }
            }

            if (SameAsReported())
            {
                return;
            }

            _board.ReportFreeTiles(_freeNow);
            _reported.Clear();
            foreach (int id in _freeNow)
            {
                _reported.Add(id);
            }

            RefreshTints();
        }

        private bool SameAsReported()
        {
            if (_freeNow.Count != _reported.Count)
            {
                return false;
            }

            foreach (int id in _freeNow)
            {
                if (!_reported.Contains(id))
                {
                    return false;
                }
            }

            return true;
        }

        // Free (D14): nothing rests on the tile, and it is still. A tile that has left the pile is kinematic, so it is not free.
        private bool IsFree(int tileId, Rigidbody body)
        {
            if (body.isKinematic)
            {
                return false;
            }

            if (body.linearVelocity.magnitude > SettledSpeed)
            {
                return false;
            }

            if (body.angularVelocity.magnitude > SettledSpin)
            {
                return false;
            }

            Vector3 centre = body.position + new Vector3(0f, TopOffset + RestGap * 0.5f, 0f);
            Vector3 half = new Vector3(RestHalfWidth, RestGap * 0.5f, RestHalfWidth);
            int hits = Physics.OverlapBoxNonAlloc(centre, half, _overlaps, Quaternion.identity);
            for (int k = 0; k < hits; k++)
            {
                Rigidbody other = _overlaps[k].attachedRigidbody;
                if (other != null && other != body)
                {
                    return false;   // another tile rests on this one
                }
            }

            return true;
        }

        private void LeavePile(int tileId)
        {
            if (_bodies.TryGetValue(tileId, out Rigidbody body))
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.isKinematic = true;
                body.GetComponent<Collider>().enabled = false;
            }

            if (_views.TryGetValue(tileId, out TileView view))
            {
                view.ReleaseFromPhysics();
            }
        }

        private void LayoutTray()
        {
            for (int i = 0; i < _board.Tray.Count; i++)
            {
                if (_views.TryGetValue(_board.Tray[i].Id, out TileView view))
                {
                    view.SetTarget(TraySlot(i), arc: true);
                }
            }
        }

        private void ClearTiles()
        {
            // The old bodies are switched off first. Their replacements spawn at the same places, so an old body that is still
            // in the physics scene would overlap a new one and push it through the floor.
            foreach (KeyValuePair<int, Rigidbody> pair in _bodies)
            {
                if (pair.Value == null)
                {
                    continue;   // destroyed (a Unity null): there is nothing left to switch off
                }

                pair.Value.isKinematic = true;
                pair.Value.GetComponent<Collider>().enabled = false;
            }

            foreach (TileView view in _views.Values)
            {
                if (view != null)
                {
                    Destroy(view.gameObject);
                }
            }

            _views.Clear();
            _bodies.Clear();
            _reported.Clear();
            _freeNow.Clear();
        }

        // Which tile goes to which spawn place. Core's tiles come in symbol order, so without this, tiles of one symbol would be
        // neighbours in the pile and the levels would be too easy.
        private static int[] ShuffledOrder(int count, System.Random random)
        {
            int[] order = new int[count];
            for (int i = 0; i < count; i++)
            {
                order[i] = i;
            }

            for (int i = count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }

            return order;
        }

        // The spawn (D14 feel gap): each tile starts at a random place over the plate, with a random turn. Tiles of one wave keep
        // SpawnSpacing apart, so none starts inside another. A wave that is full starts a higher one, so the pile falls as a heap
        // and not as a flat grid. The plate is sized as LayoutFloor sizes it, because this runs before the floor is laid out.
        private void PlanSpawn(int count, System.Random random)
        {
            _spawnPoints.Clear();
            _spawnTurns.Clear();
            float width = _spec.GridWidth * CellPitch + 2f * FloorMargin;
            float depth = _spec.GridHeight * CellPitch + 2f * FloorMargin;
            float halfX = width * 0.5f - SpawnInset;
            float halfZ = depth * 0.5f - SpawnInset;
            float highest = TopOffset + SpawnDrop;
            int placed = 0;
            int wave = 0;
            while (placed < count)
            {
                int first = _spawnPoints.Count;
                float baseY = TopOffset + SpawnDrop + wave * WavePitch;
                while (placed < count)
                {
                    bool found = false;
                    float x = 0f;
                    float z = 0f;
                    for (int attempt = 0; attempt < SpawnTries && !found; attempt++)
                    {
                        x = (float)(random.NextDouble() * 2.0 - 1.0) * halfX;
                        z = (float)(random.NextDouble() * 2.0 - 1.0) * halfZ;
                        found = true;
                        for (int k = first; k < _spawnPoints.Count; k++)
                        {
                            float dx = _spawnPoints[k].x - x;
                            float dz = _spawnPoints[k].z - z;
                            if (dx * dx + dz * dz < SpawnSpacing * SpawnSpacing)
                            {
                                found = false;
                                break;
                            }
                        }
                    }

                    if (!found)
                    {
                        break;   // this wave is full: the next tile starts the next wave
                    }

                    float y = baseY + (float)random.NextDouble() * SpawnJitter;
                    _spawnPoints.Add(new Vector3(x, y, z));
                    _spawnTurns.Add((float)(random.NextDouble() * 360.0));
                    highest = Math.Max(highest, y);
                    placed++;
                }

                wave++;
            }

            _spawnWaves = wave;
            _spawnTopFace = highest + TopOffset;
        }

        private void SpawnPile(IReadOnlyList<MatchTile> tiles, int[] order)
        {
            int count = tiles.Count;
            if (count > MaxBodies)
            {
                throw new InvalidOperationException($"Physics mode allows at most {MaxBodies} rigid bodies; this level has {count}.");
            }

            for (int i = 0; i < count; i++)
            {
                CreateTile(tiles[order[i]].Id, _spawnPoints[i], _spawnTurns[i]);
            }
        }

        private void CreateTile(int tileId, Vector3 position, float turn)
        {
            MatchTile tile = LooseTile(tileId);
            // The object is placed before any collider or body is added. A collider added at the origin (the floor's surface)
            // and moved afterwards gave the physics a sweep through the floor, which launched some tiles.
            var go = new GameObject("Tile " + tileId);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, turn, 0f);
            go.transform.localScale = new Vector3(TileScale, TileScale, TileScale);
            go.AddComponent<MeshFilter>().sharedMesh = _tileMeshes[tile.Symbol % _tileMeshes.Length];
            go.AddComponent<MeshRenderer>();
            go.AddComponent<BoxCollider>().size = new Vector3(1f, SlabThickness, 1f);   // the slab: the pile stacks on its top faces
            var body = go.AddComponent<Rigidbody>();
            body.mass = 1f;
            body.linearDamping = 0.05f;
            body.angularDamping = 0.1f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;   // a fast tile cannot tunnel through the floor
            TileView view = go.AddComponent<TileView>();
            view.Init(tileId, position);
            view.SetMaterials(MaterialFor(tile.Symbol, dimmed: true), EmblemMaterial(dimmed: true));   // covered until reported
            view.AttachToPhysics();
            _views.Add(tileId, view);
            _bodies.Add(tileId, body);
        }

        // The symbol for a tile id, from Core's board. Core keeps every tile, so a spawn can always look it up.
        private MatchTile LooseTile(int tileId)
        {
            _board.TryGetOnBoard(tileId, out MatchTile tile);
            return tile;
        }

        private void LayoutFloor()
        {
            float width = _spec.GridWidth * CellPitch + 2f * FloorMargin;
            float depth = _spec.GridHeight * CellPitch + 2f * FloorMargin;
            _floorHalf = new Vector3(width * 0.5f, PlateThickness * 0.5f, depth * 0.5f);
            _floor.transform.localScale = new Vector3(width, PlateThickness / SlabThickness, depth);
            _floor.transform.position = new Vector3(0f, -PlateThickness * 0.5f, 0f);
            _floorBody.transform.localScale = new Vector3(width, 1f, depth);   // one unit thick, its top at y = 0
            _floorBody.transform.position = new Vector3(0f, -0.5f, 0f);

            float wallX = width * 0.5f + WallThickness * 0.5f;
            float wallZ = depth * 0.5f + WallThickness * 0.5f;
            float wallHeight = Math.Max(WallHeight, _spawnTopFace + WallMargin);   // taller for a higher spawn, so no tile leaves over a wall
            PlaceWall(0, new Vector3(wallX, wallHeight * 0.5f, 0f), new Vector3(WallThickness, wallHeight, depth + 2f * WallThickness));
            PlaceWall(1, new Vector3(-wallX, wallHeight * 0.5f, 0f), new Vector3(WallThickness, wallHeight, depth + 2f * WallThickness));
            PlaceWall(2, new Vector3(0f, wallHeight * 0.5f, wallZ), new Vector3(width + 2f * WallThickness, wallHeight, WallThickness));
            PlaceWall(3, new Vector3(0f, wallHeight * 0.5f, -wallZ), new Vector3(width + 2f * WallThickness, wallHeight, WallThickness));
        }

        private void PlaceWall(int index, Vector3 position, Vector3 scale)
        {
            _walls[index].transform.position = position;
            _walls[index].transform.localScale = scale;
        }

        private void LayoutTrayWell()
        {
            float trayWidth = (_rules.TrayCapacity - 1) * TraySpacing + TileScale + 2f * PlateMargin;
            float trayDepth = TileScale + 2f * PlateMargin;
            _wellHalf = new Vector3(trayWidth * 0.5f, WellThickness * 0.5f, trayDepth * 0.5f);
            _trayWell.transform.localScale = new Vector3(trayWidth, WellThickness / SlabThickness, trayDepth);
            _trayWell.transform.position = new Vector3(0f, WellThickness * 0.5f, TrayZ);
        }

        private GameObject CreateSlab(string name, Material material)
        {
            var go = new GameObject(name);
            go.AddComponent<MeshFilter>().sharedMesh = _slabMesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

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

            // The walls and the floor have colliders but no tile. Take the nearest hit that is a tile, so an invisible wall in
            // front of a tile does not swallow the tap.
            TileView picked = null;
            float nearest = float.MaxValue;
            int count = Physics.RaycastNonAlloc(ray, _pickHits, PickDistance);
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = _pickHits[i];
                if (hit.distance >= nearest)
                {
                    continue;
                }

                TileView view = hit.collider.GetComponent<TileView>();
                if (view != null)
                {
                    picked = view;
                    nearest = hit.distance;
                }
            }

            if (picked != null)
            {
                Tap(picked.TileId);
            }
        }

        private void UpdateCameraNudge()
        {
            if (_camera == null || _nudgeLeft <= 0f)
            {
                return;
            }

            _nudgeLeft -= Time.deltaTime;
            if (_nudgeLeft <= 0f)
            {
                _camera.transform.position = _cameraBase;
                return;
            }

            float strength = _nudgeLeft / NudgeSeconds;
            _camera.transform.position = _cameraBase + _cameraForward * (NudgeAmplitude * strength * strength);
        }

        // Confetti: three waves of 20 (60 in all), 0.35 s apart, over the floor. Reduce Motion stops them.
        private void UpdateConfetti()
        {
            if (_wavesLeft <= 0 || _effects == null)
            {
                return;
            }

            if (_reduceMotion)
            {
                _wavesLeft = 0;
                return;
            }

            _waveTimer -= Time.deltaTime;
            if (_waveTimer > 0f)
            {
                return;
            }

            _wavesLeft--;
            _waveTimer = ConfettiWaveSeconds;
            _effects.ReleaseConfetti(_floorHalf.x, _floorHalf.z, _spawnTopFace + 2f);
        }

        private Quaternion FacingCamera()
        {
            return _camera != null ? _camera.transform.rotation : Quaternion.identity;
        }

        // Frames the floor, the pile and the tray. It is the grid game's framing, with the HUD bands left out of the board's
        // band (D13), so the board sits between the HUD blocks in both aspects.
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
            float tanVFree = tanV * (1f - HudTopReserve - HudBottomReserve);
            Vector3 right = _camera.transform.right;
            Vector3 up = _camera.transform.up;
            Vector3 forward = _camera.transform.forward;

            float distance = 0f;
            foreach (Vector3 p in _fitPoints)
            {
                Vector3 rel = p - center;
                float ahead = Dot(rel, forward);
                distance = Mathf.Max(distance, Mathf.Abs(Dot(rel, right)) / tanH - ahead);
                distance = Mathf.Max(distance, Mathf.Abs(Dot(rel, up)) / tanVFree - ahead);
            }

            float d = distance * FramingMargin;
            float screenCentreY = HudBottomReserve - HudTopReserve;
            _cameraBase = center - forward * d + up * (-screenCentreY * d * tanV);
            _cameraForward = forward;
            _camera.transform.position = _cameraBase;
        }

        private void BuildFitPoints()
        {
            _fitPoints.Clear();
            AddExtents(new Vector3(0f, -PlateThickness * 0.5f, 0f), _floorHalf);
            float pileTop = _spawnTopFace;
            AddExtents(new Vector3(0f, pileTop * 0.5f, 0f), new Vector3(_floorHalf.x, pileTop * 0.5f, _floorHalf.z));
            for (int i = 0; i < _rules.TrayCapacity; i++)
            {
                AddBox(TraySlot(i));
            }

            AddExtents(new Vector3(0f, WellThickness * 0.5f, TrayZ), _wellHalf);
            _fitPoints.Add(new Vector3(0f, pileTop * (1f + HeadroomFraction), 0f));
        }

        private void AddBox(Vector3 center)
        {
            AddExtents(center, new Vector3(TileRadius, TileHalfHeight, TileRadius));
        }

        // The eight corners of a box with the given half sizes, for the framing.
        private void AddExtents(Vector3 centre, Vector3 half)
        {
            for (int i = 0; i < 8; i++)
            {
                _fitPoints.Add(new Vector3(
                    centre.x + ((i & 1) == 0 ? -half.x : half.x),
                    centre.y + ((i & 2) == 0 ? -half.y : half.y),
                    centre.z + ((i & 4) == 0 ? -half.z : half.z)));
            }
        }

        // Tiles in the pile with nothing resting on them show the bright colour; the rest show the dimmed one, so the picture
        // shows what is tappable. Tray tiles were free when they were tapped, so they keep the bright colour.
        private void RefreshTints()
        {
            foreach (KeyValuePair<int, TileView> pair in _views)
            {
                if (!_board.TryGetOnBoard(pair.Key, out MatchTile tile))
                {
                    continue;
                }

                bool dimmed = !_board.IsFree(pair.Key);
                pair.Value.SetMaterials(MaterialFor(tile.Symbol, dimmed), EmblemMaterial(dimmed));
            }
        }

        private Material RedMaterial => ColorMaterial(RedKey, RedColor);

        private Material RedEmblemMaterial => ColorMaterial(RedEmblemKey, RedColor, EmblemGloss);

        private Material EmblemMaterial(bool dimmed)
        {
            Color color = EmblemColor;
            if (dimmed)
            {
                color = new Color(color.r * DimmedBrightness, color.g * DimmedBrightness, color.b * DimmedBrightness, 1f);
            }

            return ColorMaterial((-2, dimmed), color, EmblemGloss);
        }

        private Material MaterialFor(int symbol, bool dimmed)
        {
            int paletteIndex = symbol % Palette.Length;
            Color color = Palette[paletteIndex];
            if (dimmed)
            {
                color = new Color(color.r * DimmedBrightness, color.g * DimmedBrightness, color.b * DimmedBrightness, 1f);
            }

            return ColorMaterial((paletteIndex, dimmed), color);
        }

        // One material per key, created once and shared by every tile that uses it (D3).
        private Material ColorMaterial((int, bool) key, Color color, float glossiness = BodyGloss)
        {
            if (_materials.TryGetValue(key, out Material material))
            {
                return material;
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

            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", glossiness);
            }

            _materials.Add(key, material);
            return material;
        }

        private Vector3 TraySlot(int index)
        {
            float offset = (_rules.TrayCapacity - 1) * 0.5f;
            return new Vector3((index - offset) * TraySpacing, TrayY, TrayZ);
        }

        private static float Dot(Vector3 a, Vector3 b)
        {
            return a.x * b.x + a.y * b.y + a.z * b.z;
        }

        private void OnDestroy()
        {
            foreach (Material material in _materials.Values)
            {
                Destroy(material);
            }

            _materials.Clear();

            if (_tileMeshes != null)
            {
                foreach (Mesh mesh in _tileMeshes)
                {
                    Destroy(mesh);
                }
            }

            if (_slabMesh != null)
            {
                Destroy(_slabMesh);
            }
        }
    }
}
