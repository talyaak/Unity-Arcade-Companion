using System.Collections.Generic;

namespace UnityLearningCompanion
{
    public sealed class Concept
    {
        public Concept(string id, string title, string summary, string[] keywords, string[] pitfalls)
        {
            Id = id;
            Title = title;
            Summary = summary;
            Keywords = keywords;
            Pitfalls = pitfalls;
        }

        public string Id { get; }
        public string Title { get; }
        public string Summary { get; }
        public string[] Keywords { get; }
        public string[] Pitfalls { get; }
    }

    /// Original explanations written for this tool. Not copied from Unity documentation.
    public static class ConceptLibrary
    {
        public static readonly IReadOnlyList<Concept> All = new List<Concept>
        {
            new Concept("asmdef", "Assembly definitions (asmdef)",
                "An asmdef makes its folder a separate compiled assembly. Code in it can only reference what its 'references' list names, and it can be restricted by platform. Dependencies between asmdefs are explicit and checked by the compiler.",
                new[] { "asmdef", "assembly", "references", "noEngineReferences" },
                new[] { "Forgetting a reference gives 'type not found' even when the namespace is right.", "noEngineReferences blocks UnityEngine, so anything that needs it must live elsewhere." }),
            new Concept("monobehaviour-lifecycle", "MonoBehaviour lifecycle",
                "Unity calls methods such as Awake, OnEnable, Start, Update and OnDisable on attached components. Awake runs before Start. Update runs once per frame, so per-frame work should be cheap and time-based logic should use the frame delta.",
                new[] { "MonoBehaviour", "Awake", "Start", "Update", "lifecycle" },
                new[] { "Reading another object in Awake can see it before its own Awake has run.", "Time.deltaTime can be large after a stall, so clamp it when logic depends on it." }),
            new Concept("scriptableobject", "ScriptableObject for data",
                "A ScriptableObject is an asset that holds data and is edited in the Inspector. It is useful for tuning values that designers change without touching scene objects.",
                new[] { "ScriptableObject", "asset", "CreateAssetMenu", "data" },
                new[] { "Runtime changes to a ScriptableObject persist in the editor session and can leak between play sessions." }),
            new Concept("editor-window", "EditorWindow tools",
                "An EditorWindow is a custom window inside the Unity Editor. Code in an Editor-only assembly can use UnityEditor APIs. Runtime assemblies cannot, and a build fails if they do.",
                new[] { "EditorWindow", "UnityEditor", "Editor-only", "tool" },
                new[] { "Importing UnityEditor from a runtime assembly breaks player builds." }),
            new Concept("upm-package", "UPM package layout",
                "A UPM package is a folder with a package.json at its root. Editor code goes under an Editor folder with an Editor-only asmdef. Keeping the engine-free logic in its own assembly makes it testable outside Unity.",
                new[] { "package.json", "UPM", "package", "Editor folder" },
                new[] { "The package.json name must be unique and follow reverse-domain form." }),
            new Concept("test-modes", "EditMode and PlayMode tests",
                "EditMode tests run without entering Play mode and are fast, so pure logic belongs there. PlayMode tests run inside the player loop and suit MonoBehaviour timing and scene behaviour.",
                new[] { "EditMode", "PlayMode", "Test Runner", "NUnit", "test" },
                new[] { "EditMode tests cannot rely on Update or coroutines running." })
        };
    }
}
