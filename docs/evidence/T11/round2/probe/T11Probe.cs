using System;
using System.Collections.Generic;
using System.IO;
using PulseArena.Core;
using PulseArena.Game;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.UIElements;

/// Temporary T11 probe (physics mode, D14). The text copy that was run is in docs/evidence/T11/probe/. It lives in
/// Assets/Temp, which git ignores, so it is not committed.
/// -t11-play plays one game in the physics scene. It waits for the pile to settle and checks the reported free set against a
/// physical ray test, taps a covered tile (expected Blocked), then plays with one policy until Won or Lost. Then it restarts the
/// way the HUD does (Next stage after a win, Retry after a loss), waits for that pile to settle, and taps one free tile.
/// Options: -t11-stage2 (stage 2, 21 tiles; otherwise stage 1, 12 tiles); -t11-seed N (start seed, default 20261011);
/// -t11-allsymbols (the first seed from the start seed on whose layout uses every symbol, checked with Core's generator);
/// -t11-avoid (the free tile whose symbol has the fewest tiles in the tray); -t11-careless (a random free tile); by default
/// greedy (a free tile whose symbol is already in the tray); -t11-reduce (Reduce Motion on before the game);
/// -t11-shot-falling, -t11-shot-pile, -t11-shot-end NAME (screenshots, written next to the exe).
/// Results go to t11-play.txt next to the exe. The probe does not allocate in the play loop: tap data are stored as ints and the
/// text is built after the game. The frames with a screenshot or a probe measurement are not sampled for GC.
public sealed class T11Probe : MonoBehaviour
{
    private const int MaxTaps = 150;
    private const int Samples = 4096;
    private const int SymbolSlots = 8;
    private const int SeedSearchLimit = 2000;
    private const ulong DefaultSeed = 20261011UL;

    private enum Phase
    {
        Wait,
        Settling,
        CoveredTap,
        CoveredCheck,
        Play,
        EndWait,
        Restart,
        RestartSettle,
        Finish
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        string[] args = Environment.GetCommandLineArgs();
        if (Array.IndexOf(args, "-t11-play") < 0)
        {
            return;
        }

        var go = new GameObject("T11Probe");
        UnityEngine.Object.DontDestroyOnLoad(go);
        T11Probe probe = go.AddComponent<T11Probe>();
        probe._stage = Array.IndexOf(args, "-t11-stage2") >= 0 ? 1 : 0;
        probe._seed = ParseSeed(ArgValue(args, "-t11-seed")) ?? DefaultSeed;
        probe._allSymbols = Array.IndexOf(args, "-t11-allsymbols") >= 0;
        probe._careless = Array.IndexOf(args, "-t11-careless") >= 0;
        probe._reduce = Array.IndexOf(args, "-t11-reduce") >= 0;
        probe._avoid = Array.IndexOf(args, "-t11-avoid") >= 0;
        probe._shotFalling = ArgValue(args, "-t11-shot-falling");
        probe._shotPile = ArgValue(args, "-t11-shot-pile");
        probe._shotEnd = ArgValue(args, "-t11-shot-end");
    }

