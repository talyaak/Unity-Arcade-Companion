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
| T1 | Verify the match play loop in Unity | builder | done (loop verified; carry-over items go to T3) |
| T2 | Visual baseline: pipeline check, framing, light | builder | done |
| T3 | Game feel pass v1 (per the juice standard) | builder | todo |
| T4 | HUD decision (uGUI or UI Toolkit), then migrate the match HUD | architect decides, builder implements | done (UI Toolkit HUD; carry-over: GC Alloc, Reduce Motion, touch) |
| T5 | Stage pacing from simulator bands, plus a playtest log | architect (bands), builder (playtest) | doing (bands + Core test done, D7; builder playtest log pending) |
| T6 | Asset pipeline spec for AI-generated models (no import yet) | architect | done (spec-only; approved as a later stage, no asset work until the user starts it) |
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
- 2026-10-11: Architect: drift check clean (no money terms outside prohibitions). Created docs/DECISIONS.md (D1-D7). T4 decided UI Toolkit (D6). T5 bands in D7 plus 2 Core tests (109/109 pass; run dotnet with DOTNET_ROLL_FORWARD=Major). T6 spec and ledger written, awaiting user approval.

- 2026-10-11: Architect review of builder commit 3a36505 (Input System, Match scene, Tile material): progress note, not a ticket handoff. T1 stays doing (no evidence path, Play Mode not run). Claims match the commit body. Core tests re-run by builder at 109. Spot-check deferred: needs the Editor, which the builder owns during T1. Next for builder: run T1 in Play Mode, put evidence under docs/evidence/T1/, then set T1 to review.
- 2026-10-11 builder, T1 blocked: Input System 1.20.1 is installed and Active Input Handling is saved as Both, but the Editor must restart before the new backend is active. Restart (`quit` tool) was denied by the permission classifier, so Pointer taps and the IMGUI Retry / Next stage buttons are not verified. Through `MatchController.Tap()` I saw: Blocked and Moved outcomes, a triple clear, stage 1 Won, stage 5 Lost, 0 Console errors, flat material count (83). Evidence: `docs/evidence/T1/` (4 PNGs). Not seen: move timing, shake, popup, HUD text, buttons, last-slot triple, tint comparison. dotnet CoreTests: 109 passed (board expected 107). Render pipeline is Built-in (Standard shader), for T2.
- 2026-10-11 builder handover: the previous builder session (unity-arcade-companion-b3) is paused, and a new builder session replaces it. Its full handoff is at `C:\Dev\Unity-Arcade-Companion-BUILDER-HANDOFF.md` (outside the repo). T2 is doing. Its uncommitted WIP (framing and one directional light, in MatchController.cs and PulseArenaSceneBuilder.cs) is unverified and fails the GameCompile stub build; a patch copy is at `C:\Dev\BUILDER-WIP-T2.patch`. The Editor still needs a restart before the Input System backend is active (the quit tool was denied). CoreTests: 109 of 109 pass.
- 2026-10-11: User approved the asset spec as a later-stage plan. T6 done as a spec-only ticket. No generator work, assets or ledger rows until the user starts the asset stage. Silhouette gap (4 shapes, 7 symbols) recorded in ASSET_PIPELINE as a known gap. Tile scale to 0.9 units stays in T2 (builder).
- 2026-10-11 builder, T2 review: Built-in pipeline recorded under D5 (builder reading only). One soft-shadow directional light. Camera refits to stage 5 at 16:9 and 9:16; all 8 content-box corners project inside the viewport. Evidence: `docs/evidence/T2/` (2 PNGs, framing.txt, graphics-settings.txt). Not verified: popup/HUD framing (IMGUI is not in render-texture captures; waits on T4/T3), live window resize. dotnet: 109/109, GameCompile 0 errors (stubs extended, gate unchanged). Next: T4.
- 2026-10-11 builder handoff to architect (pulse-arena-archi, Remote Control; delivery not confirmed): T2 review at 7149564. T1 still blocked (Editor restart; quit denied). Questions: (1) CoreTests baseline 109 vs brief's 107, treated as 109 unless told otherwise; (2) confetti: read as waves of at most 40 live, 60 total, unless told otherwise; (3) may builder start T4 (UI Toolkit HUD, D6) while T1 waits? Builder is holding T4 until answered.
- 2026-10-11: Architect review of T2 (7149564): NOT yet accepted, stays review. Evidence exists and matches claims (framing.txt: all 8 corners inside at 16:9 and 9:16; Built-in reading). Observations from the screenshots: nothing clipped, but the board fills only about a third of the width in landscape, and the tray is not visible in either capture, so tray framing is unproven. Needed to accept: (1) Unity spot-check by me of the camera framing, after the builder says the Editor is free; (2) one capture with the tray visible. Not blocking T4. D5 decided Built-in. Q1 109, Q2 waves of at most 40 live and 60 total (D9), Q3 yes: start T4.
- 2026-10-11 builder, T2 re-capture (for the architect's spot-check): camera now fits the real content points (tiles, tray slots, popup point), not a box. Stage 5 with three tiles in the tray. Landscape 16:9: content spans 46% of the width (was about 31%); all points and tray corners inside. Portrait 9:16: 88% of the width. Evidence replaced in `docs/evidence/T2/`. The Editor is free now (Play Mode stopped, nothing unsaved). T2 stays review. Not verified: popup/HUD framing; live resize. Next: T4 (HUD code); Editor needed again for the T4 Play tests.
- 2026-10-11: Architect ACCEPTS T2 (846b04d). Evidence read: framing.txt (0 points outside at 16:9 and 9:16, width 46% and 88%), both screenshots with 3 tiles in the tray. Spot-check in Unity (editor read-only, no Play Mode): exactly one Directional Light (Realtime) and one Camera in the Match scene; Console 0 errors (1 Input Manager deprecation warning, expected under D2). Observations, not blockers: in the landscape capture the three tray tiles overlap each other (slot spacing smaller than tile size at the current scale), so fix it with the 0.9 scale/tray spacing in T3 or T4; popup/HUD framing and live resize are unverified and move to T4/T3.
- 2026-10-11 builder, Editor relaunched: the MCP bridge had dropped, so the Editor was not running. The builder started `Unity.exe` (6000.4.0f1) on PulseArenaGame; the `unity command` CLI then reached the Pipeline server. No project files were changed by the launch.
- 2026-10-11 builder, T1 review: Pointer path verified. `simulate_pointer` press on a free tile moved it to the tray (tray 0 to 1). Earlier press attempts did nothing: `wasPressedThisFrame` never reached `MatchController.HandlePointer` in this Editor, so the tap now reads the press edge from `isPressed`. HUD buttons pressed with the pointer (Next stage, Retry). Not verified: move timing, shake, popup timing, last-slot triple, tint comparison.
- 2026-10-11 builder, T4 review: match HUD in UI Toolkit (`MatchHud`, one UIDocument, one `MatchPanelSettings`), built in code. OnGUI removed from the match scene. Verified in Play Mode: labels, Level cleared and Next stage, Tray full and Retry, "+100" popup in the message label, both buttons pressed with the pointer. Evidence: `docs/evidence/T4/` (3 PNGs). Not verified: GC Alloc per frame (no Profiler capture), touch device (T7), Reduce Motion (no transitions yet; T3).
- 2026-10-11 builder, T2 follow-up (architect's tray note): primitive tiles at 0.9 units (ASSET_PIPELINE section 3). Tray pitch 1.5 units. Gaps between tray tiles in the captures: 24 px and 24 px at 16:9, 42 px and 46 px at 9:16 (`docs/evidence/T2/tray-spacing-tile09.txt`). Content now fills 61% of the width at 16:9 and 95% at 9:16 (`framing-tile09.txt`). Margins are tight: bottom 2.4% at 16:9, sides 2.6% at 9:16. Question for the architect: raise `FramingMargin` (1.10 would give about 7% at the sides)? I will do it if you say so.
- 2026-10-11: Architect ACCEPTS T4 (6ff85c2) and T1. Evidence read: docs/evidence/T4/ (hud-playing, hud-won, hud-lost) and docs/evidence/T1/ (4 screenshots), plus the checklist sections; claims match, and the not-verified lists are honest. Spot-checks by me: (a) grep finds no OnGUI or GUI. in Game/Match (T4 claim: OnGUI removed); (b) Build Settings read back in Unity lists Challenge and Match, enabled (T1 claim); (c) hud-lost.png shows "Tray full" with Retry and 7/7 in the tray. I did not enter Play Mode. Core tests: NOT re-run by me this time. dotnet was blocked by a Windows Application Control policy ("An Application Control policy has blocked this file", 0x800711C7 loading PulseArena.CoreTests.dll). It ran 109/109 earlier today. Core and Tests sources are unchanged since then (git diff shows only .meta files and UnityStubs.cs), so I accept on that basis and flag it to the user. I did not try to bypass the policy.
  Carry-over to T3 (from T1/T4, not verified yet): move timing (0.25 s), shake on blocked tap, popup timing, dim/bright tint comparison, last-slot triple in the scene, GC Alloc 0 B per frame in Playing (Profiler), Reduce Motion, touch (T7). T3 evidence must cover the first six.
  FramingMargin: yes, raise to 1.10 (margins of 2.4 to 2.6 percent are too tight for phone bezels). Re-capture 16:9 and 9:16 with the tray visible.

