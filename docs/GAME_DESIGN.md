# Game design standard (Pulse Arena)

## Target

A good, playable, visually pleasing tile game, with strong game feel as the default. The current MVP is the
triple-match game (see `PulseArenaGame/docs/MATCH_MVP.md`). It is original. It has no cash, no prizes, no gambling,
and no third-party brands. The one-tap challenge game is a prototype and not the product.

## Player loop

Tap a free tile. It moves to the tray. Three of a kind clear, which scores and builds a combo. Clear the board to win
the stage. A full tray loses the stage. Then restart, or go to the next stage.

## Playable MVP (done when all of these are true)

1. A new player understands the rules on stage 1 without reading text. The only hint is "tap an uncovered tile".
2. Five stages play from start to finish in the Unity Editor, with no Console errors.
3. Every stage ends in a clear win or loss state, with a restart or next-stage control.
4. Every player action gives visible feedback in the same frame, plus a sound hook.
5. The juice standard below is met, with evidence (screenshots, clips, or Profiler numbers).
6. A phone test is recorded once an Android device or emulator is available.

Feel judgments ("it feels good") need a human playtest. Automated checks cannot confirm them.

## Juice standard

Starting values. Tune them in playtests and record any change in `docs/DECISIONS.md`.

**Motion**
- Tap squash: 0.12 s, scale to 0.9 and back.
- Move to tray: 0.25 s ease-out, with a small arc (peak 0.3 units).
- Clear: tiles pop to 1.15x, then shrink to zero over 0.2 s. A particle burst (at most 12 per tile). A "+points" popup rises 0.6 units and fades over 0.9 s.
- Hit-stop on clear: 60 ms of presentation delay only. The Core clock never pauses.
- Blocked tap: horizontal shake for 0.3 s, amplitude 0.12 units, with a brief red tint (0.15 s).
- Camera nudge on clears at combo 3 or more: amplitude at most 0.05 units, 0.15 s.
- Win: confetti in waves, at most 60 particles in total and never more than 40 live at once. The result panel slides in over 0.3 s.
- Loss: tray slots flash red for 0.3 s. Remaining tray tiles drop 0.5 units and shrink to zero (an alpha fade needs a transparent material, which waits for the art stage; D11).
- Easing: nothing the player watches moves linearly. Use ease-out for arrivals and ease-in for departures.

**Audio (placeholder)**
- Named hooks for tap, move, clear, blocked, win and lose.
- Silent or generated placeholder clips only, until licensed audio is chosen and logged.

**Comfort**
- A Reduce Motion toggle turns off camera nudge, shake, confetti and hit-stop. Moves and colour changes stay.
- Colour is never the only cue. Each symbol has its own shape as well as its colour.

**Performance**
- At most 40 live particles at any time.
- No per-frame allocations during Playing, after warm-up (Profiler: GC Alloc 0 B per frame).
- Shared materials only. No per-tile material copies.

## Visual standard

- **Palette:** the seven colours in `MatchController`. Covered tiles are dimmed to 45% brightness.
- **Shapes:** cube, sphere, cylinder and capsule, cycled by symbol.
- **Lighting:** one directional light, with soft shadows if the render pipeline supports them. No default clutter.
- **Framing:** the board, tray and popup fully visible at 16:9 and 9:16, with margin.
- **Final art:** AI-generated models, once the pipeline in `docs/ASSET_PIPELINE.md` exists. Until then, primitives are the art.

## Out of scope for now

Money of any kind, cash, prizes, sweepstakes, loot boxes with random paid items, licensed or third-party art or audio
without a complete ledger row, social features, and network features.


