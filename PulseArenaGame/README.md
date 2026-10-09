# Pulse Arena

An original, mobile-first arcade challenge: tap the target in its lane before it expires, build combos, finish the run, replay.

> **Status: Milestone 1 (in progress).** The engine-free game core is implemented and tested with `dotnet test`.
> The Unity layer (scene, UI, adapter MonoBehaviour) is **not yet written**, and nothing has been compiled or run inside the Unity Editor.

## Verified vs. not verified

| Area | Status |
|---|---|
| Core logic: seeded RNG, scoring, combo, state machine, target timeline, session | Implemented. **48/48 NUnit tests pass** under `dotnet test` (.NET 8 SDK, C# 9 language level) |
| Test suite catches regressions | Checked by injecting a scoring-order bug. One test failed, as expected, and the file was restored |
| Unity compile of `Assets/PulseArena/**` | **Not verified.** No Unity Editor is available in this environment |
| Unity Test Runner (EditMode) | **Not verified** |
| Scene, UI, input, prefabs | **Not started** |
| Device / mobile performance | **Not started** |

## Layout

```
PulseArenaGame/
  Assets/PulseArena/Core/        Engine-free C# (asmdef: noEngineReferences = true)
    Random/SeededRandom.cs       SplitMix64, same sequence on every platform
    Config/                      ChallengeConfig, ScoringRules (plain data + Validate())
    Scoring/ScoreTracker.cs      Score, combo, hit/miss counters
    Challenge/                   State machine, target schedule, session, result
  Assets/PulseArena/Tests/EditMode/   NUnit tests (Unity Test Runner and dotnet test run the same files)
  Harness/                       dotnet test project that compiles Core + tests outside Unity
```

## Run the tests (no Unity needed)

```bash
cd PulseArenaGame/Harness
dotnet test
```

Requires the .NET 8 SDK. NuGet packages are restored automatically.

## Open in Unity (not yet verified)

1. Unity Hub → Add project from disk → select `PulseArenaGame/`.
2. Use Unity 6 LTS (or 2022.3 LTS). The code uses only C# 9 features, but neither version has been tested.
3. Ensure `com.unity.test-framework` is installed (Package Manager → Unity Registry).
4. Window → General → Test Runner → EditMode → Run All.

Unity generates `ProjectSettings/`, `Packages/manifest.json` and `.meta` files on first open. Commit those after the first open, once you've checked them.

## Design decisions (current)

- **Deterministic timeline.** `ChallengeSchedule.Generate` builds all targets from the seed before play. Same seed plus same taps gives the same score. This is what makes replays and tests possible, and it is the basis for later tournament verification.
- **Multiplier read before combo increments.** The hit that reaches a new combo step is still paid at the old multiplier. This is a rule, and it is tested.
- **Empty taps have no penalty.** Only expired targets count as misses. This avoids punishing mis-taps, at the cost of making spamming a lane safe. A design trade-off to revisit once playtesting starts.
- **Score floors at zero.** Misses subtract points but can't push the score negative.
- **Explicit transition table.** `ChallengeStateMachine.TryGetNext` is a pure function. Invalid commands return `false` and change nothing.
- **Config validation collects all errors.** `ChallengeConfig.Validate()` returns every problem, and it rejects NaN. The session throws on invalid config.

## Known gaps (to handle before this is a real game)

- No pause handling. A long `deltaTime` (app backgrounded, debugger break) will expire targets. The Unity adapter must clamp or pause this. The core won't do it silently.
- No Unity adapter, scene or UI yet.
- Tournament simulation, leaderboard, progression, diagnostics and remote configuration are not started.
- Scoring is client-side and cannot be trusted against cheating. Server-authoritative validation is a future requirement.
