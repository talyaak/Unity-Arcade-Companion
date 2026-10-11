# Unity verification checklist (Pulse Arena)

Everything here is **unverified** until someone runs it in the Unity Editor. Tick an item only after
you've seen it pass in Unity, and note the Unity version. The `dotnet test` results cover only the engine-free core.

Unity version under test: _not yet opened_

## Compile and project setup
- [ ] Project opens in Unity 6 LTS (or 2022.3 LTS) without errors; `ProjectSettings/` and `Packages/manifest.json` generated and committed
- [ ] `PulseArena.Core` compiles with no engine references (asmdef `noEngineReferences: true` is enforced by the compiler)
- [ ] `PulseArena.Game` compiles against the UnityEngine API (the stub compile only checks the types used)
- [ ] `com.unity.test-framework` installed and the EditMode test assembly is discovered in Test Runner
- [ ] EditMode Test Runner: all tests in `Tests/EditMode` pass (currently 87 expected; `dotnet test` also reports 87)
- [ ] The test asmdef's references are sufficient for plain `[Test]` discovery in Unity (asmdef is written from memory, not checked against the docs)
- [ ] `.meta` files for the asmdef and scripts are committed, so GUIDs are stable

## Adapter behaviour (needs Play Mode)
- [ ] `ChallengeController.Update` advances the session every frame, and a 30 s run ends at about 30 s wall time
- [ ] `Time.deltaTime` clamp (`FrameTime.Clamp`, default 0.1 s) keeps a long frame (e.g. forced 1 s stall) from expiring targets
- [ ] App pause (`OnApplicationPause`) and focus loss (`OnApplicationFocus`) freeze the run, and taps are ignored while paused
- [ ] Resume after pause continues from the frozen time (no burst of spawns or expiries)
- [ ] `ChallengeConfigAsset` values set in the Inspector produce the same `ChallengeConfig` as the code defaults (covered by `ConfigValueTests`, but only the rounding helper is tested, not the asset)
- [ ] `TapLog` matches the tap sequence seen in play, and `ReplayVerifier.Verify` on that log reproduces the on-screen score

## Playable scene (verified in Unity 6000.4.0f1, Play Mode, Editor)
- [x] Menu Pulse Arena > Build Challenge Scene runs without errors, twice, with one Build Settings entry; creates the config asset and scene
- [x] Start shows a countdown, then targets appear in lanes; tapping a live lane scores; the run ends at the duration
- [x] Results panel shows score, hits, misses, best combo, accuracy and seed; "Replay check: OK" appears
- [x] Replay starts a new run with a new seed; Menu on the results panel returns to the start screen (fixed in ae32a9a)
- [x] Quit during a run returns to the start screen with no lit lanes (fixed in cbfa6e2)
- [x] Pause: the status line shows "(paused)" and the timer stops
- [x] Scoring: +100 per hit at x1; -50 per miss, floored at 0; combo rises (best combo 5 in one run) and reads 0 after each miss
- [ ] Multiplier step in Unity: x2 at combo 5 and +200 on the 6th hit. **Not verified in Unity.** Covered by Core tests (`MultiplierFor_StepsAtComboBoundaries`, `Hit_UsesMultiplierBeforeComboIncreases`). The asset-to-config mapping was checked by reading the code, not by a test. No run has yet reached combo 5 with a further hit.

## Match scene framing and light (T2; Unity 6000.4.0f1, Play Mode, Editor; evidence in docs/evidence/T2/)
- [x] Pulse Arena > Build Match Scene leaves exactly one directional light (soft shadows, rotation 50, -30, 0), read back with `FindObjectsByType<Light>()`
- [x] Stage 5 with three tiles in the tray (6x6, stack height 5, 7-slot tray row, popup headroom): every projected content point and every tray slot corner is inside the viewport at 16:9 and 9:16. At the T2 tray pitch (1.0) the content filled 46% of the width at 16:9 and 88% at 9:16 (`framing.txt`). At the current tray pitch (1.5, tiles 0.9 units) it fills 61% at 16:9 and 95% at 9:16 (`framing-tile09.txt`)
- [x] Render pipeline is Built-in (`graphics-settings.txt`)
- [ ] Popup and HUD framing. **Not verified.** Render-texture captures do not include IMGUI; waits for T4 (HUD) and T3 (world-space popup).
- [ ] Framing on a real window resize (`Update` refit when Screen size changes). **Not verified**; only `FitCamera` with a forced aspect was run.

