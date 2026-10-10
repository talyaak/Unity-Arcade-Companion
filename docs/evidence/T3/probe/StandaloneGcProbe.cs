using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PulseArena.Core;
using PulseArena.Game;
using Unity.Profiling;
using UnityEngine;

// TEMPORARY measurement probe for one standalone development build. It lives in Assets/Temp (git-ignored) and is deleted
// after the build. It drives the Match scene with a bot (the T5 playtest rules: one tap every 0.5 s of game time) and writes
// its results next to the executable. Modes: -probe-gc (timeScale 1, per-frame GC Alloc) and -probe-playtest (bots, 50 seeds).
public static class StandaloneGcProbe
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        string[] args = Environment.GetCommandLineArgs();
        string mode = null;
        bool noHud = Array.IndexOf(args, "-probe-gc-nohud") >= 0;
        if (Array.IndexOf(args, "-probe-gc") >= 0 || noHud)
        {
            mode = "gc";
        }
        else if (Array.IndexOf(args, "-probe-playtest") >= 0)
        {
            mode = "playtest";
        }
        else if (Array.IndexOf(args, "-probe-iso") >= 0)
        {
            mode = "iso";
        }

        if (mode == null)
        {
            return;
        }

        var go = new GameObject("StandaloneGcProbe");
        UnityEngine.Object.DontDestroyOnLoad(go);
        ProbeRunner runner = go.AddComponent<ProbeRunner>();
        runner.NoHud = noHud;
        runner.Configure(mode);
    }
}

public sealed class ProbeRunner : MonoBehaviour
{
    private const int GcFrames = 9000;
    private const double TapGapSeconds = 0.5;
    private const float WinCheckSeconds = 1f;
    private const int Seeds = 50;

    private string _mode;
    [System.NonSerialized] public bool NoHud;
    private MatchController _mc;
    private ProfilerRecorder _gcRecorder;
    private long[] _gcBytes;
    private byte[] _flags;
    private float[] _dtMs;
    private int _gcCount;
    private byte _pending;
    private byte[] _phaseOf;
    private MatchHud _hud;
    private MatchPopup _popup;
    private int _isoPhase;
    private int _isoFrames;
    private static readonly int[] IsoLengths = { 200, 300, 300, 300, 300, 300, 300, 300, 300 };
    private Camera _cam;
    private int _frameSinceStart;
    private bool _finished;

    private readonly StringBuilder _events = new StringBuilder();
    private readonly StringBuilder _runs = new StringBuilder();
    private System.Random _rng = new System.Random(12345);

    // Current game
    private bool _greedy = true;
    private string _botName = "greedy";
    private int _stage;          // 0-based
    private int _seed;
    private double _lastTapClock = -1.0;
    private int _taps;
    private int _blocked;
    private int _clears;
    private int _endState;       // 0 playing, 1 won (waiting for the tile check), 2 lost
    private float _endedAt;
    private int _tilesOnScreenAfterWin = -1;

    // Playtest order: bots x stages x seeds
    private int _runIndex;
    private readonly string[] _bots = { "greedy", "uniform" };

    public void Configure(string mode)
    {
        _mode = mode;
        Application.runInBackground = true;
        if (mode == "gc" || mode == "iso")
        {
            _gcBytes = new long[GcFrames];
            _flags = new byte[GcFrames];
            _dtMs = new float[GcFrames];
            _phaseOf = new byte[GcFrames];
        }
        else
        {
            Time.timeScale = 30f;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
        }
    }

