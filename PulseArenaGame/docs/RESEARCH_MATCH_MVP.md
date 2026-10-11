# Match MVP research (non-device, 2026-10-10)

Scope: what the store rules require, what the genre does, what benchmarks exist, and what our Unity code needs
before device testing. Each claim is marked **verified** (read from an official page on the date shown) or
**directional** (store listings, reviews, blogs, or third-party summaries). Nothing here is legal advice. Have counsel
review anything involving money before release.

## 1. Rules that shape the game (verified)

**Apple App Store Review Guidelines** (page shows "Last Updated: June 8, 2026"):
- 5.3.1–5.3.2: sweepstakes and contests must be sponsored by the developer. Official rules must be shown in the app.
- 5.3.3: apps may not use in-app purchase to buy currency for use with real-money gaming.
- 5.3.4: real-money gaming (betting, poker, casino) or lotteries need licenses where used, geo-restriction, and a free
  download. A lottery app needs consideration, chance and a prize.
- 3.1.1: apps offering loot boxes or other randomized virtual items for purchase must disclose the odds of each item
  type before purchase.

**Google Play real-money policy** (help page, no date shown; footer ©2026):
- Prohibited: games that accept money for a chance at a physical or monetary prize, and games that accept money and
  offer cash or real-world-value prizes. Also apps that accept wagers or in-app currency to gamble.
- Gambling is allowed only in approved, licensed, adult-rated apps, not sold as paid apps, and not using Play Billing.
- Non-game apps may run sweepstakes under disclosure rules. Game apps may not.
- Google's pilot page (effective June 4, 2026) says there are currently no active pilots.

**What this means for us**
- A cash-prize tile game would be prohibited on Google Play, not just regulated. Our no-cash rule is also a store
  compliance requirement. Keep it, and say so in `MATCH_MVP.md`.
- No sweepstakes, prize draws or raffles in any mode. We are a game app.
- If we ever sell randomized items, the odds must be shown before purchase (3.1.1). Simplest path: sell only fixed
  items, or earn boosters in play and never sell random packs.

## 2. Genre patterns (directional)

Sources: store listings, a level-design essay about four years old, and user reviews.
- **Tray:** a seven-slot tray is common. Overflow ends the run. Our default of 7 matches the pattern, but it is a
  starting value, not a benchmark.
- **Layers and blockers:** buried tiles stay locked until the tiles above clear. Some titles add keys or mystery
  tiles that open slots. Our stacked-cell model covers the first pattern.
- **Boosters:** undo, shuffle, and a tool that clears one triple on demand. Earned in play or bought.
- **Timers:** some titles add a per-level timer as pressure. Ours has none.
- **Difficulty:** gentle openings, then planned strategy. Designers avoid repeating mechanics over long stretches, and
  warn against look-alike boards.
- **Friction:** reviews repeatedly complain about forced ads and booster-or-ad gates. Treat this as a warning, not a
  measured finding.

**Implication:** our MVP has the tray and layers but no boosters. For the first release, add earned boosters only,
and no random paid packs. That avoids the odds rule and the real-money questions.

## 3. Benchmarks (directional, conflicting)

- A GameAnalytics 2026 summary (sponsored, third-party write-up, about 16,000 games): all-genre median about 22% day 1,
  about 4% day 7, about 0.7% day 30. Top tenth about 40%, 12%, 4%.
- A Segwise blog (late July 2026): typical day 1 near 27%, day 7 8–14%, day 30 3–7%.
- A Playio blog: top-quartile day 1 about 26–28%.
- No casual-puzzle or tile-match breakdown was found. Genre numbers would need GameAnalytics or Sensor Tower tables.

**Implication:** we can't set retention targets from genre data yet. We need our own events before any release:
runs started, levels completed, session length, and day 1 return per install.

## 4. Unity technical findings

**Input (verified):** Unity's manual says the legacy Input Manager is not recommended and may be removed in future
versions. The Input System package is the replacement. Our `MatchController` calls `Input.GetMouseButtonDown` and
`Input.mousePosition`. The laptop Console has shown an Input Manager deprecation warning. **Before device testing,
check Active Input Handling in Player Settings and move tap input to the Input System.** Touch should come from the
Input System's touchscreen or pointer API, not the legacy calls.

