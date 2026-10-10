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
        private const string MatchScenePath = SceneFolder + "/Match.unity";
        private const string MaterialFolder = "Assets/PulseArena/Materials";
        private const string TileMaterialPath = MaterialFolder + "/Tile.mat";

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

        [MenuItem("Pulse Arena/Build Match Scene")]
        public static void BuildMatch()
        {
            EnsureFolder(SceneFolder);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var root = new GameObject("MatchRoot");
            MatchController controller = root.AddComponent<MatchController>();

            var serialized = new SerializedObject(controller);
            serialized.FindProperty("_tileMaterial").objectReferenceValue = EnsureTileMaterial();
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Look down at the board from the front, so the tray sits near the camera and the stacks are visible.
            Camera camera = Camera.main;
            if (camera != null)
            {
                camera.transform.position = new Vector3(0f, 13f, -9f);
                camera.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
            }

            EditorSceneManager.SaveScene(scene, MatchScenePath);
            AddToBuildSettings(MatchScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"Pulse Arena: built {MatchScenePath}.");
        }

        // One base material asset for tiles. Copied from the default primitive material, so it works in any pipeline.
        private static Material EnsureTileMaterial()
        {
            EnsureFolder(MaterialFolder);
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(TileMaterialPath);
            if (existing != null)
            {
                return existing;
            }

            GameObject template = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Material defaultMaterial = template.GetComponent<Renderer>().sharedMaterial;
            var material = new Material(defaultMaterial);
            Object.DestroyImmediate(template);
            AssetDatabase.CreateAsset(material, TileMaterialPath);
            return material;
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
