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

## Playable scene (written, not yet run in Unity)
- [ ] Menu Pulse Arena > Build Challenge Scene runs without errors; creates `Assets/PulseArena/Config/DefaultChallenge.asset` and `Assets/PulseArena/Scenes/Challenge.unity`; adds the scene to Build Settings
- [ ] Play the scene: Start shows a countdown, then targets appear in lanes; tapping a live lane scores; the run ends at the duration
- [ ] Results panel shows score, hits, misses, best combo, accuracy and seed; "Replay check: OK" appears (replay matches live score)
- [ ] Replay starts a new run with a new seed; Menu returns to the start screen
- [ ] Quit during a run returns to the start screen with no errors

## Platform and performance (device-gated, not started)
- [ ] Runs on a real Android or iOS device at target frame rate (no measurement exists yet)
- [ ] Input latency measured and acceptable (no measurement exists yet)
- [x] Engine-free play loop allocates 0 bytes per frame (dotnet proxy: `PulseArenaPerfTests`, passing; not Unity's allocator)
- [ ] No per-frame GC allocations in Unity Play Mode (`GC Alloc` column in the Profiler, Editor and standalone build)

## Not built yet
- [ ] Scene, UI, input wiring (touch or mouse → `Tap(lane)`)
- [ ] Tournament simulation, local leaderboard, progression, diagnostics, config versioning and feature flags
- [ ] Companion package (`UnityLearningCompanion/`), including its Editor-only assemblies and LLM provider abstraction
