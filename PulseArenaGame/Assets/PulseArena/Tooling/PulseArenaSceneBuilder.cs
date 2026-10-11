using PulseArena.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

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
        private const string UiFolder = "Assets/PulseArena/UI";
        private const string PanelSettingsPath = UiFolder + "/MatchPanelSettings.asset";
        private const string HudStylePath = UiFolder + "/MatchHud.uss";

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

            // The HUD is one UIDocument with one PanelSettings. MatchHud builds its panel in Start.
            var hudObject = new GameObject("Match HUD");
            UIDocument document = hudObject.AddComponent<UIDocument>();
            document.panelSettings = EnsurePanelSettings();
            MatchHud hud = hudObject.AddComponent<MatchHud>();
            var hudSerialized = new SerializedObject(hud);
            hudSerialized.FindProperty("_match").objectReferenceValue = controller;
            hudSerialized.FindProperty("_style").objectReferenceValue = AssetDatabase.LoadAssetAtPath<StyleSheet>(HudStylePath);
            hudSerialized.ApplyModifiedPropertiesWithoutUndo();

            // A 2.5D view (T10, D13): a steeper look-down than the T2 baseline (55 degrees) and a narrower field of view, so the
            // stacks show their depth without leaning. FitCamera moves the camera along this view axis, so the framing holds at
            // any aspect ratio. The background is a dark solid, so the tiles and tray read against it.
            Camera camera = Camera.main;
            if (camera != null)
            {
                camera.transform.position = new Vector3(0f, 13f, -9f);
                camera.transform.rotation = Quaternion.Euler(60f, 0f, 0f);
                camera.fieldOfView = 35f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.07f, 0.12f, 0.16f, 1f);
            }

            // Exactly one directional light, with soft shadows. The new scene brings its own, so keep that one and
            // remove any others.
            Light sun = null;
            foreach (Light light in Object.FindObjectsByType<Light>())
            {
                if (light.type != LightType.Directional)
                {
                    continue;
                }

                if (sun == null)
                {
                    sun = light;
                }
                else
                {
                    Object.DestroyImmediate(light.gameObject);
                }
            }

            if (sun == null)
            {
                sun = new GameObject("Directional Light").AddComponent<Light>();
                sun.type = LightType.Directional;
            }

            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            sun.intensity = 1f;
            sun.shadows = LightShadows.Soft;

            EditorSceneManager.SaveScene(scene, MatchScenePath);
            AddToBuildSettings(MatchScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"Pulse Arena: built {MatchScenePath}.");
        }

        // One PanelSettings for the match HUD. It scales with the screen and uses a 1080 x 1920 reference, so landscape and
        // portrait both fit. Created once and reused on later builds.
        private static PanelSettings EnsurePanelSettings()
        {
            EnsureFolder(UiFolder);
            PanelSettings existing = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (existing != null)
            {
                return existing;
            }

            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1080, 1920);
            settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            settings.match = 0.5f;
            AssetDatabase.CreateAsset(settings, PanelSettingsPath);
            return settings;
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