    private static string ArgValue(string[] args, string name)
    {
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    private static ulong? ParseSeed(string text)
    {
        return text != null && ulong.TryParse(text, out ulong seed) ? seed : (ulong?)null;
    }

    private int _stage;
    private ulong _seed;
    private ulong _chosenSeed;
    private int _seedSkipped;
    private bool _allSymbols;
    private bool _careless;
    private bool _reduce;
    private bool _avoid;
    private string _shotFalling;
    private string _shotPile;
    private string _shotEnd;
    private readonly System.Random _random = new System.Random(7);
    private readonly List<string> _lines = new List<string>();
    private readonly List<string> _exceptionText = new List<string>();
    private readonly List<double> _gcSettle = new List<double>(Samples);
    private readonly List<double> _gcPlay = new List<double>(Samples);
    private readonly List<double> _gcRestart = new List<double>(Samples);
    private readonly List<int> _freeScratch = new List<int>(64);
    private readonly int[] _levelSymbols = new int[SymbolSlots];
    private readonly int[] _tapId = new int[MaxTaps];
    private readonly int[] _tapSymbol = new int[MaxTaps];
    private readonly int[] _tapOutcome = new int[MaxTaps];
    private readonly int[] _tapTrayBefore = new int[MaxTaps];
    private readonly int[] _tapTrayAfter = new int[MaxTaps];
    private readonly int[] _tapFree = new int[MaxTaps];
    private readonly int[] _tapState = new int[MaxTaps];
    private ProfilerRecorder _gc;
    private PhysicsMatchController _pc;
    private Phase _phase;
    private int _frame;
    private int _startFrame;
    private int _phaseFrame;
    private int _nextTapAt;
    private int _gcSkip;
    private int _quitAt;
    private int _taps;
    private int _blocked;
    private int _noFreeFrames;
    private string _endReason = "";
    private int _totalTiles;
    private int _startBodies;
    private int _levelDistinct;
    private int _materialsBefore;
    private int _materialsAfterRestart;
    private bool _settled;
    private float _settleSeconds;
    private float _startTime;
    private float _playStartTime;
    private int _reportedFree;
    private int _reportedCovered;
    private int _rayCovered;
    private int _disagreements;
    private int _spawnWaves;
    private float _spawnTopFace;
    private int _spawnOverlapPairs;
    private int _tilesAboveFloor;
    private float _topFaceMax;
    private float _spreadMax;
    private int _contactDisagreements;
    private int _contactDisagreements2;
    private int _denseDisagreements;
    private readonly List<string> _mismatches = new List<string>();
    private double _maxPlay;
    private bool _maxPlayTap;
    private bool _maxPlayAfterResult;
    private float _maxPlaySeconds;
    private float _resultTime = -1f;
    private bool _tapThisFrame;
    private int _coveredId = -1;
    private int _coveredOutcome;
    private Vector3 _coveredStart;
    private float _coveredMoved;
    private string _restartKind = "";
    private float _restartStart;
    private bool _restartSettled;
    private float _restartSettleSeconds;
    private int _restartStage;
    private int _restartTotal;
    private int _restartBodies;
    private int _restartState;
    private int _restartReportedFree;
    private int _restartTapOutcome = -1;
    private int _exceptions;

    private void Awake()
    {
        Application.logMessageReceived += OnLog;
    }

    private void OnDestroy()
    {
        Application.logMessageReceived -= OnLog;
    }

    private void Update()
    {
        _frame++;
        if (_frame == 2)
        {
            ChooseSeed();
        }

        if (_phase == Phase.Finish)
        {
            if (_frame >= _quitAt)
            {
                Finish();
                Application.Quit();
            }

            return;
        }

        if (_pc == null)
        {
            _pc = UnityEngine.Object.FindAnyObjectByType<PhysicsMatchController>();
        }

        if (_pc == null || _pc.Board == null)
        {
            return;
        }

        if (!_gc.Valid)
        {
            _gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1);
        }

        if (_resultTime < 0f && _pc.Board.State != MatchState.Playing)
        {
            _resultTime = Time.time;
        }

        SampleGc();

        switch (_phase)
        {
            case Phase.Wait:
                if (_frame < 60)
                {
                    return;
                }

                _pc.StartLevel(_stage, _chosenSeed);
                if (_reduce)
                {
                    _pc.ToggleReduceMotion();
                }

                _startFrame = _frame;
                _startTime = Time.time;
                _phase = Phase.Settling;
                return;

            case Phase.Settling:
                StepSettling();
                return;

            case Phase.CoveredTap:
                StepCoveredTap();
                return;

            case Phase.CoveredCheck:
                if (_frame - _phaseFrame < 30)
                {
                    return;
                }

                _coveredMoved = (_pc.BodyOf(_coveredId).position - _coveredStart).magnitude;
                StartPlay();
                return;

            case Phase.Play:
                StepPlay();
                return;

            case Phase.EndWait:
                if (_frame - _phaseFrame < 60)
                {
                    return;
                }

                Report();
                _restartKind = _pc.Board.State == MatchState.Won ? "NextStage" : "Retry";
                _phase = Phase.Restart;
                return;

            case Phase.Restart:
                _restartStart = Time.time;
                if (_restartKind == "NextStage")
                {
                    _pc.NextStage();
                }
                else
                {
                    _pc.Retry();
                }

                _phase = Phase.RestartSettle;
                _phaseFrame = _frame;
                return;

            case Phase.RestartSettle:
                StepRestartSettle();
                return;
        }
    }

