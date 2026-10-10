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
- [ ] Menu Pulse Arena > Build Match Scene runs without errors and adds `Assets/PulseArena/Scenes/Match.unity` to Build Settings
- [ ] Play: the board shows 3D tiles, stacked by cell, with covered tiles dimmed and free tiles bright
- [ ] Tapping a free tile moves it to the tray in about 0.25 s; the tray lays out in order
- [ ] Tapping a covered tile shakes it and changes nothing else
- [ ] Three of a kind clear, with a `+points` popup and the combo shown in the HUD
- [ ] A tile that completes a triple in the last tray slot clears (no loss)
- [ ] Filling the tray without a triple shows "Tray full" with a Retry button that works
- [ ] Clearing the board shows "Level cleared" with a Next stage button that works
- [ ] Touch on a real device behaves like mouse (deferred)
- [ ] Feel criteria in `docs/MATCH_MVP.md` judged by a person (not automatable)