**UI (directional):** runtime IMGUI (`OnGUI`) is described by Unity staff and an issue-tracker reply as deprecated
in favour of UI Toolkit, with no removal date found. Our HUD uses `OnGUI`. That is fine for the MVP, but plan a move
to uGUI or UI Toolkit before polish. Ask Unity about the current status of runtime IMGUI.

**Materials (verified in our code, standard Unity behaviour):** `Renderer.material` creates a material instance for
that renderer. `TileView.SetTint` reads `material` for every tile on every tap, so each tap can create new material
instances. That costs draw calls, batching and memory. **Fix:** create one shared material per palette colour and one
dimmed variant, and assign `sharedMaterial` once at creation. Confirm the before and after in the Profiler (SetPass
calls and batches).

**Mobile performance (directional):** sources on the SRP Batcher and draw calls are mostly 2019–2021 forum threads with
mixed results. Our scene is small (at most 54 tiles), so draw calls are unlikely to be the bottleneck. The real risks
are per-tap material churn and IMGUI cost. Measure both on a device when available.

## 5. Game feel (directional)

Practitioner guidance (not the original books, which I did not verify): use screen shake, hit-stop, particles and
tweening sparingly, and only on real impacts. A freeze of roughly 50–100 ms on a heavy hit sells the force. Run freezes
through presentation timing, not by stopping game logic.

**Implication:** add a short presentation-only hit-stop (about 60 ms) on a clear. The Core clock must not pause, so
the freeze delays visuals only. Our feel criteria already cover tap feedback and clear feedback. Add this as an
explicit criterion.

## 6. Recommendations (for decision)

1. **Before device testing:** move tap input to the Input System. Check Active Input Handling.
2. **Next small code change:** shared materials in `TileView`, with one dimmed variant per colour.
3. **Keep no cash, prizes or sweepstakes.** Add the store-rule notes above to `MATCH_MVP.md`.
4. **Boosters:** earned in play for the first release. No random paid packs. Any paid item must be fixed-price.
5. **Feel:** add a presentation-only hit-stop on clear, and a feel criterion for it.
6. **Analytics:** define the events above before any release, even if they are only logged locally at first.
7. **UI decision:** choose uGUI or UI Toolkit for the HUD before polish, and ask Unity about runtime IMGUI.

## 7. What is still unknown

- Genre-level retention and revenue data for tile-match games.
- Primary game-feel sources (Swink's book, the Vlambeer talk, "Juice It or Lose It"). Cited from memory only.
- Current Google Play real-money policy wording beyond the pages above. Check the official text before any release.
- Whether Unity 6000.4 still supports both input backends by default in this project.

## Sources

- Apple App Store Review Guidelines (sections 3.1.1 and 5.3): https://developer.apple.com/app-store/review/guidelines/
- Google Play real-money gambling and games policy: https://support.google.com/googleplay/android-developer/answer/9877032
- Google Play real-money pilots page: https://support.google.com/googleplay/android-developer/answer/12918670
- Triple-match store listing (tray, boosters, timers): https://minigames.quartsoft.com/en/triple-match/
- Tile-match level-design essay (about four years old): https://room8studio.com/news/smart-casual-the-state-of-tile-puzzle-games-level-design-part-1/
- GameAnalytics 2026 benchmark summary (sponsored): https://gamedevreports.substack.com/p/gameanalytics-mobile-and-pc-game
- Playio retention guidance: https://blog.playio.co/mobile-game-kpi-performance-goals-retention-strategy
- Segwise retention benchmarks: https://segwise.ai/blog/mobile-gaming-app-user-retention-strategies.md
- Unity legacy Input Manager page (6000.4): https://docs.unity3d.com/6000.4/Documentation/Manual/InputLegacy.html
- Unity Input System migration (1.17): https://docs.unity3d.com/Packages/com.unity.inputsystem@1.17/manual/Migration.html
- Unity forum on the future of IMGUI: https://discussions.unity.com/t/plans-for-imgui-editor-ui-in-the-future/1556734
- Unity mobile optimization tips, part 2: https://unity.com/how-to/mobile-game-optimization-tips-part-2
- Game-feel practitioner guide: https://www.wayline.io/learn/game-feel/1
