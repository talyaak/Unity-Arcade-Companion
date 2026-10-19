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
        public static float time;
        public static int frameCount;
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

        public static void Log(object message)
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
        public Quaternion rotation = new Quaternion();
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

        public static Vector3 up => new Vector3(0f, 1f, 0f);
        public static Vector3 zero => new Vector3(0f, 0f, 0f);
        public static Vector3 down => new Vector3(0f, -1f, 0f);

        public static float Dot(Vector3 a, Vector3 b)
        {
            return a.x * b.x + a.y * b.y + a.z * b.z;
        }

        public static Vector3 Cross(Vector3 a, Vector3 b)
        {
            return new Vector3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
        }

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
        public float distance;
    }

    public class Mesh : Object
    {
        public Vector3[] vertices;
        public Vector3[] normals;
        public int subMeshCount;

        public void SetTriangles(int[] triangles, int submesh)
        {
        }

        public void RecalculateBounds()
        {
        }
    }

    public class MeshFilter : Component
    {
        public Mesh sharedMesh;
    }

    public class MeshRenderer : Renderer
    {
    }

    public class BoxCollider : Collider
    {
        public Vector3 center;
        public Vector3 size;
    }

    public class Collider : Component
    {
        public bool enabled = true;
        public Rigidbody attachedRigidbody;
    }

    public enum RigidbodyInterpolation { None, Interpolate, Extrapolate }

    public enum CollisionDetectionMode { Discrete, Continuous, ContinuousDynamic }

    public class Rigidbody : Component
    {
        public float mass = 1f;
        public float linearDamping;
        public float angularDamping;
        public RigidbodyInterpolation interpolation;
        public CollisionDetectionMode collisionDetectionMode;
        public bool isKinematic;
        public Vector3 linearVelocity;
        public Vector3 angularVelocity;
        public Vector3 position;

        public void AddTorque(Vector3 torque)
        {
        }

        public void AddForce(Vector3 force)
        {
        }
    }

    public class Renderer : Component
    {
        public Material material = new Material();
        public Material[] sharedMaterials;
        public Material sharedMaterial;
    }

    public class Material : Object
    {
        public Material(Shader shader)
        {
        }

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

        public void SetFloat(string name, float value)
        {
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
        public static int OverlapBoxNonAlloc(Vector3 center, Vector3 halfExtents, Collider[] results, Quaternion orientation)
        {
            return 0;
        }

        public static bool Raycast(Ray ray, out RaycastHit hit)
        {
            hit = default(RaycastHit);
            return false;
        }

        public static int RaycastNonAlloc(Ray ray, RaycastHit[] results, float maxDistance)
        {
            return 0;
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

        public static float Cos(float value)
        {
            return (float)System.Math.Cos(value);
        }

        public static float Sqrt(float value)
        {
            return (float)System.Math.Sqrt(value);
        }

        public static float Sin(float value)
        {
            return (float)System.Math.Sin(value);
        }

        public const float Deg2Rad = 0.0174532924f;
        public const float PI = 3.14159265f;

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
        public bool isPressed => false;
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

namespace UnityEngine
{
    public enum TextAnchor
    {
        UpperLeft,
        MiddleLeft,
        MiddleCenter
    }
}

namespace UnityEngine.UIElements
{
    // Compile-only stand-ins for the UI Toolkit calls used by MatchHud.
    public enum DisplayStyle { Flex, None }
    public enum FlexDirection { Column, Row }
    public enum Justify { FlexStart, Center }
    public enum PickingMode { Position, Ignore }
    public enum Align { Auto, FlexStart, Center, FlexEnd, Stretch }

    public sealed class IStyle
    {
        public DisplayStyle display;
        public Align alignItems;
        public float flexGrow;
        public FlexDirection flexDirection;
        public Justify justifyContent;
        public float paddingLeft;
        public float paddingRight;
        public float paddingTop;
        public float paddingBottom;
        public float marginLeft;
        public float marginRight;
        public float marginBottom;
        public float marginTop;
        public float opacity;
        public StyleLength width;
        public float fontSize;
        public Color color;
        public Color backgroundColor;
        public TextAnchor unityTextAlign;
    }

    public class VisualElement
    {
        public readonly IStyle style = new IStyle();
        public PickingMode pickingMode = PickingMode.Position;
        public readonly System.Collections.Generic.List<StyleSheet> styleSheets = new System.Collections.Generic.List<StyleSheet>();

        public void AddToClassList(string name)
        {
        }

        public void EnableInClassList(string name, bool enable)
        {
        }

        public void Add(VisualElement child)
        {
        }
    }

    public class Label : VisualElement
    {
        public string text;
    }

    public class Button : VisualElement
    {
        public string text;
        public event System.Action clicked;
    }

    public struct Length
    {
        public float value;

        public static Length Percent(float value)
        {
            return new Length { value = value };
        }
    }

    public struct StyleLength
    {
        public static implicit operator StyleLength(Length length)
        {
            return default(StyleLength);
        }
    }

    public class StyleSheet : Object
    {
    }

    public class UIDocument : MonoBehaviour
    {
        public PanelSettings panelSettings;
        public VisualElement rootVisualElement = new VisualElement();
    }

    public class PanelSettings : ScriptableObject
    {
    }
}

namespace UnityEngine
{
    // Compile-only stand-ins for the popup, particle and random calls used by the match juice.
    public struct Quaternion
    {
        public static Quaternion identity => new Quaternion();
        public static Quaternion Euler(float x, float y, float z) => new Quaternion();
    }

    public static class Random
    {
        public static float Range(float min, float max)
        {
            return min;
        }

        public static int Range(int min, int max)
        {
            return min;
        }
    }

    public enum TextAlignment
    {
        Left,
        Center,
        Right
    }

    public class TextMesh : Component
    {
        public string text;
        public float characterSize;
        public int fontSize;
        public TextAnchor anchor;
        public TextAlignment alignment;
        public Color color;
    }

    public class Shader : Object
    {
        public static Shader Find(string name)
        {
            return null;
        }
    }

    public struct Color32
    {
        public static implicit operator Color32(Color color)
        {
            return new Color32();
        }
    }

    public enum ParticleSystemRenderMode
    {
        Billboard
    }

    public enum ParticleSystemSimulationSpace
    {
        Local,
        World
    }

    public class ParticleSystemRenderer : Component
    {
        public ParticleSystemRenderMode renderMode;
        public Material sharedMaterial;
    }

    public class ParticleSystem : Component
    {
        public struct MainModule
        {
            public bool loop;
            public bool playOnAwake;
            public int maxParticles;
            public float startLifetime;
            public float startSpeed;
            public float startSize;
            public float gravityModifier;
            public ParticleSystemSimulationSpace simulationSpace;
        }

        public struct EmissionModule
        {
            public float rateOverTime;
        }

        public struct ShapeModule
        {
            public bool enabled;
        }

        public struct EmitParams
        {
            public Vector3 position;
            public Vector3 velocity;
            public Color32 startColor;
        }

        public MainModule main => new MainModule();

        public EmissionModule emission => new EmissionModule();

        public ShapeModule shape => new ShapeModule();

        public int particleCount => 0;

        public void Play()
        {
        }

        public void Emit(EmitParams emitParams, int count)
        {
        }
    }
}

namespace UnityEngine.UIElements
{
    // Compile-only additions for the match result panel.
}

namespace UnityEngine.SceneManagement
{
    public static class SceneManager
    {
        public static void LoadScene(string sceneName)
        {
        }
    }
}
