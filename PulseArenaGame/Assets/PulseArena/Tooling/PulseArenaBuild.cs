using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// Builds the game for WebGL (D18, Stage 0). Menu: Pulse Arena > Build WebGL. With the Editor closed, run it in batch:
///   Unity -batchmode -quit -projectPath PulseArenaGame -executeMethod PulseArenaBuild.BuildWebGL
/// It builds the scenes in Scenes, in that order, to PulseArenaGame/Builds/WebGL (ignored by git). Build Settings is not read
/// here, so its order stays as it is for the desktop builds (D18 item 3). Compression is off. The page is
/// Assets/WebGLTemplates/PulseArena: a full-window canvas, portrait-sized by default.
/// The class is in the global namespace so the -executeMethod name is exactly PulseArenaBuild.BuildWebGL, as D18 says.
public static class PulseArenaBuild
{
    private const string TemplateName = "PulseArena";
    private const string OutputFolder = "Builds/WebGL";
    private const int PortraitWidth = 1080;
    private const int PortraitHeight = 1920;

    // The first scene is the first screen. Match comes first because the link is for the match game (D18 item 3).
    private static readonly string[] Scenes =
    {
        "Assets/PulseArena/Scenes/Match.unity",
        "Assets/PulseArena/Scenes/Challenge.unity",
    };

    [MenuItem("Pulse Arena/Build WebGL")]
    public static void BuildWebGL()
    {
        foreach (string scene in Scenes)
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

        string outputPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutputFolder));
        var options = new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = outputPath,
            target = BuildTarget.WebGL,
            targetGroup = BuildTargetGroup.WebGL,
            options = BuildOptions.None,
        };

        BuildSummary summary = BuildPipeline.BuildPlayer(options).summary;
        bool succeeded = summary.result == BuildResult.Succeeded;
        string line = "[PulseArenaBuild] WebGL " + summary.result + ": " + summary.totalErrors + " errors, " +
            summary.totalWarnings + " warnings, " + summary.totalSize + " bytes, scenes " + string.Join(", ", Scenes) +
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
