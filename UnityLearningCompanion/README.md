# Unity Learning Companion

An Editor-only UPM package (`com.talyaak.unity-learning-companion`) that reads a Unity project and helps you learn it.
It finds assemblies, references and C# declarations, labels every claim as **verified** (read from the text) or
**inferred** (pattern-matched), flags rule breaks, and offers concept notes, missions and interview practice.

> **Status: Milestone 3 (scan and explain) built, Milestone 4 (missions, interview) partly built, Milestone 5 (LLM provider)
> offline only. Milestone 6 (install into Pulse Arena inside Unity) not done.**
> Core is tested with `dotnet test`. The Editor window is compile-checked against stubs. Nothing has run inside Unity.

## Verified vs. not verified

| Area | Status |
|---|---|
| asmdef parsing (name, references, platforms, noEngineReferences) | Implemented. Tested against the real `PulseArena.Core.asmdef` |
| C# declaration and import scan (line-based) | Implemented. Tested on fixtures and on the Pulse Arena project. Not a full C# parser |
| Project rule checks (engine-free asmdef importing UnityEngine; runtime importing UnityEditor) | Implemented and tested. The Pulse Arena project currently has zero warnings |
| Concept library, missions, interview scoring | Implemented and tested. Interview scoring is keyword coverage, a rough check |
| Redaction and payload policy | Implemented and tested. Pattern-based, so it is a last line of defence |
| Local explainer (offline) | Implemented and tested. No network |
| Remote LLM provider | **Interface only.** No provider, no key, nothing sent anywhere |
| Editor window | Written. **Compile-checked against UnityEditor stubs only.** Not opened in Unity |
| Unity Test Runner (EditMode) | **Not run.** Asmdef for tests is written from memory and not checked against Unity docs |
| Refresh | Full rescan on demand. `ScanCache` (hash-based) exists in Core for incremental use later, not wired into the window |

## Safety rules (enforced in code where possible)

- **Never writes project files.** The Core has no write API. The Editor shell only reads `Assets/`.
- **Scanned text is data.** Comments and strings are never executed or treated as instructions. A test checks that
  an injected "upload the project" comment produces no fact.
- **No source leaves the machine by default.** `PayloadPolicy.AllowSourceSnippets` is `false`. Payloads are capped at 2000 characters.
- **No keys in the package.** Remote providers would read a key from the user's own environment, not from this repo.
- **No Tripo or other asset-generation integration.** Out of scope for this package.

## Layout

```
UnityLearningCompanion/
  package.json                       UPM manifest (name, version, unity 2022.3)
  Editor/Core/                       Engine-free logic (asmdef noEngineReferences, Editor-only)
    AsmdefParser.cs, CSharpScanner.cs, ProjectScanner.cs   scan and rule checks
    Facts.cs, SourceFile.cs                                 fact model and content hash
    ConceptLibrary.cs, Missions.cs, Interview.cs           learning content and practice
    Redactor.cs, PayloadPolicy.cs, Explainer.cs            privacy and explanation
    ScanCache.cs                                           change detection (not wired yet)
  Editor/Shell/                      Thin EditorWindow (Window > Learning Companion)
  Tests/Editor/                      NUnit tests (Unity Test Runner and dotnet test)
  Harness/                           dotnet test project; ShellCompile.csproj compile-checks the window against stubs
```

## Run the tests (no Unity needed)

```bash
cd UnityLearningCompanion/Harness
dotnet test UnityLearningCompanion.Tests.csproj          # 17 tests
dotnet build UnityLearningCompanion.ShellCompile.csproj  # window compiles for net8.0 and netstandard2.1
```

The PulseArena-based tests look for `PulseArenaGame/` next to the repo and skip if it is missing.

## Install (not yet verified)

Add the package from disk in Unity: Package Manager → Add package from disk → select `UnityLearningCompanion/package.json`.
Then open Window → Learning Companion. Check the Console for errors on first import; Unity will generate `.meta` files.

## Known gaps

- The scanner is line-based. Generics, nested types, `#if` branches and multi-line declarations can be misread. Those findings are marked inferred where that matters, but not all of them are.
- asmdef parsing is regex-based, so an unusual asmdef layout could be misread. Empty results mean "not read", not "none exist".
- The local explainer matches keywords. It will not answer questions outside its concept library.
- Remote LLM integration is not built. Its privacy rules are in `PayloadPolicy`, and they are untested against a real provider.
