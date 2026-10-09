# Limitations (honest list)

- **Unity not verified.** Core is tested with `dotnet test`. The Unity project has not been opened, asmdef files and `.meta` files are generated only on first open, and the test asmdef has not been checked against the Unity docs.
- **Adapter behaviour is untested.** Pause, focus loss, and frame clamping run only in a Unity player, which has not been run.
- **No scene, UI, or touch input.** The game cannot be played yet.
- **No persistence.** Profile, leaderboard, and config history live in memory and are lost on exit.
- **Cheating is not prevented.** Local replay checks consistency only. A tampered client with a made-up but consistent tap log passes.
- **Performance is unmeasured.** See `PERFORMANCE.md`.
- **Combo tie-break is untested** (see `TRADEOFFS.md`).
- **Cosmetics and names are placeholders.**
