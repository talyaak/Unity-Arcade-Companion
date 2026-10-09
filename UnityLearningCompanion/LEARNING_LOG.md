# Learning log

Use one entry per session. Keep it short and factual: what you looked at, what the tool said, and what you checked yourself in Unity.

## Template

```
### YYYY-MM-DD: <topic>
- Scanned: <project or folder>
- Learned: <one or two concrete points>
- Verified in Unity: <what you opened or ran, or "not yet">
- Open question: <what you still do not understand>
```

## Example

### 2026-10-09: Core assembly rules
- Scanned: PulseArenaGame
- Learned: PulseArena.Core has noEngineReferences, so any `using UnityEngine` in Core would be a rule break.
- Verified in Unity: not yet
- Open question: does Unity's Test Runner need extra references for [Test] discovery in a new test asmdef?
