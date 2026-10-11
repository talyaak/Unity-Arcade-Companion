# Decisions (Pulse Arena)

Owner: architect. Each entry has an id, a status, the reasons, and what would change it.

## D1: The MVP is the triple-match game. Status: decided (2026-10-11)

The product is the triple-match tile game (`PulseArenaGame/docs/MATCH_MVP.md`). The one-tap challenge game stays in the
repo as a prototype with its scene and tests, and is not the product.
Would change it: a playtest showing the match loop is not fun after the juice pass (T3) and pacing (T5).

## D2: Input System, Active Input Handling = Both. Status: decided (2026-10-11)

Taps use the Input System package. Active Input Handling stays on **Both** because the IMGUI HUD (`OnGUI`) and the
challenge prototype need the legacy backend until T4 removes `OnGUI` from the match scene.
Would change it: after T4 lands and the challenge scene is either ported or removed, set it to Input System only.

## D3: One shared material per palette colour and dimmed state. Status: decided (2026-10-11)

Tiles use shared materials: one per palette colour, one per dimmed variant, never a per-tile copy. This keeps the
material count flat across taps (T1 check) and supports batching.
Would change it: final art (AI-generated models) needing per-tile variation. Then use a MaterialPropertyBlock, not
`renderer.material`.

## D4: Boosters. Status: OPEN, no boosters in the MVP

Proposal: if boosters are added, they are **earned only** by play (for example, a clean stage). No purchase of any
kind, no random packs, no paid items. Decision on whether to add them is deferred until pacing (T5) and the playtest
show a need. Anything touching money goes to the user first.

## D5: Render pipeline = Built-in for the MVP. Status: decided (2026-10-11)

The builder records Built-in or URP in T2, with the evidence (Graphics settings screenshot or MCP reading).
Constraint for the choice: soft shadows on one directional light, and shared materials from D3. Do not migrate
pipelines without a decision entry here.

Builder reading (2026-10-11, T2): Built-in. `get_graphics_settings` returns no default render pipeline and
`usingScriptableRenderPipeline: false`; `Tile.mat` uses the Standard shader. The Match scene has one soft-shadow
directional light. Evidence: `docs/evidence/T2/graphics-settings.txt`.

## D6: HUD framework = UI Toolkit. Status: decided (2026-10-11), T4

Reasons:
- Unity 6000.4 ships it built in (`com.unity.modules.uielements` is already in the manifest). uGUI would need the
  `com.unity.ugui` package plus a TextMeshPro essentials import, which adds files to review.
- Flex layout and USS handle landscape and 9:16 without per-aspect rects, and `PanelSettings` scale-with-screen plus
  safe-area padding covers the framing rule in `GAME_DESIGN.md`. Our current IMGUI rects are the thing to replace.
- The HUD is small: score, combo, tray count, one message, one button. It can be built in code from `VisualElement`s
  in the Tooling scene builder, so there is no hand-edited UXML to drift.
- Buttons get mouse and touch through the panel with the Input System. The scene needs one `UIDocument` and one
  `PanelSettings` asset (no EventSystem or uGUI module involved).
