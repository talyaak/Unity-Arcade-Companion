using System;
using System.Collections.Generic;
using System.IO;
using PulseArena.Core;
using PulseArena.Game;
using UnityEngine;
using UnityEngine.UIElements;

/// Temporary T10 step d probe. A text copy is kept in docs/evidence/T10/d-verify/probe/. It is not committed: it lives in
/// Assets/Temp, which git ignores, and it is removed after the build. Modes: -t10-sample (material counts across taps, and a
/// covered tile against a free one), -t10-clear (a triple clears, then the HUD labels are read), -t10-lost (taps until the
/// tray is full, then the result card). Results go to t10d-<mode>.txt next to the exe; -t10-shot <name> also writes a screenshot.
public sealed class T10DProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        string[] args = Environment.GetCommandLineArgs();
        string mode = null;
        if (Array.IndexOf(args, "-t10-sample") >= 0) { mode = "sample"; }
        else if (Array.IndexOf(args, "-t10-clear") >= 0) { mode = "clear"; }
        else if (Array.IndexOf(args, "-t10-lost") >= 0) { mode = "lost"; }
        if (mode == null) { return; }

        string shot = null;
        int shotArg = Array.IndexOf(args, "-t10-shot");
        if (shotArg >= 0 && shotArg + 1 < args.Length) { shot = args[shotArg + 1]; }

        var go = new GameObject("T10DProbe");
        UnityEngine.Object.DontDestroyOnLoad(go);
        T10DProbe probe = go.AddComponent<T10DProbe>();
        probe._mode = mode;
        probe._shot = shot;
        probe._stage5 = Array.IndexOf(args, "-t10-stage5") >= 0;
    }

    private string _mode;
    private string _shot;
    private readonly List<string> _lines = new List<string>();
    private MatchController _mc;
    private int _frame;
    private int _phase;          // 0 wait for the level, 1 acting, 2 settling, 3 finished
    private int _nextActAt;
    private int _settleUntil;
    private int _quitAt;
    private int _taps;
    private int _chosenSymbol = -1;
    private bool _stage5;

    private void Update()
    {
        _frame++;
        if (_phase == 3)
        {
            if (_frame >= _quitAt)
            {
                Application.Quit();
            }

            return;
        }

        if (_mc == null)
        {
            _mc = UnityEngine.Object.FindAnyObjectByType<MatchController>();
        }

        // Give Start and the HUD warm-up time to finish before the first tap.
        if (_mc == null || _mc.Board == null || _frame < 90)
        {
            return;
        }

        if (_phase == 0)
        {
            _phase = 1;
            _nextActAt = _frame;
            if (_stage5)
            {
                // Stage 5 has seven symbols, so a tray of seven distinct tiles can fill and lose.
                _mc.StartLevel(4, 20261011UL);
                _lines.Add("level=stage5");
            }

            _lines.Add("mode=" + _mode);
            _lines.Add("materialsBefore=" + Resources.FindObjectsOfTypeAll<Material>().Length);
            return;
        }

        if (_phase == 1)
        {
            if (_frame < _nextActAt)
            {
                return;
            }

            _nextActAt = _frame + 12;   // one action every 12 frames, so each tap settles before the next
            if (_mode == "sample")
            {
                ActSample();
            }
            else if (_mode == "clear")
            {
                ActClear();
            }
            else
            {
                ActLost();
            }

            return;
        }

        if (_phase == 2 && _frame >= _settleUntil)
        {
            Report();
        }
    }

    private void ActSample()
    {
        if (_taps < 3)
        {
            if (!TapDistinct())
            {
                _taps = 3;
            }

            _taps++;
            if (_taps == 3)
            {
                _lines.Add("materialsAfterThreeTaps=" + Resources.FindObjectsOfTypeAll<Material>().Length);
            }

            return;
        }

        if (_taps < 9)
        {
            TapAnyFree();
            _taps++;
            return;
        }

        _lines.Add("materialsAfterNineTaps=" + Resources.FindObjectsOfTypeAll<Material>().Length);
        Settle(60);
    }

    private void ActClear()
    {
        if (_chosenSymbol < 0)
        {
            var counts = new Dictionary<int, int>();
            for (int id = 0; id < _mc.Board.TotalTiles; id++)
            {
                if (!_mc.Board.TryGetOnBoard(id, out MatchTile t) || !_mc.Board.IsFree(id))
                {
                    continue;
                }

                counts.TryGetValue(t.Symbol, out int c);
                counts[t.Symbol] = c + 1;
            }

            foreach (KeyValuePair<int, int> pair in counts)
            {
                if (pair.Value >= 3)
                {
                    _chosenSymbol = pair.Key;
                    break;
                }
            }

            _lines.Add("chosenSymbol=" + _chosenSymbol);
            if (_chosenSymbol < 0)
            {
                Settle(60);
                return;
            }
        }

        for (int id = 0; id < _mc.Board.TotalTiles; id++)
        {
            if (!_mc.Board.TryGetOnBoard(id, out MatchTile t) || t.Symbol != _chosenSymbol || !_mc.Board.IsFree(id))
            {
                continue;
            }

            _mc.Tap(id);
            _taps++;
            break;
        }

        if (_taps >= 3)
        {
            Settle(90);
        }
    }

    private void ActLost()
    {
        bool tapped = TapDistinct() || TapAnyFree();
        if (tapped)
        {
            _taps++;
        }

        if (_mc.Board.State != MatchState.Playing || _taps >= 60 || !tapped)
        {
            Settle(150);
        }
    }

    // Taps a free tile whose symbol is not in the tray yet. Returns false when there is none.
    private bool TapDistinct()
    {
        for (int id = 0; id < _mc.Board.TotalTiles; id++)
        {
            if (!_mc.Board.TryGetOnBoard(id, out MatchTile t) || !_mc.Board.IsFree(id))
            {
                continue;
            }

            bool inTray = false;
            foreach (MatchTile inTrayTile in _mc.Board.Tray)
            {
                if (inTrayTile.Symbol == t.Symbol)
                {
                    inTray = true;
                }
            }

            if (inTray)
            {
                continue;
            }

            _mc.Tap(id);
            return true;
        }

        return false;
    }

    // Taps the first free tile. Returns false when the board has no free tile.
    private bool TapAnyFree()
    {
        for (int id = 0; id < _mc.Board.TotalTiles; id++)
        {
            if (_mc.Board.TryGetOnBoard(id, out MatchTile t) && _mc.Board.IsFree(id))
            {
                _mc.Tap(id);
                return true;
            }
        }

        return false;
    }

    private void Settle(int frames)
    {
        _phase = 2;
        _settleUntil = _frame + frames;
    }

    private void Report()
    {
        MatchBoard board = _mc.Board;
        _lines.Add("state=" + board.State + " tray=" + board.Tray.Count + " tilesCleared=" + board.TilesCleared
            + " score=" + board.Score + " combo=" + board.Combo + " remainingOnBoard=" + board.RemainingOnBoard
            + " totalTiles=" + board.TotalTiles + " taps=" + _taps);
        _lines.Add("materialsAfter=" + Resources.FindObjectsOfTypeAll<Material>().Length);
        ReadHud();
        if (_mode == "sample")
        {
            SampleTiles();
        }

        string dir = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!string.IsNullOrEmpty(_shot))
        {
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, _shot));
            _lines.Add("screenshot=" + _shot);
        }

        File.WriteAllLines(Path.Combine(dir, "t10d-" + _mode + ".txt"), _lines.ToArray());
        _phase = 3;
        _quitAt = _frame + 10;
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

    private void SampleTiles()
    {
        TileView free = null;
        TileView covered = null;
        foreach (TileView view in UnityEngine.Object.FindObjectsByType<TileView>(FindObjectsSortMode.None))
        {
            if (!_mc.Board.TryGetOnBoard(view.TileId, out MatchTile t))
            {
                continue;
            }

            if (_mc.Board.IsFree(view.TileId))
            {
                if (free == null)
                {
                    free = view;
                }
            }
            else if (covered == null)
            {
                covered = view;
            }
        }

        _lines.Add("freeTileFound=" + (free != null) + " coveredTileFound=" + (covered != null));
        if (free == null || covered == null)
        {
            return;
        }

        Material[] fm = free.GetComponent<Renderer>().sharedMaterials;
        Material[] cm = covered.GetComponent<Renderer>().sharedMaterials;
        float fl = Luminance(fm[0].color);
        float cl = Luminance(cm[0].color);
        _lines.Add("freeBody=" + fm[0].color + " freeEmblem=" + fm[1].color);
        _lines.Add("coveredBody=" + cm[0].color + " coveredEmblem=" + cm[1].color);
        _lines.Add("luminanceFree=" + fl + " luminanceCovered=" + cl + " coveredOverFree=" + (cl / fl));
    }

    private static float Luminance(Color c)
    {
        return 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
    }
}