    // Core's generator is pure, so the layouts can be checked before the game starts. The search runs only with -t11-allsymbols.
    private void ChooseSeed()
    {
        MatchLevelSpec spec = MatchStages.All[_stage];
        ulong seed = _seed;
        for (int attempt = 0; attempt < SeedSearchLimit; attempt++)
        {
            if (!_allSymbols || DistinctSymbols(MatchLevelGenerator.Generate(spec, seed)) == spec.SymbolCount)
            {
                _chosenSeed = seed;
                _seedSkipped = attempt;
                return;
            }

            seed++;
        }

        _chosenSeed = _seed;
        _seedSkipped = -1;   // no layout in the search range uses every symbol
    }

    private static int DistinctSymbols(MatchLevel level)
    {
        var used = new HashSet<int>();
        foreach (MatchTile tile in level.Tiles)
        {
            used.Add(tile.Symbol);
        }

        return used.Count;
    }

    // One reading per frame: ProfilerRecorder.LastValue is the frame before this one. The phase picks the list. A frame with a
    // probe measurement or a screenshot is skipped (_gcSkip), so the probe's own work is not counted in the game's figures.
    private void SampleGc()
    {
        bool sampledHadTap = _tapThisFrame;   // the frame being read had a tap (the flag is set in that frame)
        _tapThisFrame = false;
        List<double> list;
        if (_phase == Phase.Settling)
        {
            list = _gcSettle;
        }
        else if (_phase == Phase.CoveredTap || _phase == Phase.CoveredCheck || _phase == Phase.Play || _phase == Phase.EndWait)
        {
            list = _gcPlay;
        }
        else if (_phase == Phase.RestartSettle)
        {
            list = _gcRestart;
        }
        else
        {
            return;
        }

        if (_gcSkip > 0)
        {
            _gcSkip--;
            return;
        }

        double value = _gc.LastValue;
        list.Add(value);
        if (list == _gcPlay && value > _maxPlay)
        {
            _maxPlay = value;
            _maxPlayTap = sampledHadTap;
            _maxPlayAfterResult = _pc.Board.State != MatchState.Playing;
            _maxPlaySeconds = _resultTime < 0f ? -1f : Time.time - _resultTime;
        }
    }

    private void StepSettling()
    {
        int since = _frame - _startFrame;
        if (since == 1)
        {
            RecordLevelStart();
            CountSpawnOverlaps();
            _spawnWaves = _pc.SpawnWaves;
            _spawnTopFace = _pc.SpawnTopFace;
            _materialsBefore = Resources.FindObjectsOfTypeAll<Material>().Length;
            _gcSkip = 1;
        }

        if (since == 25 && _shotFalling != null)
        {
            ScreenCapture.CaptureScreenshot(Path.Combine(Dir(), _shotFalling));
            _gcSkip = 2;
        }

        if (since % 10 != 0)
        {
            return;
        }

        bool settled = AllSleeping();
        if (!settled && since < 1500)
        {
            return;
        }

        _settled = settled;
        _settleSeconds = Time.time - _startTime;
        if (_shotPile != null)
        {
            ScreenCapture.CaptureScreenshot(Path.Combine(Dir(), _shotPile));
            _gcSkip = 2;
        }

        TruthCheck();
        _phase = Phase.CoveredTap;
        _phaseFrame = _frame;
    }

    private void StepCoveredTap()
    {
        if (_frame - _phaseFrame < 2)
        {
            return;
        }

        int covered = FirstTile(false);
        if (covered < 0)
        {
            StartPlay();
            return;
        }

        _coveredId = covered;
        _coveredStart = _pc.BodyOf(covered).position;
        _coveredOutcome = (int)_pc.Tap(covered).Outcome;
        _tapThisFrame = true;
        _phase = Phase.CoveredCheck;
        _phaseFrame = _frame;
    }