## Platform and performance (device-gated, not started)
- [ ] Runs on a real Android or iOS device at target frame rate (no measurement exists yet)
- [ ] Input latency measured and acceptable (no measurement exists yet)
- [x] Engine-free play loop allocates 0 bytes per frame (dotnet proxy: `PulseArenaPerfTests`, passing; not Unity's allocator)
- [ ] No per-frame GC allocations in Unity Play Mode (`GC Alloc` column in the Profiler, Editor and standalone build)

## Not built yet
- [ ] Scene, UI, input wiring (touch or mouse → `Tap(lane)`)
- [ ] Tournament simulation, local leaderboard, progression, diagnostics, config versioning and feature flags
- [ ] Companion package (`UnityLearningCompanion/`), including its Editor-only assemblies and LLM provider abstraction

## Match MVP (new, not yet run in Unity)
- [x] Menu Pulse Arena > Build Match Scene runs without errors and adds `Assets/PulseArena/Scenes/Match.unity` to Build Settings (Unity 6000.4.0f1, run through the `menu` MCP tool; Console 0 errors; `get_build_settings` lists Challenge and Match)
- [ ] Play: the board shows 3D tiles, stacked by cell, with covered tiles dimmed and free tiles bright
- [ ] Tapping a free tile moves it to the tray in about 0.25 s; the tray lays out in order
- [ ] Tapping a covered tile shakes it and changes nothing else
- [ ] Three of a kind clear, with a `+points` popup and the combo shown in the HUD
- [ ] A tile that completes a triple in the last tray slot clears (no loss)
- [x] Filling the tray without a triple shows "Tray full" with a Retry button that works (stage 5 lost with 7 in the tray; Retry pressed with `simulate_pointer` restarted stage 5 with an empty tray; see the T4 section)
- [ ] Clearing the board shows "Level cleared" with a Next stage button that works
- [ ] Touch on a real device behaves like mouse (deferred)
- [ ] Feel criteria in `docs/MATCH_MVP.md` judged by a person (not automatable)

## T1 run notes (2026-10-11, Unity 6000.4.0f1, Play Mode, evidence in `docs/evidence/T1/`)
Taps in this run were sent with `MatchController.Tap(tileId)` through the `eval` MCP tool, **not** through the Pointer.
Reason: Input System 1.20.1 is installed and `activeInputHandler` is 2 (Both) on disk, but the Editor has not been restarted,
so the new backend is inactive (`simulate_pointer` refused: "installed but inactive"). Restarting was denied by the permission
classifier and needs the user.
What was seen through that path (outcomes and positions read from Core and the scene, plus screenshots):
- Blocked tap on a covered tile returned `Blocked` and changed nothing; a free tap returned `Moved`. Tile 4 settled at (-3.00, 0.50, -4.60), tray slot 0 by the controller's layout (`01-after-first-tap-tile-in-tray.png`).
- A triple returned `Cleared` (score 100, combo 1, tray 0). The three views were removed (TileView count equalled tiles left on the board).
- Stage 1 played to `Won` (score 700, combo 3, 0 left); stage 5 played to `Lost` with 7 in the tray (`03-stage5-lost-tray-full.png`).
- Console: 0 errors, 0 warnings during Play (one logged error was a mistaken `find_assets` call of mine, not game code).
Not seen: the 0.25 s move timing, the shake animation, the `+points` popup, the HUD text and the Retry / Next stage buttons (the IMGUI HUD does not appear in the screenshot tool's capture, and IMGUI buttons need real input), the last-slot triple case, and the dimmed/bright tint comparison. Those boxes stay unticked.
Game view only advances while the Editor is the active window; `Application.runInBackground = true` was set from eval for this Play session only (not a project setting).

## Match input and materials (code written, not yet run in Unity)
- [ ] Input System package installed in Package Manager; Active Input Handling set to Both (Player Settings); no legacy Input errors in the Console. **Partly done:** package 1.20.1 installed and the Match compile error CS0234 is gone; `activeInputHandler: 2` is saved, but the backend is not active until an Editor restart
- [x] Tap a tile with the mouse in Play Mode: it moves to the tray (the tap goes through Pointer, not the legacy Input class). Seen with `simulate_pointer` press on a free tile, tray 0 to 1. Device-level pointer input, not a physical mouse
- [x] Build Match Scene creates `Assets/PulseArena/Materials/Tile.mat`, and the controller's Tile Material field is set (read back with eval: `_tileMaterial` = `Assets/PulseArena/Materials/Tile.mat`, Built-in Standard shader)
- [ ] Tiles show the palette colours; covered tiles are dimmed; stacks and the tray look as before
- [x] Material count stays flat across taps: `Resources.FindObjectsOfTypeAll<Material>().Length` was 83 at stage start and 83 after a move and a triple clear. This is an object count, not the Profiler Rendering view
- [ ] Touch on a real device (deferred)

## Match HUD (T4; Unity 6000.4.0f1, Play Mode, Editor; evidence in docs/evidence/T4/)
- [x] No `OnGUI` in the match scene: `MatchController` no longer has one. The scene's components are MatchController, MatchHud, Camera, Light and tiles (TileView). Checked by code, not by a Unity search
- [x] One `UIDocument` and one `PanelSettings` (`Assets/PulseArena/UI/MatchPanelSettings.asset`, scale with screen, 1080 x 1920 reference). Read back in Play Mode
- [x] HUD text reads "Stage 5   Score 0   Combo 0   Tray 0/7   Left 47" from the label tree (read with eval, stage 1 shown in `hud-playing.png`)
- [x] "Level cleared" and a "Next stage" button at Won (`hud-won.png`); "Tray full" and a "Retry" button at Lost (`hud-lost.png`). Both buttons are hidden while playing
- [x] Next stage button pressed with `simulate_pointer` (down, then up): stage 2 started, score 0, board reset
- [x] Retry button pressed with `simulate_pointer`: stage 5 restarted, tray 0, buttons hidden
- [x] "+100" popup shows in the message label after a clear (superseded in T3: the popup is now a world-space TextMesh, and the message label shows results only)
- [x] Tray moves keep the material count flat: `Resources.FindObjectsOfTypeAll<Material>().Length` 99 before and after three tray moves
- [x] Console: no game errors. Two `/api/exec` timeouts from the MCP bridge during eval calls, unrelated to the game
- [ ] GC Alloc 0 B per frame during Playing (Profiler). **Standalone measure:** see the T3 item below. The Editor's 9 KB baseline is not the game's
- [ ] Buttons and taps on a touch device. **Not verified:** no device (T7)
- [ ] Reduce Motion disables USS transitions. **Nothing to disable yet:** the HUD has no transitions. The Reduce Motion button is in T3 (see the Juice section)

## Juice (T3; Unity 6000.4.0f1, Play Mode, Editor; numbers in docs/evidence/T3/measurements.txt)
- [x] Move to tray: 0.253 s, ease-out, arc peak 0.300 units (design 0.25 s and 0.3)
- [x] Blocked tap: shake 0.302 s, peak offset 0.120 units; red flash 0.154 s (design 0.3 s, 0.12, 0.15 s)
- [x] Dim and bright tint: covered tile is 45% of the bright colour for the same symbol (ratio 0.450)
- [x] Clear hit-stop: 0.069 s from tap to pop start (design 0.06 s, one frame of error); Core's clock keeps running
- [x] Burst: 12 particles per cleared tile, 36 for a triple (design: at most 12 per tile)
- [x] Popup: '+100' world-space, 0.900 s, rises 0.600 units, alpha reaches 0 (design 0.9 s and 0.6 units); screenshot `popup-over-tray.png`
- [x] Camera nudge on a clear at combo 3 or more: 0.155 s, peak 0.0431 units (design 0.15 s, at most 0.05 units)
- [x] Win confetti: 60 particles in 3 waves; first to last wave 0.707 s; live peak 40 (design: 60 in total, never more than 40 live)
- [x] Result panel slide: 0.290 s (design 0.3 s); screenshot `confetti-wave.png` shows the panel mid-slide
- [x] Loss: tray red flash 0.301 s, drop 0.500 units in 0.498 s (design 0.3 s, 0.5 units). **Deviation:** the fade is a shrink to zero, not an alpha fade (no transparent material). Screenshot `loss-flash.png`
- [x] Last-slot triple in the scene: tray 6 to 4 on a triple, state Playing, no loss (`last-slot-triple.txt`)
- [x] Reduce Motion on: no shake (peak 0.000), no hit-stop (0.006 s), no confetti (0 on a win); the red flash stays (0.153 s). **Not confirmed:** the camera nudge under Reduce Motion (the combo reached in that run was not logged)
- [ ] GC Alloc 0 B per frame during Playing (standalone Development build, `docs/evidence/T3/gc-standalone-prewarm.txt`). **Met for steady frames:** idle and animation frames are 0 B at the median and p90. **Not met, accepted as event work (D12):** the frame after each tap is about 390 B, from the HUD's label updates. The six 0.6 to 1.5 MB spikes are gone after the HUD pre-warm (before the pre-warm: `docs/evidence/T3/gc-standalone.txt`). Android device numbers are T7
- [ ] Tap squash (0.12 s, to 0.9 and back). **Not measured**
- [ ] Shared materials only: the red material is shared; a material count across T3 taps was not re-measured
- [ ] Touch on a device (T7). Feel (playtest, T5). Audio hooks are silent by design (`AudioCues`)
- [x] Reduce Motion button moved to the top row, clear of the popup area (screenshots `docs/evidence/T9/hud-16x9.png` and `hud-9x16.png`)
- [ ] Framing at FramingMargin 1.10: all points inside at 16:9 and 9:16, margins 4.5% (bottom, 16:9) and 4.9% (sides, 9:16) (`docs/evidence/T2/framing-margin110.txt`). **Note:** the popup can sit under the bottom Reduce Motion button at 1280 x 720 (`popup-over-tray.png`)

## Playtest (T5; bot runs only, `docs/playtests/2026-10-11.md`)
- [x] Bots play stages 1 to 5 in a standalone build: 500 games (greedy and uniform, seeds 1 to 50). Every win rate is inside its D7 band
- [x] No tile stays on screen after a win: checked 1 s of game time after each of the 327 wins
- [ ] Human playtest. **Not done:** every run above is a bot run. Blocked-tap feedback, popup readability and the confusing moments in the log need a person

## Human playtest (T9; needs the user)
- [x] Standalone release build of the Match scene: `PulseArenaGame/Builds/T9-Playtest/PulseArenaGame.exe` (git-ignored; Build Settings unchanged). Smoke-tested by the builder (12 s run): starts, responds, no exceptions in the log
- [x] Player note for the playtester: `docs/playtests/T9-player-note.md`
- [x] Human playtest of stages 1 to 5, written up in `docs/playtests/2026-10-11-human.md`. **Accepted as done** by the architect (1eda29b). Recorded gaps: stage 5 not played; build copy not confirmed
- [x] Stages 1 to 4 played by one person (reported by the user; not observed by the builder)
- [ ] Time to clear for each stage. **Partly:** one range reported, "about 10 seconds, maybe 15 in the hardest", not per stage. It is the player's clock, not game seconds
- [x] Stage 1: rules understood without reading anything (reported: "everything made sense")
- [x] No unexpected tap (reported: "every tap had an expected output")
- [x] Motion felt about right (reported: "no, they felt about right")
- [x] Score, tray count and popup readable (reported: "yes I could"). The score and UI were called "very basic": a polish item, T10
- [x] Reduce motion button found; it made the game less shaky (reported)
- [x] Nothing broke or froze, as of the report (reported)
- [x] Windows 11 Pro, 27-inch at 1920 x 1080 (reported). No screenshot from the player; the builder's captures are in `docs/evidence/T9/`
- [ ] Stage 5 played. **Not verified:** not played; the player stopped before it ("looked like a lot") and said this is not meant as a comment
- [ ] Build copy confirmed as the T9 release build. **Not confirmed**

## Polish after T9 (T10; D13: 2.5D camera, portrait default, tile look, UI bars and buttons)
Captures for every step are at 576 x 1024 (9:16) and 1920 x 1080 (16:9), in `docs/evidence/T10/<step>/`.

### Step a: portrait default and 2.5D camera (`docs/evidence/T10/a-portrait-camera/`)
- [x] Player Settings: standalone windowed 540 x 960, not native, resizable; mobile default orientation Portrait with portrait autorotate only. Read back in the Editor (`notes.txt`). The Editor's Game view aspect is local state and is not set
- [x] Camera: pitch 60 degrees (was 55), field of view 35 (was 60), solid dark background. Read back in the Editor
- [x] Framing: board, tray and HUD inside the frame at 9:16 and 16:9 (standalone captures `t10a-portrait-576x1024.png` and `t10a-landscape-1920x1080.png`)
- [ ] Not in this step: tile look (step b), HUD (step c), GC re-measure (step d)

### Step b: procedural tiles and emblems (`docs/evidence/T10/b-tiles/`, `notes.txt`)
- [x] Tiles are rounded slabs with a flat emblem, built in code (`TileMeshes.cs`; nothing imported; D13). Seen in both captures
- [x] Seven distinct emblems (circle, square, triangle, diamond, star, plus, hexagon), readable at 576 x 1024 (`t10b-portrait-576x1024.png`). The colour is not the only cue
- [x] Stacks step 0.6 units (was 0.95); the layers read as separate tiles; stacks stay vertical per cell
- [x] Tap target: a BoxCollider on each tile's root covers the slab and the emblem; a hit resolves to the TileView (code read; the Editor Play check of taps is in step d)
- [x] Portrait framing: tray pitch 1.05 (was 1.5); headroom above the tallest stack is 20 percent of its height (was 1 unit). The board plate is about 78 percent of the portrait width and the tray about 90 percent (estimated from the capture); the content spans about 54 percent of the height, and the rest is for the HUD bars (step c)
- [x] Build: Unity recompile 0 errors; GameCompile 0 errors (stubs extended, gate unchanged); Development standalone build 0 errors; player log 0 exception lines
- [ ] Material count flat across taps (D3 as amended by D13): not re-checked yet (step d)
- [ ] GC: not re-measured in this step (step d; D12 limits unchanged)
- [ ] HUD: not changed in this step (step c)
