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
}