    private void StartPlay()
    {
        _phase = Phase.Play;
        _playStartTime = Time.time;
        _nextTapAt = _frame;
    }

    private void StepPlay()
    {
        if (_frame < _nextTapAt)
        {
            return;
        }

        _nextTapAt = _frame + 4;
        if (_pc.Board.State != MatchState.Playing || _taps >= MaxTaps || Time.time - _playStartTime > 150f)
        {
            _endReason = _pc.Board.State != MatchState.Playing ? "state" : _taps >= MaxTaps ? "tapCap" : "timeCap";
            EnterEndWait();
            return;
        }

        int next = ChooseFree();
        if (next < 0)
        {
            _noFreeFrames++;
            if (_noFreeFrames > 300)
            {
                _endReason = "noFreeTiles";
                EnterEndWait();
            }

            return;
        }

        _noFreeFrames = 0;
        int symbol = _pc.Board.TryGetOnBoard(next, out MatchTile chosen) ? chosen.Symbol : -1;
        int trayBefore = _pc.Board.Tray.Count;
        MatchStep step = _pc.Tap(next);
        _tapThisFrame = true;
        if (step.Outcome == MatchOutcome.Blocked)
        {
            _blocked++;
        }

        _tapId[_taps] = next;
        _tapSymbol[_taps] = symbol;
        _tapOutcome[_taps] = (int)step.Outcome;
        _tapTrayBefore[_taps] = trayBefore;
        _tapTrayAfter[_taps] = _pc.Board.Tray.Count;
        _tapFree[_taps] = _pc.ReportedFreeCount;
        _tapState[_taps] = (int)_pc.Board.State;
        _taps++;
    }

    private void EnterEndWait()
    {
        _phase = Phase.EndWait;
        _phaseFrame = _frame;
    }

    private void StepRestartSettle()
    {
        int since = _frame - _phaseFrame;
        if (since % 10 != 0)
        {
            return;
        }

        bool settled = AllSleeping();
        if (!settled && since < 1500)
        {
            return;
        }

        _restartSettled = settled;
        _restartSettleSeconds = Time.time - _restartStart;
        _restartStage = _pc.StageNumber;
        _restartTotal = _pc.Board.TotalTiles;
        _restartBodies = _pc.BodyCount;
        _restartState = (int)_pc.Board.State;
        _restartReportedFree = _pc.ReportedFreeCount;
        int next = ChooseFree();
        if (next >= 0)
        {
            _restartTapOutcome = (int)_pc.Tap(next).Outcome;
        }

        _materialsAfterRestart = Resources.FindObjectsOfTypeAll<Material>().Length;
        _phase = Phase.Finish;
        _quitAt = _frame + 10;
    }

    private bool AllSleeping()
    {
        for (int id = 0; id < _pc.Board.TotalTiles; id++)
        {
            if (!_pc.Board.TryGetOnBoard(id, out MatchTile _))
            {
                continue;
            }

            Rigidbody body = _pc.BodyOf(id);
            if (body != null && !body.IsSleeping())
            {
                return false;
            }
        }

        return true;
    }

    private void RecordLevelStart()
    {
        _totalTiles = _pc.Board.TotalTiles;
        _startBodies = _pc.BodyCount;
        Array.Clear(_levelSymbols, 0, SymbolSlots);
        for (int id = 0; id < _totalTiles; id++)
        {
            if (_pc.Board.TryGetOnBoard(id, out MatchTile tile))
            {
                _levelSymbols[tile.Symbol]++;
            }
        }

        _levelDistinct = 0;
        for (int s = 0; s < SymbolSlots; s++)
        {
            if (_levelSymbols[s] > 0)
            {
                _levelDistinct++;
            }
        }
    }

