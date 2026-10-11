# Match MVP (triple-match, 3D tiles)

**Decision:** the MVP is now an original triple-match game: 3D tiles, a tray, three-of-a-kind clears. The challenge
game stays in the repo as a prototype and keeps its scene and tests. Whether it stays in the build is an open decision.

## Rules

- **Board:** tiles sit in grid cells and can stack. A tile is **free** only if no remaining tile sits on top of it in
  the same cell. Core answers this (`MatchBoard.IsFree`); views never work it out themselves.
- **Tray:** a free tile tapped moves into the tray. Equal symbols group together.
- **Clear:** three of one symbol in the tray leave it, and the score rises.
- **Order inside one tap:** move, resolve the triple, then check for overflow. A tile that completes a triple in the
  last tray slot clears; it does not lose.
- **Lose:** the tray reaches capacity (default 7) without a triple.
- **Win:** the board and tray are both empty.
- **Scoring:** a clear pays `ClearPoints` (default 100) times a combo multiplier. A clear within `ComboWindowSeconds`
  (default 3) of the previous clear extends the combo. The multiplier is capped at `MaxComboMultiplier` (default 4).
  Moves cost nothing.

All of these values are this game's own starting points. They are not taken from any other game.

## Winnable by construction

`MatchLevelGenerator` gives every tile a clear index, and tiles cleared earlier sit on top of their cell. Tapping in
clear order always frees the next tile, so every generated level is winnable. Tests tap that order through the real
board, for every stage and 100 seeds, and check that the tray never holds more than two tiles.

## Difficulty (simulator, 200 seeds per stage; superseded by D15 in docs/DECISIONS.md: greedy 100/97/94/85/76, uniform 100/54/7/0/0)

| Stage | Symbols | Triples | Grid | Uniform random taps | Greedy (prefers the tray's symbol) |
|---|---|---|---|---|---|
| 1 | 3 | 4 | 3×3 | 100% | 100% |
| 2 | 4 | 7 | 4×4 | 73% | 99% |
| 3 | 5 | 10 | 5×5 | 25% | 96% |
| 4 | 6 | 14 | 5×5 | 4% | 84% |
| 5 | 7 | 18 | 6×6 | 0% | 72% |

These are bot numbers, not player numbers. The shape of the curve is useful; the exact values need playtesting before
they are trusted. Stage sizes are starting values.

## Replay

`MatchReplay.Verify` re-runs a level from its spec, seed, rules and tap log. It rejects taps that were blocked or
ignored. Like the challenge replay, it checks consistency only. It cannot tell a human from a bot.

## Money and fairness

- No cash, payouts, entry fees, wagers or cash-named currency, in this game or in anything planned on top of it.
- Tournaments, leaderboards and progression stay prize-free.
- A replay check is not an anti-cheat system. Competitive results would need server-side validation.

## Status

| Piece | Status |
|---|---|
| Core: board, tray, triples, scoring, combo, generator, replay, simulator | Built. Tested with dotnet: 107/107 pass, including the overflow-order edge case (a deliberate bug is caught) |
| Game: `MatchController` and `TileView` (3D tiles, tap, animation, HUD) | Written. Compile-checked against UnityEngine stubs only |
| Tooling: Pulse Arena > Build Match Scene | Written. **Not run in Unity** |
| Unity play-through (tap, clear, win, lose, touch) | **Not run** |
| Feel ("captivating") | **Not verified**. See the criteria below |

## Feel criteria (to check on the laptop, not by reading the code)

- A tap on a free tile moves it toward the tray in about 0.25 s, with no visible snapping.
- A blocked tap shakes that tile and nothing else moves.
- A clear shrinks the three tiles away and shows a `+points` popup for about 0.9 s.
- The tray re-lays out smoothly after each move or clear.
- Covered tiles look dimmed, and no free tile ever looks dimmed.
- Win and lose each show a message and one button.

## Open decisions

- Keep the challenge scene in Build Settings, or remove it.
- Final symbol shapes and palette. The current ones are primitives.
- Stage tuning from human playtests.
- Audio and particle feedback (none yet).
- Touch on a real device (deferred, along with the profiler numbers).
