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
| T3 | Game feel pass v1 (per the juice standard) | builder | done (juice v1 implemented and measured; carry-over: GC in a standalone build, tap squash, Reduce Motion nudge) |
| T4 | HUD decision (uGUI or UI Toolkit), then migrate the match HUD | architect decides, builder implements | done (UI Toolkit HUD; carry-over: GC Alloc, Reduce Motion, touch) |
| T5 | Stage pacing from simulator bands, plus a playtest log | architect (bands), builder (playtest) | done (bands, Core test and the builder bot-run log; the human playtest is split out as T9) |
| T6 | Asset pipeline spec for AI-generated models (no import yet) | architect | done (spec-only; approved as a later stage, no asset work until the user starts it) |
| T7 | Android device or emulator run, with frame time and input | builder | blocked (needs a device or emulator) |
| T8 | Companion package | nobody | out of scope |
| T9 | Human playtest of stages 1 to 5 (needs a person) | user, builder prepares | todo |

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
- 2026-10-11 builder: T3 set to doing (juice standard, GAME_DESIGN). Carry-over evidence (from the architect's T1/T4 note): move timing, blocked-tap shake, popup timing, dim/bright tint, last-slot triple in the scene, GC Alloc per frame (ProfilerRecorder if no Profiler capture is possible). Also: Reduce Motion and the audio hooks (silent). FramingMargin 1.10 and the tray re-capture follow. CoreTests on this machine: 109/109 (dotnet ran here; the architect's machine is blocked by policy).
- 2026-10-11: Builder reports CoreTests 109/109 pass on its side (dotnet ran there); this closes the gap noted in the T1/T4 acceptance. I did not re-run it myself (Application Control block here). Builder has the Editor for T3 checks and the FramingMargin 1.10 re-capture; architect does not need it yet.
- 2026-10-11 builder, FramingMargin 1.10 (architect's yes): re-captured 16:9 and 9:16 with the tray visible (stage 5, three tiles in the tray). All fit points inside. Margins: 4.5% bottom at 16:9, 4.9% sides at 9:16. These are less than the roughly 7% I estimated before the change, so 1.10 gives less margin than I said. Evidence: `docs/evidence/T2/framing-margin110.txt` and two PNGs.
- 2026-10-11 builder, T3 review: juice pass implemented in Game only (Core untouched). Measured in Play Mode (`docs/evidence/T3/measurements.txt`): move 0.253 s with a 0.300 arc; shake 0.302 s, 0.120 offset; red flash 0.154 s; dim tint 0.450 of bright; hit-stop 0.069 s; burst 12 per tile; popup 0.900 s, 0.600 rise; nudge 0.155 s, 0.043 peak; confetti 60 in three waves, live peak 40; loss flash 0.301 s, drop 0.500 units in 0.498 s; result slide 0.290 s; last-slot triple 6 to 4, Playing. Reduce Motion: no shake, no hit-stop, no confetti, red flash kept. Screenshots in `docs/evidence/T3/`.
  Deviations, for the architect to record: (1) the loss fade is a shrink to zero, because an alpha fade needs a transparent material and that breaks shared materials. (2) The loss flash is on the tray tiles (the slots have no visuals). (3) The popup sits over the tray slot where the triple formed (it was over the tapped tile's old board position before). (4) The camera nudge under Reduce Motion was not confirmed.
  Not verified: GC Alloc 0 B per frame. The Editor's steady baseline is 9,013 B per frame, with every match component disabled too. The match code adds none (idle or animation), but 0 B needs a standalone build or a device run (`docs/evidence/T3/gc-profiler.txt`). Tap squash (0.12 s) not measured. Touch (T7). Feel (T5 playtest).
  Questions: (a) accept the shrink as the loss fade, or ask for a transparent material? (b) for 0 B: may I make a standalone Windows build of PulseArenaGame to measure GC Alloc there? (c) FramingMargin 1.10 gives 4.5% and 4.9%: raise to 1.15?
  dotnet: CoreTests 109/109 and GameCompile 0 errors on this machine.
- 2026-10-11: Architect ACCEPTS T3 (53d6a51) as 'juice v1 implemented and measured'. This is not a feel verdict; feel needs a playtest (T5). Evidence read: measurements.txt, gc-profiler.txt, last-slot-triple.txt, confetti-wave.png. Numbers match the standard within one frame (move 0.253 s, shake 0.302 s, popup 0.900 s, tint 0.450, confetti 60 in 3 waves with a live peak of 40, last-slot triple 6 to 4 and Playing). Spot-checks by me (Editor in Edit Mode, no Play Mode): MatchEffects.MaxLive = 40 and main.maxParticles is set from it; MatchHud calls ToggleReduceMotion and MatchController exposes ReduceMotion; scene has Match HUD with UIDocument and MatchHud. Honest gaps accepted: GC Alloc 0 B NOT met in the Editor (9,013 B baseline even with match components off); tap squash not measured; camera nudge under Reduce Motion not confirmed; loss fade is a shrink (D11). Observation: in confetti-wave.png the board still shows tiles while Left 0, presumably mid-shrink under the sliding panel; check in the next pass that no tile outlives a win. Answers: (a) yes, shrink accepted, GAME_DESIGN edited; (b) yes, build a standalone Windows development build to measure GC, output to a gitignored folder, commit no binaries, leave Build Settings as they are; (c) no, keep 1.10, revisit at T7.
- 2026-10-11 builder, GC standalone (final build, `docs/evidence/T3/gc-standalone.txt`): idle and animation frames are 0 B at the median and p90. The frame after each tap is about 390 B, and six spike frames are 593 KB to 1.47 MB. With the HUD component disabled, no frame is over 50 KB, so the HUD's UI Toolkit label updates (Tray and Left change on nearly every tap) cause both. Two other causes were ruled out: popup glyphs and tile material creation. Standalone also exposed a defect that is now fixed: MatchEffects threw ArgumentNullException at every level start because the particle shader is stripped from a player unless it is in the build (fixed; no throw).
- 2026-10-11 builder, T5 playtest log: bot runs only, `docs/playtests/2026-10-11.md` (raw: `docs/playtests/2026-10-11-bot-runs.csv`). 500 games, two bots, stages 1 to 5. Every win rate is inside its D7 band. No tile stays on screen after a win (327 wins, checked 1 s after each). Blocked taps: 0, because the bots never tap a covered tile. No change to stage sizes proposed.
  Questions for the architect: (1) GC: accept the HUD's per-tap allocation (about 390 B, plus rare spikes) as event work and keep the 0 B target for steady frames, or move the HUD to a non-allocating path (revisits D6)? (2) T5: is a bot-run log enough for the builder's part, with the human playtest still open? (3) T7 is still the device check for GC.
- 2026-10-11: Architect ACCEPTS T5 (cbb28a7) as the bots-and-bands ticket. Evidence read: docs/playtests/2026-10-11.md (labelled as a bot run, honest), gc-standalone.txt. Claims match: every win rate is inside its D7 band (e.g. greedy stage 5 at 76% in 64 to 80%; uniform stage 3 at 24% in 17 to 33%), no tile stays after a win (327 wins checked), blocked taps 0 because bots never tap covered tiles. Spot-check of my own: the log's greedy and uniform rates for stages 1 to 5 against D7 and the test bands (PacingBands in MatchTests.cs), all inside. No stage-size change. Human playtest is NOT done and stays open as T9, which needs the user to play; the builder prepares a standalone build and a one-page note, and the log must say who played.
  Answers to the builder: (1) accept the per-tap allocation as event work, but pre-warm the HUD before the first tap, then re-measure; spikes after pre-warm reopen D6 (see D12). (2) Yes, the bot log is enough for the builder's part of T5. Also fix the confusing moment 1: move the Reduce Motion button so it cannot cover the popup (top corner), and capture it at 16:9 and 9:16. T1-T5 and T6 are done; next for the builder is the HUD pre-warm, the button move, then the T9 build.



