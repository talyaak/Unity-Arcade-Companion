# Testing and monitoring

## Tests

| Command | What it covers | Status |
|---|---|---|
| `cd Harness && dotnet test PulseArena.CoreTests.csproj` | All Core logic and the EditMode test files | 82/82 pass |
| `cd Harness && dotnet build PulseArena.GameCompile.csproj` | Game adapter type-checks against UnityEngine stubs, for net8.0 and netstandard2.1 | Builds |
| Unity Test Runner, EditMode | Same test files inside Unity | **Not run** (no Editor here) |
| Play Mode and device tests | Adapter behaviour, pause, input | **Not written** |

Test design:
- Fixed configs in session tests make timing predictable.
- Determinism is covered by a known-seed regression value (`KnownSeed_ProducesFixedFirstValue`), by same-seed-same-result tests, and by chunked-advance invariance.
- Replay is covered by live-versus-replay equality over 25 random input streams.
- A mutation check injected a scoring-order bug, and the scoring test caught it.

## Monitoring (designed, not running)

`DiagnosticsLog` uses stable codes. Planned dashboards key on these:

| Code | Level | Meaning |
|---|---|---|
| `config.staged` | Info | Candidate passed simulated rounds |
| `config.rejected` | Warning | Candidate failed validation or simulation |
| `config.promoted` | Info | Active config changed |
| `config.rolled_back` | Warning | Previous config restored |
| `flags.unknown_ignored` | Warning | Remote payload contained unknown flags |

Submission outcomes (`SubmissionStatus`) should be logged with the same convention once a backend exists. Alert on `ScoreMismatch` and `InvalidReplay` spikes, since they indicate client bugs or tampering.

Message convention: log values needed to act, never raw player ids or tap logs.
