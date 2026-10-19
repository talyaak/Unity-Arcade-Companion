using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// Builds the game for WebGL (D18, Stage 0). Menu: Pulse Arena > Build WebGL. With the Editor closed, run it in batch:
///   Unity -batchmode -quit -projectPath PulseArenaGame -executeMethod PulseArenaBuild.BuildWebGL
/// It builds the scenes in Scenes, in that order, to PulseArenaGame/Builds/WebGL (ignored by git). Build Settings is not read
/// here, so its order stays as it is for the desktop builds (D18 item 3). Compression is off. The page is
/// Assets/WebGLTemplates/PulseArena: a full-window canvas, portrait-sized by default.
/// Pulse Arena > Build WebGL (Physics) builds only Physics.unity (D14, prototype) to PulseArenaGame/Builds/WebGL-Physics, a
/// separate folder, so the match build is not overwritten while it is served. Not in Build Settings either.
/// The class is in the global namespace so the -executeMethod name is exactly PulseArenaBuild.BuildWebGL, as D18 says.
public static class PulseArenaBuild
{
    private const string TemplateName = "PulseArena";
    private const string OutputFolder = "Builds/WebGL";
    private const int PortraitWidth = 1080;
    private const int PortraitHeight = 1920;

    // The first scene is the first screen: the welcome screen, which asks for the mode. Match and Physics are the two modes it
    // loads. Challenge stays in the list. Build Settings is not read here, so its order does not matter for this build.
    private static readonly string[] Scenes =
    {
        "Assets/PulseArena/Scenes/Start.unity",
        "Assets/PulseArena/Scenes/Match.unity",
        "Assets/PulseArena/Scenes/Physics.unity",
        "Assets/PulseArena/Scenes/Challenge.unity",
    };

    // Physics mode (D14) is a prototype outside Build Settings. Its own build and link, in its own folder, so the match
    // build at OutputFolder is never touched while it is served.
    private const string PhysicsOutputFolder = "Builds/WebGL-Physics";
    private static readonly string[] PhysicsScenes =
    {
        "Assets/PulseArena/Scenes/Physics.unity",
    };

    [MenuItem("Pulse Arena/Build WebGL")]
    public static void BuildWebGL()
    {
        Build(Scenes, OutputFolder);
    }

    [MenuItem("Pulse Arena/Build WebGL (Physics)")]
    public static void BuildWebGLPhysics()
    {
        Build(PhysicsScenes, PhysicsOutputFolder);
    }

    private static void Build(string[] scenes, string outputFolder)
    {
        foreach (string scene in scenes)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scene) == null)
            {
                Fail("scene not found: " + scene);
                return;
            }
        }

        // In the open Editor, switch the target first and then run this method again. A switch made inside this call
        // reimports the project, which can end the call before the build starts. In batch mode the switch finishes first.
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL &&
            !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL))
        {
            Fail("could not switch the active build target to WebGL");
            return;
        }

        // Set here, so a batch build and a menu build use the same values.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.template = "PROJECT:" + TemplateName;
        PlayerSettings.defaultWebScreenWidth = PortraitWidth;
        PlayerSettings.defaultWebScreenHeight = PortraitHeight;

        string outputPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", outputFolder));
        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.WebGL,
            targetGroup = BuildTargetGroup.WebGL,
            options = BuildOptions.None,
        };

        BuildSummary summary = BuildPipeline.BuildPlayer(options).summary;
        bool succeeded = summary.result == BuildResult.Succeeded;
        string line = "[PulseArenaBuild] WebGL " + summary.result + ": " + summary.totalErrors + " errors, " +
            summary.totalWarnings + " warnings, " + summary.totalSize + " bytes, scenes " + string.Join(", ", scenes) +
            ", output " + outputPath;
        if (succeeded)
        {
            Debug.Log(line);
        }
        else
        {
            Debug.LogError(line);
        }

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(succeeded ? 0 : 1);
        }
    }

    private static void Fail(string reason)
    {
        Debug.LogError("[PulseArenaBuild] " + reason);
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(1);
        }
    }
}
