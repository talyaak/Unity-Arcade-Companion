using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace PulseArena.Game
{
    /// The welcome screen, the first scene of the game. It asks which mode to play: the classic match game, or physics mode
    /// (D14, prototype). Each button loads that scene. It keeps no game state, and the look is built in code, as in MatchHud.
    public sealed class ModeSelectScreen : MonoBehaviour
    {
        public const string ClassicScene = "Match";
        public const string PhysicsScene = "Physics";

        private static readonly Color Background = new Color(0.07f, 0.1f, 0.13f, 1f);
        private static readonly Color Ink = new Color(0.93f, 0.95f, 0.97f, 1f);
        private static readonly Color Muted = new Color(0.6f, 0.68f, 0.74f, 1f);

        private void Start()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            root.style.flexGrow = 1f;
            root.style.display = DisplayStyle.Flex;
            root.style.flexDirection = FlexDirection.Column;
            root.style.justifyContent = Justify.Center;
            root.style.alignItems = Align.Center;
            root.style.backgroundColor = Background;
            root.style.paddingLeft = 16f;
            root.style.paddingRight = 16f;

            root.Add(Text("Pulse Arena", 56f, Ink));
            root.Add(Text("Choose a game mode", 24f, Muted));
            root.Add(ModeButton("Classic", "Tiles, tray and triples. No physics.", ClassicScene));
            root.Add(ModeButton("Physics (prototype)", "The same rules, with the tiles in a physical pile.", PhysicsScene));
        }

        private static Label Text(string text, float size, Color color)
        {
            var label = new Label { text = text };
            label.style.fontSize = size;
            label.style.color = color;
            label.style.marginBottom = 12f;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            return label;
        }

        // A mode button with a line under it that says what the mode is. The button loads the scene.
        private static VisualElement ModeButton(string caption, string detail, string scene)
        {
            // Both boxes share one width, so the two buttons line up.
            var box = new VisualElement();
            box.style.width = Length.Percent(40f);
            box.style.alignItems = Align.Center;
            box.style.marginTop = 24f;

            var button = new Button { text = caption };
            button.style.width = Length.Percent(100f);
            button.style.fontSize = 28f;
            button.style.paddingTop = 14f;
            button.style.paddingBottom = 14f;
            button.clicked += () => SceneManager.LoadScene(scene);

            box.Add(button);
            box.Add(Text(detail, 16f, Muted));
            return box;
        }
    }
}
