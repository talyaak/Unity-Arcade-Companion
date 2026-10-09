// Minimal stand-ins for the UnityEditor and UnityEngine types used by the Companion's Editor shell.
// They exist only so the shell compiles outside Unity. They do not model editor behaviour.
using System;

namespace UnityEditor
{
    public class EditorWindow
    {
        public static T GetWindow<T>(string title) where T : EditorWindow, new()
        {
            return new T();
        }
    }

    public sealed class MenuItemAttribute : Attribute
    {
        public MenuItemAttribute(string path)
        {
        }
    }
}

namespace UnityEngine
{
    public static class Application
    {
        public static string dataPath = string.Empty;
    }

    public static class GUILayout
    {
        public static void Label(string text)
        {
        }

        public static bool Button(string text)
        {
            return false;
        }

        public static string TextField(string text)
        {
            return text;
        }
    }
}
