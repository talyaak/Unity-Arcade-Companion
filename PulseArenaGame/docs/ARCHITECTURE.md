# Pulse Arena architecture

## Layers

| Layer | Location | Engine dependency | Verified how |
|---|---|---|---|
| Core (rules, scoring, schedule, state, replay, tournament, progression, diagnostics, config) | `Assets/PulseArena/Core` | None (`noEngineReferences: true`) | `dotnet test` (82 NUnit tests) |
| Game adapter (`ChallengeController`, `ChallengeConfigAsset`) | `Assets/PulseArena/Game` | UnityEngine | Compile-only against stubs (`Harness/PulseArena.GameCompile.csproj`) |
| Tests | `Assets/PulseArena/Tests/EditMode` | NUnit | Same files run by Unity Test Runner (not yet run) and `dotnet test` |

Rule: Core never imports UnityEngine. Game holds no rules; it only forwards time and taps.

## Core modules

- **Random:** `SeededRandom` (SplitMix64). Same seed gives the same sequence on any platform, unlike `UnityEngine.Random` whose sequence is not a guaranteed contract.
- **Challenge:** `ChallengeSchedule` precomputes every target from the seed. `ChallengeSession` plays a run: it processes spawns and expiries in time order and tracks state through an explicit transition table.
- **Scoring:** `ScoreTracker` and `ScoringRules`. The multiplier is read before the combo increases.
- **Replay:** `ReplayVerifier` recomputes a run from `(config, seed, tap log)`.
- **Tournament:** `TournamentRound` validates submissions. `TournamentSimulator` runs seeded bots through the same path.
- **Progression:** `PlayerProfile` applies a finished run to XP, level and unlocks.
- **Diagnostics:** `DiagnosticsLog` with an injected `ITimeSource`.
- **Config:** `ConfigRegistry` (stage, promote, rollback) and `FeatureFlags`.

## Data flow for one run

```
config + seed ──► ChallengeSchedule (all targets, fixed up front)
                       │
Update(dt) ──► FrameTime.Clamp ──► ChallengeSession.Advance
tap(lane)  ──► ChallengeSession.TapLane ──► TapRecord log
                       │
result ◄── session end (Results state)
                       │
submission (seed, config version, taps, claimed score)
                       ▼
TournamentRound.Submit ──► ReplayVerifier.Verify ──► accept / reject with reason
```

## Not built yet

- Persistence of profile and leaderboard (planned: `IKeyValueStore` in Core, PlayerPrefs implementation in Game).
- Scene, UI, touch input wiring.
- Backend. Submissions are currently produced and checked in-process.
