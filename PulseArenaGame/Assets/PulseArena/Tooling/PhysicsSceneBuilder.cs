using PulseArena.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace PulseArena.Tooling
{
    /// Builds the physics-mode scene (D14): its own scene and controller, with the same HUD, camera, light and tile look as the
    /// grid game. It leaves the grid scene alone, and it does not add itself to Build Settings. A build that needs it lists it.
    public static class PhysicsSceneBuilder
    {
        private const string SceneFolder = "Assets/PulseArena/Scenes";
        private const string PhysicsScenePath = SceneFolder + "/Physics.unity";
        private const string TileMaterialPath = "Assets/PulseArena/Materials/Tile.mat";
        private const string PanelSettingsPath = "Assets/PulseArena/UI/MatchPanelSettings.asset";
        private const string HudStylePath = "Assets/PulseArena/UI/MatchHud.uss";

        [MenuItem("Pulse Arena/Build Physics Scene")]
        public static void BuildPhysics()
        {
            EnsureFolder(SceneFolder);

            // Opening a new scene unloads the scene that was open, and that scene (the grid game) holds these assets. So the
            // assets are loaded after the new scene is open. Loaded earlier, the stylesheet came back null.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var tile = AssetDatabase.LoadAssetAtPath<Material>(TileMaterialPath);
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            var style = AssetDatabase.LoadAssetAtPath<StyleSheet>(HudStylePath);
            if (tile == null || panel == null || style == null)
            {
                Debug.LogError("Pulse Arena: build the grid match scene first (Pulse Arena > Build Match Scene), so its assets exist.");
                return;
            }

            var root = new GameObject("PhysicsRoot");
            PhysicsMatchController controller = root.AddComponent<PhysicsMatchController>();
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("_tileMaterial").objectReferenceValue = tile;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // The HUD is the same UIDocument, PanelSettings and stylesheet as the grid scene. It reads the physics controller.
            var hudObject = new GameObject("Match HUD");
            UIDocument document = hudObject.AddComponent<UIDocument>();
            document.panelSettings = panel;
            MatchHud hud = hudObject.AddComponent<MatchHud>();
            var hudSerialized = new SerializedObject(hud);
            hudSerialized.FindProperty("_physics").objectReferenceValue = controller;
            hudSerialized.FindProperty("_style").objectReferenceValue = style;
            hudSerialized.ApplyModifiedPropertiesWithoutUndo();

            // The camera pose and the look are the grid scene's (T10 step a): 60 degrees, field of view 35, a dark background.
            Camera camera = Camera.main;
            if (camera != null)
            {
                camera.transform.position = new Vector3(0f, 13f, -9f);
                camera.transform.rotation = Quaternion.Euler(60f, 0f, 0f);
                camera.fieldOfView = 35f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.07f, 0.12f, 0.16f, 1f);
            }

            // One directional light with soft shadows, the same as the grid scene.
            foreach (Light light in Object.FindObjectsByType<Light>())
            {
                if (light.type == LightType.Directional)
                {
                    light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                    light.intensity = 1f;
                    light.shadows = LightShadows.Soft;
                    break;
                }
            }

            EditorSceneManager.SaveScene(scene, PhysicsScenePath);
            AssetDatabase.SaveAssets();

            if (new SerializedObject(hud).FindProperty("_style").objectReferenceValue == null)
            {
                Debug.LogError("Pulse Arena: the physics HUD did not get its stylesheet (MatchHud.uss).", hud);
                return;
            }

            Debug.Log($"Pulse Arena: built {PhysicsScenePath} (not added to Build Settings).");
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