    private void Start()
    {
        if (_mode == "gc" || _mode == "iso")
        {
            _gcRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1);
        }
    }

    private void Update()
    {
        if (_finished)
        {
            return;
        }

        if (_mc == null)
        {
            _mc = FindAnyObjectByType<MatchController>();
            if (_mc == null || _mc.Board == null)
            {
                return;
            }

            if (_mode == "iso")
            {
                _hud = FindAnyObjectByType<MatchHud>();
                _popup = FindAnyObjectByType<MatchPopup>();
                _mc.StartLevel(0, 1UL);
                ResetGame();
                ApplyIsoPhase();
                return;
            }

            if (_mode == "gc")
            {
                _greedy = true;
                _botName = "greedy";
                _stage = 0;
                _seed = 1;
                _mc.StartLevel(_stage, (ulong)_seed);
                ResetGame();
                if (NoHud)
                {
                    FindAnyObjectByType<MatchHud>().enabled = false;
                }
            }
            else
            {
                NextPlaytestRun();
            }

            return;
        }

        if (_mode == "gc")
        {
            StepGc();
        }
        else if (_mode == "iso")
        {
            StepIso();
        }
        else
        {
            StepPlaytest();
        }
    }

    // --- GC mode: one record per frame, written at the start of the next frame (the recorder reports the last frame) ---
    private void StepGc()
    {
        if (_gcCount < GcFrames - 1)
        {
            _gcBytes[_gcCount] = _gcRecorder.LastValue;
            _flags[_gcCount] = _pending;
            _dtMs[_gcCount] = Time.unscaledDeltaTime * 1000f;
            _gcCount++;
        }

        _pending = 0;
        if (_gcCount >= GcFrames - 1)
        {
            _finished = true;
            WriteGc();
            return;
        }

        PlayStep(allowAdvance: true);
    }

    // --- Isolation mode: no taps. Phases switch components off one group at a time; each frame is tagged with its phase. ---
    private void StepIso()
    {
        if (_gcCount < GcFrames - 1)
        {
            _gcBytes[_gcCount] = _gcRecorder.LastValue;
            _flags[_gcCount] = _pending;
            _dtMs[_gcCount] = Time.unscaledDeltaTime * 1000f;
            _phaseOf[_gcCount] = (byte)_isoPhase;
            _gcCount++;
        }

        _pending = 0;
        _isoFrames++;
        if (_isoFrames >= IsoLengths[_isoPhase])
        {
            _isoPhase++;
            _isoFrames = 0;
            if (_isoPhase >= IsoLengths.Length)
            {
                _finished = true;
                WriteIso();
                return;
            }

            _pending |= 2;
            ApplyIsoPhase();
        }
    }

    private void ApplyIsoPhase()
    {
        bool controller = true;
        bool hud = true;
        bool effects = true;
        bool tiles = true;
        switch (_isoPhase)
        {
            case 2: hud = false; break;
            case 3: controller = false; break;
            case 4: effects = false; break;
            case 5: tiles = false; break;
            case 6: break;
            case 7: controller = false; hud = false; effects = false; tiles = false; break;
            case 8: controller = false; hud = false; effects = false; tiles = false; break;
        }

        if (_cam == null)
        {
            _cam = Camera.main;
        }

        if (_cam != null)
        {
            _cam.enabled = _isoPhase != 8;
        }

        _mc.enabled = controller;
        _hud.enabled = hud;
        _mc.Effects.enabled = effects;
        _popup.enabled = effects;
        foreach (TileView view in UnityEngine.Object.FindObjectsByType<TileView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            view.enabled = tiles;
        }
    }

    private void WriteIso()
    {
        string dir = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        var csv = new StringBuilder();
        csv.Append("frame,phase,dtMs,gcBytesOfFrame,flags\n");
        var summary = new StringBuilder();
        for (int ph = 0; ph < IsoLengths.Length; ph++)
        {
            var vals = new List<long>();
            for (int i = 0; i < _gcCount; i++)
            {
                if (_phaseOf[i] == ph && _flags[i] == 0)
                {
                    vals.Add(_gcBytes[i]);
                }
            }

            vals.Sort();
            int n = vals.Count;
            if (n == 0)
            {
                summary.Append("phase " + ph + ": no frames\n");
                continue;
            }

            summary.Append("phase " + ph + ": n=" + n + " median=" + vals[n / 2] + " p90=" + vals[(int)(n * 0.9)] + " max=" + vals[n - 1] + "\n");
        }

        for (int i = 0; i < _gcCount; i++)
        {
            csv.Append(i).Append(',').Append(_phaseOf[i]).Append(',').Append(_dtMs[i].ToString("F3")).Append(',')
                .Append(_gcBytes[i]).Append(',').Append(_flags[i]).Append('\n');
        }

        File.WriteAllText(Path.Combine(dir, "iso-frames.csv"), csv.ToString());
        File.WriteAllText(Path.Combine(dir, "iso-summary.txt"), summary.ToString());
        File.WriteAllText(Path.Combine(dir, "iso-done.txt"), "frames=" + _gcCount + "\n");
        Application.Quit();
    }

    // --- Playtest mode: bots x stages x seeds, timeScale 30 ---
    private void StepPlaytest()
    {
        PlayStep(allowAdvance: true);
    }

    // Shared bot logic: tap every 0.5 s of game time, detect the end, check no tile remains after a win, then advance.
    private void PlayStep(bool allowAdvance)
    {
        if (_endState == 0)
        {
            if (_mc.Board.State != MatchState.Playing)
            {
                return;
            }

            if (_lastTapClock < 0.0 || _mc.Clock - _lastTapClock >= TapGapSeconds)
            {
                int id = ChooseTap();
                if (id >= 0)
                {
                    MatchStep step = _mc.Tap(id);
                    _lastTapClock = _mc.Clock;
                    _taps++;
                    _pending |= 1;
                    if (step.Outcome == MatchOutcome.Blocked)
                    {
                        _blocked++;
                    }
                    else if (step.Outcome == MatchOutcome.Moved)
                    {
                        _pending |= 32;
                    }
                    else if (step.Outcome == MatchOutcome.Cleared)
                    {
                        _pending |= 16;
                        _clears++;
                    }
                    else if (step.Outcome == MatchOutcome.Won)
                    {
                        _endState = 1;
                        _endedAt = Time.time;
                        _pending |= 4;
                    }
                    else if (step.Outcome == MatchOutcome.Lost)
                    {
                        _endState = 2;
                        _endedAt = Time.time;
                        _pending |= 4;
                    }
                }
            }

            return;
        }

        if (_endState == 1 && Time.time - _endedAt >= WinCheckSeconds)
        {
            _tilesOnScreenAfterWin = UnityEngine.Object.FindObjectsByType<TileView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
            _pending |= 8;
            RecordEnd("won");
            if (allowAdvance)
            {
                AdvanceAfterEnd();
            }

            return;
        }

        if (_endState == 2)
        {
            _tilesOnScreenAfterWin = -1;
            RecordEnd("lost");
            if (allowAdvance)
            {
                AdvanceAfterEnd();
            }
        }
    }

    private void RecordEnd(string outcome)
    {
        if (_mode == "gc")
        {
            _events.Append(outcome).Append(" botStage=").Append(_stage + 1)
                .Append(" clock=").Append(_mc.Clock.ToString("F2"))
                .Append(" taps=").Append(_taps)
                .Append(" blocked=").Append(_blocked)
                .Append(" clears=").Append(_clears)
                .Append(" bestCombo=").Append(_mc.Board.BestCombo)
                .Append(" tilesLeftOnScreenAfter1s=").Append(_tilesOnScreenAfterWin)
                .Append('\n');
        }
        else
        {
            _runs.Append(_botName).Append(',')
                .Append(_stage + 1).Append(',')
                .Append(_seed).Append(',')
                .Append(outcome).Append(',')
                .Append(_mc.Clock.ToString("F2")).Append(',')
                .Append(_taps).Append(',')
                .Append(_blocked).Append(',')
                .Append(_clears).Append(',')
                .Append(_mc.Board.BestCombo).Append(',')
                .Append(_mc.Board.RemainingOnBoard).Append(',')
                .Append(_tilesOnScreenAfterWin).Append('\n');
        }
    }

    private void AdvanceAfterEnd()
    {
        if (_mode == "gc")
        {
            if (_endState == 1)
            {
                _stage = (_stage + 1) % MatchStages.All.Count;
                _pending |= 2;
                _mc.NextStage();
            }
            else
            {
                _seed++;
                _pending |= 2;
                _mc.Retry();
            }

            ResetGame();
            return;
        }

        _runIndex++;
        NextPlaytestRun();
    }

    private void NextPlaytestRun()
    {
        int perBot = MatchStages.All.Count * Seeds;
        if (_runIndex >= _bots.Length * perBot)
        {
            _finished = true;
            WritePlaytest();
            return;
        }

        int bot = _runIndex / perBot;
        int within = _runIndex % perBot;
        _botName = _bots[bot];
        _greedy = bot == 0;
        _stage = within / Seeds;
        _seed = within % Seeds + 1;
        _mc.StartLevel(_stage, (ulong)_seed);
        ResetGame();
    }

    private void ResetGame()
    {
        _endState = 0;
        _lastTapClock = -1.0;
        _taps = 0;
        _blocked = 0;
        _clears = 0;
        _tilesOnScreenAfterWin = -1;
    }

    // Greedy: a free tile whose symbol is already in the tray (a triple first, then a pair), and avoid starting a new
    // symbol once the tray holds five. Uniform: any free tile, chosen at random. Neither bot allocates per tap.
    private int ChooseTap()
    {
        MatchBoard board = _mc.Board;
        int total = board.TotalTiles;
        if (!_greedy)
        {
            int free = 0;
            for (int id = 0; id < total; id++)
            {
                if (board.TryGetOnBoard(id, out _) && board.IsFree(id))
                {
                    free++;
                }
            }

            if (free == 0)
            {
                return -1;
            }

            int pick = _rng.Next(free);
            int k = 0;
            for (int id = 0; id < total; id++)
            {
                if (board.TryGetOnBoard(id, out _) && board.IsFree(id))
                {
                    if (k == pick)
                    {
                        return id;
                    }

                    k++;
                }
            }

            return -1;
        }

        int best = -1;
        int bestScore = -1;
        IReadOnlyList<MatchTile> tray = board.Tray;
        for (int id = 0; id < total; id++)
        {
            if (!board.TryGetOnBoard(id, out MatchTile tile) || !board.IsFree(id))
            {
                continue;
            }

            int same = 0;
            for (int i = 0; i < tray.Count; i++)
            {
                if (tray[i].Symbol == tile.Symbol)
                {
                    same++;
                }
            }

            int score = same == 2 ? 3 : same == 1 ? 2 : (tray.Count >= 5 ? 0 : 1);
            if (score > bestScore)
            {
                bestScore = score;
                best = id;
            }
        }

        return best;
    }

    private void WriteGc()
    {
        string dir = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        var csv = new StringBuilder();
        csv.Append("frame,dtMs,gcBytesOfFrame,flags\n");
        for (int i = 0; i < _gcCount; i++)
        {
            csv.Append(i).Append(',').Append(_dtMs[i].ToString("F3")).Append(',')
                .Append(_gcBytes[i]).Append(',').Append(_flags[i]).Append('\n');
        }

        File.WriteAllText(Path.Combine(dir, "gc-frames.csv"), csv.ToString());
        File.WriteAllText(Path.Combine(dir, "gc-events.txt"), _events.ToString());
        File.WriteAllText(Path.Combine(dir, "gc-done.txt"), "frames=" + _gcCount + "\n");
        Application.Quit();
    }

    private void WritePlaytest()
    {
        string dir = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        var csv = new StringBuilder();
        csv.Append("bot,stage,seed,outcome,clockSeconds,taps,blocked,clears,bestCombo,tilesLeftOnBoard,tilesOnScreenAfterWin\n");
        csv.Append(_runs);
        File.WriteAllText(Path.Combine(dir, "playtest-runs.csv"), csv.ToString());
        File.WriteAllText(Path.Combine(dir, "playtest-done.txt"), "runs=" + _runIndex + "\n");
        Application.Quit();
    }
}