Scope boundary: the "+points" popup, particles and the tile shake stay in world space (3D). They are not HUD.
Rules for the builder: update a label's text only when its value changes (GC target, 0 B per frame in Playing); no
`OnGUI` in the match scene; Reduce Motion also disables any USS transition on the result panel.
Would change it: (a) the Profiler shows UI Toolkit allocations in Playing that cannot be removed by update-on-change;
(b) text rendering or touch problems on the Android device run (T7); (c) a need for world-space or per-tile UI
that uGUI does this better. If so, record the reading here and switch to uGUI with a new decision.
`n## D7: Stage pacing target bands. Status: decided (2026-10-11)

Source: `MatchSimulator`, seeds 1..200, 7-slot tray, taps 0.5 s apart. Two bots bracket the player: **uniform** (taps
any free tile, a careless player) and **greedy** (prefers the symbol already in the tray, a careful player). Real
players should land between them, nearer greedy.

| Stage | Measured greedy / uniform | Greedy band | Uniform band | Intent |
|---|---|---|---|---|
| 1 | 100% / 100% | 92-100% | 92-100% | Teach. Almost no one loses. |
| 2 | 99% / 73% | 91-100% | 65-81% | Careless play can lose. |
| 3 | 96% / 25% | 88-100% | 17-33% | Paying attention matters. |
| 4 | 84% / 4% | 76-92% | 0-12% | Real challenge. |
| 5 | 72% / 0% | 64-80% | 0-8% | Hard. Careful play wins about 7 in 10. |

Tolerance: bands are the measured value plus or minus 8 points. The standard error at 200 seeds is at most 3.5 points,
so 8 is about 2.3 standard errors. The seeds are fixed, so the test is deterministic; the tolerance exists so that a
small rule change does not trip it by noise, while a real difficulty shift does.
Tests: `Pacing_EveryStage_StaysInsideItsTargetBand` and `Pacing_GreedyWinRate_NeverRisesFromOneStageToTheNext`
in `MatchTests.cs`.
Stage sizes stay unchanged until the builder's playtest log (`docs/playtests/`) exists. Bot numbers are not player numbers.
Would change it: playtest times or losses that disagree with the bands. Then change the stage sizes or the bands,
with the evidence quoted here.

Note: the harness runs on .NET 8 in the csproj but this laptop has only the .NET 10 runtime. Run
`$env:DOTNET_ROLL_FORWARD='Major'` before `dotnet test`.

## D8: Asset pipeline is a later-stage plan. Status: decided (2026-10-11, user)

`docs/ASSET_PIPELINE.md` is approved as a plan for a later stage. No generator work, generated assets or ledger rows
until the user starts the asset stage. Primitives stay the art. Known gap: four primitive shapes for seven symbols, so
three symbols differ by colour only; the art stage must fix this. Tile scale 0.9 units is a T2 task.

Decision on D5: stay on the Built-in pipeline for the MVP. The evidence (T2) shows Built-in with a soft-shadow directional
light and shared Standard materials, which meets the visual standard. A pipeline migration costs a material and shader
pass for no player-visible gain right now. Would change it: the art stage needing URP shaders or post-processing, or a
device run (T7) showing Built-in cannot hold frame time. Then add a new decision and migrate with the migrate-birp-to-urp steps.

## D9: Juice confetti limit. Status: decided (2026-10-11)

`GAME_DESIGN.md` had two limits that read as a conflict (60 total vs 40 live). Reading: confetti plays in waves, at most
60 particles in total per win and never more than 40 live at any moment. Builder asked (Q2); this is the answer.

## D10: CoreTests baseline. Status: decided (2026-10-11)

The current count is 109 (107 plus the two pacing tests). Older text that says 107 is out of date.

## D11: T3 deviations. Status: decided (2026-10-11)

- Loss "fade" is a shrink to zero for now. Reason: an alpha fade needs a transparent material, which breaks the shared-material rule (D3) until the art stage. Revisit with the art stage.
- GC Alloc 0 B per frame is **not yet met or disproved**. In the Editor the baseline is 9,013 B per frame even with every match component disabled, so the Editor overhead hides the game. The target stays. It is measured in a standalone development build; tap frames spike (up to about 611 KB) and need a look there.
- FramingMargin stays at 1.10 (4.5% bottom at 16:9, 4.9% sides at 9:16). Safe-area handling is revisited on the device run (T7).

## D12: GC target refined from the standalone measurement. Status: decided (2026-10-11)

Evidence: `docs/evidence/T3/gc-standalone.txt` (standalone development build, 8,999 frames, 381 taps).
- Steady frames (idle and animation): median and p90 0 B. The 0 B target is met there.
- Tap frames: median 2.7 KB, max 10.7 KB. The frame after a tap: about 390 B (HUD labels). Accepted as event work, not per-frame work. The rule is now: 0 B on frames with no tap, at most 16 KB on a tap frame (measured max 12.4 KB after the pre-warm; raised from 12 KB in the same pass), and no frame over 50 KB after warm-up.
- Six spike frames of 593 KB to 1.47 MB all fall in the first 300 of 8,999 frames, one frame after a tap, and none after. That is warm-up behaviour (the HUD's first label and text layout), not a steady cost. They are not accepted as is: the HUD must be pre-warmed before the first player tap (set each label through its digits and strings once at scene start or level load). Then the spikes must be gone in a re-measure.
- D6 stays: UI Toolkit. The "would change" trigger (a) fires only if spikes persist after pre-warming. Then record the reading and revisit.
- The Editor reading (9,013 B per frame baseline) is not a game cost and is not used.
- Android device numbers (T7) are still needed; desktop numbers are not phone numbers.

D12 clarification (2026-10-11, builder question): the 16 KB tap-frame limit covers board taps (move, clear, win, loss). Level-start frames (Retry and Next stage presses, which build the board) are a separate category with a limit of 50 KB. The measured 9 to 37 KB (median 31 KB) passes. Reason: they happen once per stage behind a result panel, not during play, and 54 tile objects per level is the design. No tile pooling now. Revisit if the Android run (T7) shows a visible hitch at level start; then pool the tile objects and re-measure.

## D13: T10 polish pass amends D8 and D3. Status: decided (2026-10-11)

Trigger: T9 human playtest (one player, the user) said the feel is good and asked for a more real-game look, a camera tilted up for 2.5D, portrait as the default run, and better UI bars and buttons.
- **D8 amended:** the asset stage (imported or AI-generated assets) is still not started, and no ledger rows exist. T10 may build **procedural art in code** (rounded slab tiles, flat emblems made from generated geometry). That is original work, not an imported asset, so the ledger does not apply. Anything imported, downloaded or generated by a tool still needs a full ledger row first.
- **D3 amended:** one shared material per palette colour and dimmed state still holds. Emblems add one shared emblem material per state. The material count must stay flat across taps (re-check with the same `FindObjectsOfTypeAll<Material>` count).
- **Portrait default:** Player Settings to portrait, because the user asked and the target is a phone. Landscape must still frame correctly (T10 captures both).
- **Known gap closed when T10 lands:** the silhouette gap (4 primitive shapes for 7 symbols) is closed if the seven emblems are distinct shapes, readable at 576 x 1024. Colour must never be the only cue.
- **Originality:** the reference notes (`docs/research/T10-reference.md`) are for layout and feel only. Emblems, names, colours and UI must be original and not copied or traced from any reference image or game. No timers, coins, currency, shop, lives, lock icons or boosters (D4).
- **Unchanged:** stage sizes and D7 bands, D12 GC limits, Reduce Motion behaviour, UI Toolkit (D6). T2 framing evidence is superseded by the T10 captures.

D12 clarification 2 (2026-10-11, T10 step d): the result-card slide after a win or loss allocates up to 2 KB a frame for about 18 frames (UI Toolkit style updates, 0.3 s). Accepted as event work: frames within 0.5 s after a win or loss may allocate up to 4 KB. Measured: 56 such frames, 44,592 B in total, none over 2 KB. Steady frames outside those windows stay at 0 B. Revisit if the Android run (T7) shows a hitch during the slide.

## D14: Physics tiles are a new mode beside the grid game. Status: decided (2026-10-11, user chose "new mode")

Request (user, via the builder): pressable tiles that move freely under gravity, as rigid bodies that slide, tilt and pile up. The user chose option (b): a new mode next to the grid game, not a replacement.
- **Grid game unchanged:** its rules, generator, replay check, D7 bands, bots and tests stay as they are. It remains the stable MVP.
- **Core stays the rule authority** (hard rule: Game shows what Core decides). Physics mode adds one Core hook, done and tested (114 of 114 pass): `MatchBoard.ReportFreeTiles(ids)` and `MatchTile.Loose(id, symbol)`. Game reports which tiles have nothing resting on them. Core decides every tap (Blocked if not in the latest report), the tray, triples, combo, score, win and lose. Before the first report, the grid rule decides, so the existing mode is untouched.
- **What physics mode gives up, on purpose:** no winnable-by-construction guarantee, no replay verification (the free set is not in the tap log and physics is not deterministic), and the D7 bands and dotnet bots do not apply. Pacing for this mode is tuned by playing it. Do not claim any win rate for it without a measured run.
- **Rules for the builder:**
  1. Prototype first, as its own scene with its own controller. Do not edit the grid game's scene or controller except to add a mode entry point.
  2. Game computes the free set (nothing resting on top of the tile, with a stated rule and threshold, for example no other tile in contact above it) and reports it to Core after the pile settles and whenever it changes. Tiles that are still moving fast count as not free; state the speed threshold.
  3. Same tile look (T10), same symbols, same shared materials (D3 as amended), same HUD and tray, same Reduce Motion, same D12 GC limits, and a cap on rigid bodies (start at 60 or fewer, stage-1 and stage-2 sizes first).
  4. Symbol counts stay multiples of three. Tile symbols are spawned from a seed, so a level can be recreated, but the pile is not deterministic.
  5. Original work only. No physics-game assets imported.
- **Evidence expected:** a short capture or frame set of the pile settling, the free set read back against what is visibly uncovered, a Blocked tap on a covered tile, a win and a loss, the material count flat, and a GC reading during the pile settling (it will exceed the grid mode's steady 0 B; record it, do not hide it).
- **Would change:** if the prototype feels worse than the grid game, or cannot hold frame time on the Android run (T7), keep it as an experiment and do not ship it.


## D15: Every level uses every symbol; stage 1 cannot be lost; bands re-measured. Status: decided (2026-10-11)

Found by the builder in T11. (1) A tray of 7 holds at most 2 of each symbol without a triple, so a level can be lost only with 4 or more symbols. Stage 1 (3 symbols) cannot be lost, in either mode. Accepted on purpose: stage 1 is the teaching stage (D7 intent). A test now states it. (2) `MatchLevelGenerator` drew each triple's symbol independently, so about half of the stage 2 layouts had only 3 symbols and could not be lost, and higher stages could silently lose a kind. Fixed in Core: when there are enough triples, every symbol appears at least once (the first triples take distinct symbols, then the triples are shuffled). Test: `Generate_EveryStageUsesEverySymbol_ForEverySeed`.
Effect on D7 (seeds 1..200): greedy barely moves; uniform drops, as expected, because levels are no longer accidentally easy.

| Stage | Greedy (was) | Uniform (was) | Greedy band | Uniform band |
|---|---|---|---|---|
| 1 | 100% (100) | 100% (100) | 92-100% | 92-100% |
| 2 | 97% (99) | 54% (73) | 89-100% | 46-62% |
| 3 | 94% (96) | 7% (25) | 86-100% | 0-15% |
| 4 | 85% (84) | 0% (4) | 77-93% | 0-8% |
| 5 | 76% (72) | 0% (0) | 68-84% | 0-8% |

Bands are the new measured value plus or minus 8 points, as in D7. The Core band test caught the change and was updated in this commit; 116 of 116 tests pass. A careless random tapper now loses from stage 3 on; careful play is unchanged. The human playtest (T9) saw no loss on stages 1 to 4 with careful play, which still fits. This changes grid levels for the same seed, so older seed numbers in the notes no longer match their old layouts.


## D16: T11 round 2 rulings. Status: decided (2026-10-11)

- **Pile:** the round 2 captures read as a scattered, tilted pile (tiles overlapping, two or three deep, some on the rim). Heap enough for a prototype. Whether it feels good is for a human playtest.
- **Dense ray check:** accepted as the implementation check for the free-set rule (49 rays across the whole 0.72 footprint, 0 disagreements in 9 runs). It was designed after the sparse checks disagreed, and it samples the same box the rule uses, so it shows the code matches the rule, not that the rule matches what a player sees. The player-facing test is the dim state, which comes from the same free set: a free tile is bright and a covered tile is dim, so a human playtest must say whether "blocked" ever feels unfair. The sparse results stay in the files.
- **Result-window GC:** frames after a win or loss measured 2.0 to 3.2 KB in the final set but 9.1 KB (win) and 5.2 KB (loss) in an earlier set, over the 4 KB allowance of D12 clarification 2. Accepted for the prototype only. Before physics mode could ship, the cause must be traced and the frame must meet 4 KB, or D12 is revised with evidence.
- **Settle allocations** (stage 2: about 10 to 13 KB over 17 to 21 frames): Core's `ReportFreeTiles` now reuses one set instead of building a new one on every report (116 of 116 tests pass). The builder re-measures to see how much that explains.
- **Outcomes:** 1 win and 6 losses on stage 2 in 7 runs is not a sample and no win rate is claimed. The same policy gave a different result at two aspect ratios, probably because the probe taps every 4 frames; not tested. Physics mode is not repeatable across frame rates, which is part of why it has no replay check.


## D17: Screens, win display and settings scope. Status: PROPOSED, awaiting the user's approval (2026-10-11)

Source: the builder's scope page (https://claude.ai/artifact/GfBvddZgsSeXaJoYumpCyL): title, stage select, pause, stage win, campaign complete, settings, how to play, about; saved progress; Build Settings gets the title and stage-select scenes. Architect check against the rules: no money, coins, shop, lives, timers, stars, boosters or lock icons (D4, D13); original art (D8 as amended); UI Toolkit (D6); 53 triples across five stages is right (4+7+10+14+18); mock scores are labelled examples; confetti stays within D9; physics stays out of the player menu (D14, D16). No hard-rule problem found.
Rulings and conditions, which apply once the user approves:
1. **Unlock and best-score rules live in Core, with tests, by the architect.** A `MatchProgress` class: which stages are open (stage 1 always; stage n opens when n-1 is won), best score per stage, a new-best flag on a win, run totals for the campaign screen, Reset, and a versioned string form for saving. No UnityEngine. Game stores the string in the existing key-value store and shows what Core says.
2. **Pause must stop the match clock.** The combo window runs on the time Game passes to Core, so a pause and an app-focus loss must freeze that time (and the physics, if ever shown). The builder shows evidence: combo does not expire during a pause.
3. **Haptics:** device only and unverifiable without T7. Recommend leaving it out of phase 3 until a device run exists; the Sound toggle controls the audio hooks, which are silent placeholders today, so it must say nothing misleading. The user decides.
4. **Challenge:** stays reachable from the title unchanged. It still uses OnGUI, so Active Input Handling stays on Both (D2) until it is ported or removed.
5. **Build Settings:** adding scenes needs the user's explicit approval, as the page says.
6. **Frame budgets:** scene loads count as level starts (50 KB). The 4 KB result-window rule (D12 clarification 2) applies to the new win display and must be measured in phase 2, as the page plans.
7. Stage sizes and D7/D15 bands do not change. Correction (builder's check): the earlier wording "reached rarely" was wrong. The greedy bot's stage rates (100, 97, 94, 85, 76 percent) multiply to about 59 percent for five wins in a row in one run, and a player can retry any stage, so a careful player reaches the campaign-complete screen after winning stage 5 once. A random tapper almost never does (0 percent on stages 4 and 5). Do not tune stage 5 for this without a playtest.
