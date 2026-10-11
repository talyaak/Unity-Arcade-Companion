# Mobile performance guide (plan, not measured)

**Nothing here has been measured on a device.** Treat each item as a hypothesis to check with the Unity Profiler and on hardware.

## Hot path

Each frame the controller calls `FrameTime.Clamp` and `ChallengeSession.Advance`. Advance walks the schedule forward from an index and scans the lane array. Both are O(lanes) per call with no allocation expected.

Check in the Profiler:
- No GC allocations per frame during Playing (`ProcessEventsUpTo` and `TapLane` should allocate nothing).
- `ChallengeController.Update` stays well under 1 ms on a mid-range device.

## Known allocation sources to review

- `ProcessEventsUpTo` builds no collections, but `Standings()` and `ExportText()` do. Keep them off the frame path.
- `TournamentRound.Standings` uses LINQ and allocates. It is called at the results screen only.
- `DiagnosticsLog.Record` allocates a node per event. Keep logging out of per-frame code.

## Targets (to confirm)

- 60 fps on the target device class, with the run in Playing state.
- Cold start to first countdown below an agreed budget (not yet defined).
- Input latency from touch to visual response measured end to end (not yet measured; requires a device and a camera or high-speed timing).

## Things not yet built that will matter

- Rendering of targets and effects (not started).
- Audio sync: the run uses the frame clock, so audio latency is not part of the timeline. Any audio cue must be scheduled against the session clock, and this has not been designed.
