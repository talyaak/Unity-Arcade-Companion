# Build board (Pulse Arena)

Two Claude Code instances work on this board: **architect** (decides and reviews) and **builder** (implements and
verifies in Unity). This file is the source of truth for what is next. Messages between instances are for handoffs,
and every handoff also gets a line here.

## Protocol

- Status values: `todo`, `doing`, `review`, `done`, `blocked`.
- Only the owner changes a ticket's status. The architect moves a ticket to `done`, and only with evidence.
- Evidence means a path under `docs/evidence/<ticket>/` (screenshots or clips), a Console excerpt, or a test count.
- Builder handoff message, at most five lines: ticket id, status, evidence path, what is not verified.
- Architect review message: accept, or reject with the exact reason and the next step.
- One writer per file at a time. Pull before editing. If a merge conflicts, message the other instance first.
- Ownership: the builder owns `PulseArenaGame/Assets/PulseArena/Game/**`, scenes, materials, the Tooling scene builder,
  and any ProjectSettings change (announce it). The architect owns `docs/GAME_DESIGN.md`, `docs/ASSET_PIPELINE.md`,
  `docs/DECISIONS.md`, this board's priorities, and Core rule changes. The builder may propose Core changes.
- Commits: short imperative subject. Body says what was verified and what was not. Trailer: `Claude-Session:` only.
  No model names in commits.
- Push to `feature/pulse-arena-core` only. Never open a PR, force-push or rewrite history.
- Only work in `Unity-Arcade-Companion`. Do not touch other repos.

## Tickets

| ID | Title | Owner | Status |
|---|---|---|---|
| T1 | Verify the match play loop in Unity | builder | doing |
| T2 | Visual baseline: pipeline check, framing, light | builder | todo |
| T3 | Game feel pass v1 (per the juice standard) | builder | todo |
| T4 | HUD decision (uGUI or UI Toolkit), then migrate the match HUD | architect decides, builder implements | todo |
| T5 | Stage pacing from simulator bands, plus a playtest log | architect (bands), builder (playtest) | todo |
| T6 | Asset pipeline spec for AI-generated models (no import yet) | architect | todo |
| T7 | Android device or emulator run, with frame time and input | builder | blocked (needs a device or emulator) |
| T8 | Companion package | nobody | out of scope |

## Ticket details

**T1 Verify the match play loop in Unity.** Install the Input System package. Set Active Input Handling to Both.
Run Pulse Arena > Build Match Scene. In Play Mode, check: a tap on a free tile moves it to the tray; a covered tile
dims and shakes on tap; three of a kind clear with a popup; a full tray shows "Tray full" with a working Retry; a
cleared board shows "Level cleared" with a working Next stage. Tick every Match item in
`PulseArenaGame/UNITY_VERIFICATION_CHECKLIST.md` that you saw pass, with evidence. Check the Console for errors.
Check that `Tile.mat` exists and is assigned, and that the material count stays flat across taps.

**T2 Visual baseline.** Check the render pipeline (URP or Built-in) and record it in `docs/DECISIONS.md`. Frame the
board, tray and popup so nothing is clipped, in landscape and in portrait (9:16). Set up one directional light.
Evidence: one landscape and one portrait screenshot.

**T3 Game feel pass v1.** Implement every item in the juice standard in `docs/GAME_DESIGN.md`, including Reduce
Motion. Evidence: three screenshots or a short clip of motion, plus a Profiler reading of GC Alloc during Playing
and the live particle count. Report what you saw. Do not call the feel good. That needs a playtest.

**T4 HUD.** The architect decides uGUI or UI Toolkit in `docs/DECISIONS.md`, with reasons. The builder migrates the
match HUD: score, combo, tray count, messages and buttons. The match scene must not use `OnGUI`. Buttons must work
with mouse and touch.

**T5 Pacing.** The architect turns the simulator's win rates (see `docs/MATCH_MVP.md`) into target bands per stage,
and records them in `docs/DECISIONS.md`. Add a Core test that checks the bands. The builder plays stages 1 to 5 and
logs time to clear and any confusing moments in `docs/playtests/<date>.md`.

**T6 Asset pipeline spec.** The architect writes `docs/ASSET_PIPELINE.md` and `docs/ASSET_LEDGER.md`. Cover: the
prompt log; the licence check, recorded before use, including commercial-use status; import rules (scale, pivot,
triangle budget per tile, texture size, shared materials); and the review gate. No third-party asset is imported
until its ledger row is complete. No integration with any generator until the spec is approved.

**T7 Device run.** Blocked until an Android device or the Android Emulator is available. Then build, run, and record
frame time and input latency.

## Log

- 2026-10-10: Board created. Match MVP committed at `9373396`. The Input System package and the Unity runs are not done yet.
- 2026-10-11: Builder started T1 (status doing).
