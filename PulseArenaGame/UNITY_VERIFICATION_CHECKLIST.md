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
- [ ] Filling the tray without a triple shows "Tray full" with a Retry button that works
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
- [ ] Tap a tile with the mouse in Play Mode: it moves to the tray (the tap goes through Pointer, not the legacy Input class)
- [x] Build Match Scene creates `Assets/PulseArena/Materials/Tile.mat`, and the controller's Tile Material field is set (read back with eval: `_tileMaterial` = `Assets/PulseArena/Materials/Tile.mat`, Built-in Standard shader)
- [ ] Tiles show the palette colours; covered tiles are dimmed; stacks and the tray look as before
- [x] Material count stays flat across taps: `Resources.FindObjectsOfTypeAll<Material>().Length` was 83 at stage start and 83 after a move and a triple clear. This is an object count, not the Profiler Rendering view
- [ ] Touch on a real device (deferred)