    // The gap between this tile's top face and the bottom face of the nearest tile above it (within a tile's width sideways).
    // Negative means overlap. -1 means nothing is above it.
    private float NearestGapAbove(int id, Rigidbody body)
    {
        float best = float.MaxValue;
        float top = body.position.y + 0.27f;
        for (int other = 0; other < _pc.Board.TotalTiles; other++)
        {
            if (other == id || !_pc.Board.TryGetOnBoard(other, out MatchTile _))
            {
                continue;
            }

            Rigidbody b = _pc.BodyOf(other);
            if (b.position.y <= body.position.y
                || Mathf.Abs(b.position.x - body.position.x) > 0.9f || Mathf.Abs(b.position.z - body.position.z) > 0.9f)
            {
                continue;
            }

            best = Mathf.Min(best, b.position.y - 0.27f - top);
        }

        return best == float.MaxValue ? -1f : best;
    }

    // Two tiles overlap at spawn when they are less than a tile apart in height and their bounding circles (radius 0.636) touch.
    // Counted once, on the first frame of the level, when the tiles are still where they spawned.
    private void CountSpawnOverlaps()
    {
        int pairs = 0;
        int n = _pc.Board.TotalTiles;
        for (int a = 0; a < n; a++)
        {
            if (!_pc.Board.TryGetOnBoard(a, out MatchTile _))
            {
                continue;
            }

            Vector3 pa = _pc.BodyOf(a).position;
            for (int b = a + 1; b < n; b++)
            {
                if (!_pc.Board.TryGetOnBoard(b, out MatchTile _))
                {
                    continue;
                }

                Vector3 pb = _pc.BodyOf(b).position;
                float dx = pa.x - pb.x;
                float dz = pa.z - pb.z;
                if (Mathf.Abs(pa.y - pb.y) < 0.6f && dx * dx + dz * dz < 1.2728f * 1.2728f)
                {
                    pairs++;
                }
            }
        }

        _spawnOverlapPairs = pairs;
    }

    // Compares the reported free set with a physical test: a ray up from each tile's top face. A disagreement is a mismatch.
    private void TruthCheck()
    {
        _reportedFree = 0;
        _reportedCovered = 0;
        _rayCovered = 0;
        _disagreements = 0;
        _tilesAboveFloor = 0;
        _topFaceMax = 0f;
        _spreadMax = 0f;
        _contactDisagreements = 0;
        _contactDisagreements2 = 0;
        _denseDisagreements = 0;
        _mismatches.Clear();
        for (int id = 0; id < _pc.Board.TotalTiles; id++)
        {
            if (!_pc.Board.TryGetOnBoard(id, out MatchTile _))
            {
                continue;
            }

            Rigidbody body = _pc.BodyOf(id);
            bool reported = _pc.IsReportedFree(id);
            Vector3 p = body.position;
            if (p.y > 0.7f)
            {
                _tilesAboveFloor++;
            }

            _topFaceMax = Mathf.Max(_topFaceMax, p.y + 0.27f);
            _spreadMax = Mathf.Max(_spreadMax, Mathf.Sqrt(p.x * p.x + p.z * p.z));
            Vector3 origin = body.position + Vector3.up * (0.27f + 0.02f);
            bool covered = Physics.Raycast(origin, Vector3.up, out RaycastHit hit, 1.0f)
                && hit.collider.attachedRigidbody != null && hit.collider.attachedRigidbody != body;
            if (reported)
            {
                _reportedFree++;
            }
            else
            {
                _reportedCovered++;
            }

            if (covered)
            {
                _rayCovered++;
            }

            if (covered == reported)
            {
                _disagreements++;
            }

            // Nine rays across the top face, each 0.1 long from just above the face: a touch anywhere on the top face is covered.
            bool contact = false;
            for (int ix = -1; ix <= 1 && !contact; ix++)
            {
                for (int iz = -1; iz <= 1 && !contact; iz++)
                {
                    Vector3 o = body.position + new Vector3(ix * 0.33f, 0.27f + 0.02f, iz * 0.33f);
                    if (Physics.Raycast(o, Vector3.up, out RaycastHit h9, 0.1f)
                        && h9.collider.attachedRigidbody != null && h9.collider.attachedRigidbody != body)
                    {
                        contact = true;
                    }
                }
            }

            if (contact == reported)
            {
                _contactDisagreements++;
            }

            // The same nine rays, but starting just inside the top face. A ray does not hit a collider it starts inside, so a tile
            // resting on this one (its bottom within 0.02 of the face) is missed by the rays above, and seen from inside.
            bool contactInside = false;
            for (int ix = -1; ix <= 1 && !contactInside; ix++)
            {
                for (int iz = -1; iz <= 1 && !contactInside; iz++)
                {
                    Vector3 o = body.position + new Vector3(ix * 0.33f, 0.27f - 0.01f, iz * 0.33f);
                    if (Physics.Raycast(o, Vector3.up, out RaycastHit h10, 0.13f)
                        && h10.collider.attachedRigidbody != null && h10.collider.attachedRigidbody != body)
                    {
                        contactInside = true;
                    }
                }
            }

            if (contactInside == reported)
            {
                _contactDisagreements2++;
            }

            // The dense check: 49 rays, every 0.12 across the whole box footprint (the box is 0.72 wide), from just inside the face.
            // Each ray is 0.13 long, which covers the box's 0.12 height.
            bool dense = false;
            for (int ix = -3; ix <= 3 && !dense; ix++)
            {
                for (int iz = -3; iz <= 3 && !dense; iz++)
                {
                    Vector3 o = body.position + new Vector3(ix * 0.12f, 0.27f - 0.01f, iz * 0.12f);
                    if (Physics.Raycast(o, Vector3.up, out RaycastHit h11, 0.13f)
                        && h11.collider.attachedRigidbody != null && h11.collider.attachedRigidbody != body)
                    {
                        dense = true;
                    }
                }
            }

            if (dense == reported)
            {
                _denseDisagreements++;
            }

            if (contact == reported || contactInside == reported || dense == reported)
            {
                _mismatches.Add("mismatch tile=" + id + " reportedFree=" + reported + " contactAbove=" + contact
                    + " contactInside=" + contactInside + " dense=" + dense + " centreRayCovered=" + covered
                    + " gapAbove=" + NearestGapAbove(id, body).ToString("F3"));
            }
        }
    }

