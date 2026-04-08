using UnityEngine;

public class HitmarkerOverlay : MonoBehaviour
{
    static HitmarkerOverlay instance;
    static float hitUntilTime;

    public float showTime = 0.24f;
    public float size = 38f;
    public float thickness = 3f;
    public float gap = 7f;
    public float popScale = 1.18f;
    public Color color = new Color(1f, 1f, 1f, 0.95f);

    Texture2D pixel;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap() => EnsureInstance();

    static void EnsureInstance()
    {
        if (instance != null) return;
        var go = new GameObject("HitmarkerOverlay");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<HitmarkerOverlay>();
    }

    public static void NotifyHit()
    {
        EnsureInstance();
        float candidate = Time.unscaledTime + instance.showTime;
        if (candidate > hitUntilTime)
            hitUntilTime = candidate;
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        pixel = new Texture2D(1, 1);
        pixel.SetPixel(0, 0, Color.white);
        pixel.Apply();
    }

    void OnGUI()
    {
        if (Time.unscaledTime > hitUntilTime) return;
        float life = Mathf.Clamp01((hitUntilTime - Time.unscaledTime) / Mathf.Max(showTime, 0.001f));
        float alpha = life * life * (3f - 2f * life);
        Color c = color;
        c.a *= alpha;
        GUI.color = c;
        float cx = Screen.width * 0.5f;
        float cy = Screen.height * 0.5f;
        float scale = Mathf.Lerp(1f, popScale, life);
        float arm = size * 0.5f * scale;
        float g = gap * scale;
        DrawSegment(cx - g, cy - g, -arm, -arm);
        DrawSegment(cx + g, cy - g, arm, -arm);
        DrawSegment(cx - g, cy + g, -arm, arm);
        DrawSegment(cx + g, cy + g, arm, arm);
        GUI.color = Color.white;
    }

    void DrawSegment(float x, float y, float dx, float dy)
    {
        float len = Mathf.Sqrt(dx * dx + dy * dy);
        if (len <= 0.001f) return;
        float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
        Matrix4x4 old = GUI.matrix;
        GUIUtility.RotateAroundPivot(angle, new Vector2(x, y));
        GUI.DrawTexture(new Rect(x, y - thickness * 0.5f, len, thickness), pixel);
        GUI.matrix = old;
    }
}
