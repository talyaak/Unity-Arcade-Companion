// Minimal stand-ins for the UnityEngine types used by PulseArena.Game.
// They exist only so the Game sources compile outside Unity. They do not model engine behaviour.
using System;

namespace UnityEngine
{
    public class Object
    {
    }

    public class ScriptableObject : Object
    {
    }

    public class Component : Object
    {
        public T GetComponent<T>() where T : Component
        {
            return null;
        }
    }

    public class Behaviour : Component
    {
        public bool enabled;
    }

    public class MonoBehaviour : Behaviour
    {
    }

    public static class Time
    {
        public static float deltaTime;
    }

    public static class PlayerPrefs
    {
        public static bool HasKey(string key) => false;
        public static string GetString(string key) => string.Empty;
        public static void SetString(string key, string value) { }
        public static void Save() { }
    }

    public static class Debug
    {
        public static void LogError(object message, Object context)
        {
        }
    }

    public sealed class CreateAssetMenuAttribute : Attribute
    {
        public string fileName;
        public string menuName;
    }

    public sealed class SerializeFieldAttribute : Attribute
    {
    }

    public sealed class HeaderAttribute : Attribute
    {
        public HeaderAttribute(string header)
        {
        }
    }

    public struct Rect
    {
        public Rect(float x, float y, float width, float height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public float x;
        public float y;
        public float width;
        public float height;
    }

    public struct Color
    {
        public Color(float r, float g, float b, float a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public float r;
        public float g;
        public float b;
        public float a;

        public static Color white => new Color(1f, 1f, 1f, 1f);
    }

    public class Texture2D : Object
    {
        public static Texture2D whiteTexture => new Texture2D();
    }

    public static class Screen
    {
        public static int width = 1080;
        public static int height = 1920;
    }

    public static class GUI
    {
        public static Color color = Color.white;

        public static bool Button(Rect rect, string text)
        {
            return false;
        }

        public static void Label(Rect rect, string text)
        {
        }

        public static void Box(Rect rect, string text)
        {
        }

        public static void DrawTexture(Rect rect, Texture2D texture)
        {
        }
    }

    public sealed class RequireComponentAttribute : Attribute
    {
        public RequireComponentAttribute(Type requiredComponent)
        {
        }
    }
}