    // The first tile on the board whose reported state matches (free or covered).
    private int FirstTile(bool free)
    {
        for (int id = 0; id < _pc.Board.TotalTiles; id++)
        {
            if (_pc.Board.TryGetOnBoard(id, out MatchTile _) && _pc.IsReportedFree(id) == free)
            {
                return id;
            }
        }

        return -1;
    }

    // Greedy: a reported-free tile whose symbol is already in the tray, otherwise the first reported-free tile.
    // Careless: a random reported-free tile. Avoid: the reported-free tile whose symbol has the fewest tiles in the tray.
    private int ChooseFree()
    {
        if (_avoid)
        {
            int best = -1;
            int bestCount = int.MaxValue;
            for (int id = 0; id < _pc.Board.TotalTiles; id++)
            {
                if (!_pc.Board.TryGetOnBoard(id, out MatchTile tile) || !_pc.IsReportedFree(id))
                {
                    continue;
                }

                int inTray = _pc.Board.TrayCount(tile.Symbol);
                if (inTray < bestCount)
                {
                    bestCount = inTray;
                    best = id;
                }
            }

            return best;
        }

        if (_careless)
        {
            _freeScratch.Clear();
            for (int id = 0; id < _pc.Board.TotalTiles; id++)
            {
                if (_pc.Board.TryGetOnBoard(id, out MatchTile _) && _pc.IsReportedFree(id))
                {
                    _freeScratch.Add(id);
                }
            }

            return _freeScratch.Count == 0 ? -1 : _freeScratch[_random.Next(_freeScratch.Count)];
        }

        int first = -1;
        for (int id = 0; id < _pc.Board.TotalTiles; id++)
        {
            if (!_pc.Board.TryGetOnBoard(id, out MatchTile tile) || !_pc.IsReportedFree(id))
            {
                continue;
            }

            if (first < 0)
            {
                first = id;
            }

            if (_pc.Board.TrayCount(tile.Symbol) > 0)
            {
                return id;
            }
        }

        return first;
    }

