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

## D5: Render pipeline. Status: OPEN

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
