// Minimal stand-ins for the UnityEngine types used by PulseArena.Game.
// They exist only so the Game sources compile outside Unity. They do not model engine behaviour.
using System;

namespace UnityEngine
{
    public class Object
    {
        public static void Destroy(Object obj)
        {
        }
    }

    public class ScriptableObject : Object
    {
    }

    public class Component : Object
    {
        private readonly Transform _transform = new Transform();

        public Transform transform => _transform;

        public GameObject gameObject => new GameObject("stub");

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

    public class Transform
    {
        public Vector3 position;
        public Vector3 localScale = new Vector3(1f, 1f, 1f);
        public Vector3 forward = new Vector3(0f, 0f, 1f);
        public Vector3 right = new Vector3(1f, 0f, 0f);
        public Vector3 up = new Vector3(0f, 1f, 0f);
    }

    public struct Vector3
    {
        public Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public float x;
        public float y;
        public float z;

        public float magnitude => (float)System.Math.Sqrt(x * x + y * y + z * z);

        public static Vector3 operator +(Vector3 a, Vector3 b)
        {
            return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        }

        public static Vector3 operator -(Vector3 a, Vector3 b)
        {
            return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        }

        public static Vector3 operator *(Vector3 a, float d)
        {
            return new Vector3(a.x * d, a.y * d, a.z * d);
        }

        public static Vector3 Lerp(Vector3 a, Vector3 b, float t)
        {
            return new Vector3(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t);
        }
    }

    public struct Ray
    {
        public Ray(Vector3 origin, Vector3 direction)
        {
            this.origin = origin;
            this.direction = direction;
        }

        public Vector3 origin;
        public Vector3 direction;
    }

    public struct RaycastHit
    {
        public Collider collider;
    }

    public class Collider : Component
    {
    }

    public class Renderer : Component
    {
        public Material material = new Material();
        public Material sharedMaterial;
    }

    public class Material : Object
    {
        public Material()
        {
        }

        public Material(Material source)
        {
        }

        public Color color;

        public bool HasProperty(string name)
        {
            return false;
        }

        public void SetColor(string name, Color value)
        {
        }
    }

    public struct Vector2
    {
        public Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public float x;
        public float y;
    }

    public enum PrimitiveType
    {
        Sphere,
        Capsule,
        Cylinder,
        Cube,
        Plane,
        Quad
    }

    public class GameObject : Object
    {
        public GameObject(string name)
        {
            this.name = name;
        }

        public string name;

        private readonly Transform _transform = new Transform();

        public Transform transform => _transform;

        public static GameObject CreatePrimitive(PrimitiveType type)
        {
            return new GameObject(type.ToString());
        }

        public T AddComponent<T>() where T : Component, new()
        {
            return new T();
        }
    }

    public class Camera : Component
    {
        public static Camera main = new Camera();
        public float fieldOfView = 60f;
        public float aspect = 1f;

        public Ray ScreenPointToRay(Vector3 position)
        {
            return new Ray(position, new Vector3(0f, 0f, 1f));
        }
    }

    public static class Input
    {
        public static Vector3 mousePosition;

        public static bool GetMouseButtonDown(int button)
        {
            return false;
        }
    }

    public static class Physics
    {
        public static bool Raycast(Ray ray, out RaycastHit hit)
        {
            hit = default(RaycastHit);
            return false;
        }
    }

    public static class Mathf
    {
        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        public static float Clamp01(float value)
        {
            return value < 0f ? 0f : value > 1f ? 1f : value;
        }

        public static float Sin(float value)
        {
            return (float)System.Math.Sin(value);
        }

        public const float Deg2Rad = 0.0174532924f;

        public static float Max(float a, float b)
        {
            return a > b ? a : b;
        }

        public static float Abs(float value)
        {
            return value < 0f ? -value : value;
        }

        public static float Min(float a, float b)
        {
            return a < b ? a : b;
        }

        public static float Tan(float value)
        {
            return (float)System.Math.Tan(value);
        }

        public static float Atan(float value)
        {
            return (float)System.Math.Atan(value);
        }
    }
}

namespace UnityEngine.InputSystem
{
    // Compile-only stand-ins for the Input System calls used by MatchController.
    public sealed class ButtonControl
    {
        public bool wasPressedThisFrame => false;
    }

    public sealed class Vector2Control
    {
        public Vector2 ReadValue()
        {
            return default(Vector2);
        }
    }

    public class Pointer
    {
        public static Pointer current => null;

        public ButtonControl press = new ButtonControl();
        public Vector2Control position = new Vector2Control();
    }
}