    private void Report()
    {
        MatchBoard board = _pc.Board;
        float elapsed = Time.time - _playStartTime;
        _lines.Add("run=" + string.Join(" ", Environment.GetCommandLineArgs()));
        _lines.Add("stage=" + _pc.StageNumber + " totalTiles=" + _totalTiles + " bodiesAtStart=" + _startBodies
            + " seed=" + _seed + " chosenSeed=" + _chosenSeed + " seedSkipped=" + _seedSkipped + " allSymbols=" + _allSymbols);
        _lines.Add("symbols=" + SymbolText() + " distinct=" + _levelDistinct);
        _lines.Add("policy=" + PolicyName() + " reduceMotion=" + _pc.ReduceMotion);
        _lines.Add("settled=" + _settled + " settleSeconds=" + _settleSeconds.ToString("F2"));
        _lines.Add("spawnWaves=" + _spawnWaves + " spawnTopFace=" + _spawnTopFace.ToString("F2") + " spawnOverlapPairs=" + _spawnOverlapPairs);
        _lines.Add("heap tilesAboveFloor=" + _tilesAboveFloor + " topFaceMax=" + _topFaceMax.ToString("F2") + " spreadMax=" + _spreadMax.ToString("F2"));
        _lines.Add("reportedFree=" + _reportedFree + " reportedCovered=" + _reportedCovered + " rayCovered=" + _rayCovered
            + " disagreements=" + _disagreements + " contactDisagreements=" + _contactDisagreements
            + " contactInsideDisagreements=" + _contactDisagreements2 + " denseDisagreements=" + _denseDisagreements);
        foreach (string mismatch in _mismatches)
        {
            _lines.Add(mismatch);
        }
        _lines.Add(_coveredId < 0 ? "coveredTap=none" : "coveredTap id=" + _coveredId + " outcome=" + (MatchOutcome)_coveredOutcome);
        _lines.Add("coveredTapMovedDistance=" + _coveredMoved.ToString("F4"));
        _lines.Add("materialsBefore=" + _materialsBefore + " materialsAfterEnd=" + Resources.FindObjectsOfTypeAll<Material>().Length);
        _lines.Add("state=" + board.State + " tray=" + board.Tray.Count + " tilesCleared=" + board.TilesCleared + " score=" + board.Score
            + " combo=" + board.Combo + " bestCombo=" + board.BestCombo + " remainingOnBoard=" + board.RemainingOnBoard
            + " totalTiles=" + board.TotalTiles + " taps=" + _taps + " blocked=" + _blocked + " playSeconds=" + elapsed.ToString("F1")
            + " bodies=" + _pc.BodyCount + " endReason=" + _endReason);
        _lines.Add("confettiEmitted=" + _pc.Effects.ConfettiEmitted + " burstCount=" + _pc.Effects.BurstCount);
        _lines.Add("gcLevelStart=" + (_gcSettle.Count > 0 ? _gcSettle[0].ToString("F0") : "none"));
        for (int i = 0; i < _taps; i++)
        {
            _lines.Add("tap " + i + " id=" + _tapId[i] + " symbol=" + _tapSymbol[i] + " outcome=" + (MatchOutcome)_tapOutcome[i]
                + " trayBefore=" + _tapTrayBefore[i] + " trayAfter=" + _tapTrayAfter[i] + " free=" + _tapFree[i]
                + " state=" + (MatchState)_tapState[i]);
        }

        ReadHud();
        AddSummary("gcSettle", _gcSettle);
        AddSummary("gcPlay", _gcPlay);
        _lines.Add("gcPlayMax=" + _maxPlay.ToString("F0") + " tapFrame=" + _maxPlayTap + " afterResult=" + _maxPlayAfterResult
            + " secondsAfterResult=" + _maxPlaySeconds.ToString("F2"));
        if (_shotFalling != null)
        {
            _lines.Add("screenshotFalling=" + _shotFalling);
        }

        if (_shotPile != null)
        {
            _lines.Add("screenshotPile=" + _shotPile);
        }

        if (_shotEnd != null)
        {
            ScreenCapture.CaptureScreenshot(Path.Combine(Dir(), _shotEnd));
            _lines.Add("screenshotEnd=" + _shotEnd);
        }
    }

