# Trade-offs and decisions

| Decision | Why | Cost | Revisit when |
|---|---|---|---|
| Precompute the full target timeline from the seed | Determinism for replay and tests; the run can be verified | Schedule is generated up front (small, bounded by duration) | Runs get much longer |
| Empty taps have no penalty | Mis-taps are not punished | Spamming a lane is safe | Playtests show spamming |
| Multiplier read before combo increments | The rule is explicit and tested | The hit that reaches a step is paid at the lower multiplier | Designers want a different feel |
| Score floors at zero | Keeps the score readable | Players can't go negative to show a bad run | Never, for now |
| Float config fields in the Inspector, rounded through `ConfigValue` | Unity serializes floats; rounding keeps defaults equal to code | Only values with up to 6 decimals round-trip exactly | Tuning needs more precision |
| Pause in the adapter, not the core | The core stays a pure function of time and taps | The core alone does not know about pauses | Another client platform needs pausing |
| Clamp frame delta to 0.1 s | Long stalls cannot skip the run | A real 0.2 s hitch slows the run slightly | Playtests show visible drift |
| One accepted submission per player per round | Simple, fair | Players cannot improve a round | Competitive modes need best-of |
| Rejected submissions don't use the slot | A client bug can be fixed and resent | A cheater can resend until one submission passes validation (they still need a consistent log) | Server-side validation exists |
| Tie-break on time, then player id | Deterministic order | Combo tie-break is untested | Add a test with differing combos |
| Local leaderboard and simulated rounds only | Ships the logic without a backend | Nothing is shared between players | Backend exists |
| Fictional cosmetics | No licensed or borrowed assets | Placeholder names | Art pipeline starts |
