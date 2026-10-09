# Pulse Arena system design (local now, server later)

## Trust model

Today everything runs on the device. Any score produced on a client can be forged. The local code makes two things checkable:

1. **Consistency:** a claimed score must match a replay of the submitted tap log against the seed and config version (`ReplayVerifier`).
2. **Fairness of input:** every player in a round gets the same seed and config (`TournamentRound`).

Neither proves the taps were human. A modified client can submit a tap log that never happened, and the replay will accept it, as long as the log is internally consistent.

## Required for competitive use (future)

- **Server-issued seeds and round windows.** The client must not choose its own seed. The server issues the seed and the close time.
- **Server-side replay.** The server runs `ReplayVerifier` on the submitted log. The Core code is engine-free so the same logic can run on a backend.
- **Input plausibility checks.** Tap intervals and reaction times below human limits are flagged. Thresholds need data from playtests, not guesses.
- **Rate limits and one-submission-per-round** enforced server-side. The local rule is only a guard against accidents.
- **Signed config.** Config versions are published by the server, and clients report which version they ran.

## Failure modes handled locally

| Situation | Behaviour |
|---|---|
| Submission for an old config version | Rejected with `WrongConfigVersion` |
| Submission after the round closes | Rejected with `AfterDeadline` |
| Claimed score differs from replay | Rejected with `ScoreMismatch` |
| Malformed tap log (out of order, outside run) | Rejected with `InvalidReplay` |
| Rejected submission | Does not consume the player's slot |
| Candidate config fails validation or simulation | Not staged; active config unchanged |
| Unknown feature flag in remote payload | Ignored and logged |

## Config promotion

`ConfigRegistry.StageCandidate` validates the candidate and plays simulated rounds on the given seeds. It stages only if every simulated run verifies. `Promote` keeps one step of history, and `Rollback` restores it. This is a local simulation of a rollout. It is not a production deploy, and it does not cover live traffic.