    private void Finish()
    {
        _lines.Add("restart=" + _restartKind + " restartStage=" + _restartStage + " restartTotalTiles=" + _restartTotal
            + " restartBodies=" + _restartBodies + " restartState=" + (MatchState)_restartState
            + " restartSettled=" + _restartSettled + " restartSettleSeconds=" + _restartSettleSeconds.ToString("F2")
            + " restartReportedFree=" + _restartReportedFree + " restartTapOutcome="
            + (_restartTapOutcome < 0 ? "none" : ((MatchOutcome)_restartTapOutcome).ToString()));
        _lines.Add("materialsAfterRestart=" + _materialsAfterRestart);
        AddSummary("gcRestart", _gcRestart);
        _lines.Add("exceptions=" + _exceptions);
        foreach (string text in _exceptionText)
        {
            _lines.Add("exception " + text);
        }

        _lines.Add("consoleLogPath=" + Application.consoleLogPath);
        File.WriteAllLines(Path.Combine(Dir(), "t11-play.txt"), _lines.ToArray());
    }

    private void OnLog(string message, string stackTrace, LogType type)
    {
        if (type != LogType.Exception && type != LogType.Error && type != LogType.Assert)
        {
            return;
        }

        _exceptions++;
        if (_exceptionText.Count < 5)
        {
            string first = (message ?? "").Split('\n')[0];
            _exceptionText.Add(type + ": " + first);
        }
    }

    private string PolicyName()
    {
        return _avoid ? "avoid" : _careless ? "careless" : "greedy";
    }

    private string SymbolText()
    {
        string text = "";
        int symbols = MatchStages.All[_stage].SymbolCount;
        for (int s = 0; s < symbols; s++)
        {
            text += (s == 0 ? "" : " ") + s + ":" + _levelSymbols[s];
        }

        return text;
    }

    private void AddSummary(string name, List<double> values)
    {
        if (values.Count == 0)
        {
            _lines.Add(name + "=none");
            return;
        }

        var sorted = new List<double>(values);
        sorted.Sort();
        int n = sorted.Count;
        int nonZero = 0;
        foreach (double v in values)
        {
            if (v > 0)
            {
                nonZero++;
            }
        }

        _lines.Add(name + " frames=" + n + " median=" + sorted[(n - 1) / 2].ToString("F0") + " p90=" + sorted[(int)((n - 1) * 0.9)].ToString("F0")
            + " max=" + sorted[n - 1].ToString("F0") + " framesAllocating=" + nonZero + " totalBytes=" + SumOf(values).ToString("F0"));
    }

    private static double SumOf(List<double> values)
    {
        double total = 0;
        foreach (double v in values)
        {
            total += v;
        }

        return total;
    }

    private void ReadHud()
    {
        MatchHud hud = UnityEngine.Object.FindAnyObjectByType<MatchHud>();
        if (hud == null)
        {
            _lines.Add("hud=missing");
            return;
        }

        VisualElement root = hud.GetComponent<UIDocument>().rootVisualElement;
        _lines.Add("hudStage=" + LabelText(root, "hud-badge-value"));
        _lines.Add("hudScore=" + LabelText(root, "hud-value"));
        _lines.Add("hudCleared=" + LabelText(root, "hud-kicker-value"));
        _lines.Add("hudBanner=" + LabelText(root, "hud-banner"));
        _lines.Add("hudDetail=" + LabelText(root, "hud-detail"));
        Button retry = UQueryExtensions.Q<Button>(root, null, "hud-button-secondary");
        Button next = UQueryExtensions.Q<Button>(root, null, "hud-button-primary");
        _lines.Add("hudRetryDisplay=" + (retry == null ? "(none)" : retry.style.display.value.ToString()));
        _lines.Add("hudNextDisplay=" + (next == null ? "(none)" : next.style.display.value.ToString()));
    }

    private static string LabelText(VisualElement root, string className)
    {
        Label label = UQueryExtensions.Q<Label>(root, null, className);
        return label == null ? "(none)" : label.text;
    }

    private static string Dir()
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    }
}
