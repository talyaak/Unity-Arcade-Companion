using PulseArena.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PulseArena.Tooling
{
    /// Creates the playable challenge scene and its default config asset. Safe to re-run:
    /// the config asset is reused if it exists, and the scene is rebuilt.
    public static class PulseArenaSceneBuilder
    {
        private const string ConfigFolder = "Assets/PulseArena/Config";
        private const string ConfigPath = ConfigFolder + "/DefaultChallenge.asset";
        private const string SceneFolder = "Assets/PulseArena/Scenes";
        private const string ScenePath = SceneFolder + "/Challenge.unity";

        [MenuItem("Pulse Arena/Build Challenge Scene")]
        public static void Build()
        {
            EnsureFolder(ConfigFolder);
            EnsureFolder(SceneFolder);

            ChallengeConfigAsset config = AssetDatabase.LoadAssetAtPath<ChallengeConfigAsset>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ChallengeConfigAsset>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var root = new GameObject("ChallengeRoot");
            var controller = root.AddComponent<ChallengeController>();
            root.AddComponent<ChallengeHud>();

            var serialized = new SerializedObject(controller);
            SerializedProperty configProperty = serialized.FindProperty("_config");
            configProperty.objectReferenceValue = config;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);

            if (configProperty.objectReferenceValue == null)
            {
                Debug.LogError("Pulse Arena: failed to assign ChallengeConfigAsset to ChallengeController._config.");
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"Pulse Arena: built {ScenePath} with config {ConfigPath}.");
        }

        // Adds the scene to Build Settings without removing scenes that are already there.
        private static void AddToBuildSettings(string path)
        {
            EditorBuildSettingsScene[] existing = EditorBuildSettings.scenes;
            foreach (EditorBuildSettingsScene s in existing)
            {
                if (s.path == path)
                {
                    return;
                }
            }

            var updated = new EditorBuildSettingsScene[existing.Length + 1];
            existing.CopyTo(updated, 0);
            updated[existing.Length] = new EditorBuildSettingsScene(path, true);
            EditorBuildSettings.scenes = updated;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            string name = System.IO.Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
