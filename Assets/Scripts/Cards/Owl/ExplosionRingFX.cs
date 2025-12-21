using UnityEngine;
using System.Collections;

public class ExplosionRingFX : MonoBehaviour
{
    [Header("Visual")]
    public Color ringColor = new Color(0.9f, 0.85f, 0.2f, 0.6f);
    [Range(8, 256)] public int segments = 64;
    [Tooltip("Final ring width (pixels-ish).")]
    public float lineWidth = 0.08f;

    [Header("Anim")]
    [Tooltip("How long the pulse lives.")]
    public float duration = 0.45f;
    [Tooltip("Final radius the ring reaches.")]
    public float finalRadius = 3.5f;
    [Tooltip("Start radius as a fraction of final (0.6 = start smaller then expand).")]
    [Range(0f, 1f)] public float startRadiusFraction = 0.6f;

    // ---- internals ----
    private LineRenderer _lr;
    private float _time;
    private float _startRadius;

    // Use this to spawn quickly from any script.
    public static void Spawn(Vector3 worldPos, float radius, Color color, float width = 0.08f, float duration = 0.45f, int segments = 64, float startFrac = 0.6f)
    {
        var go = new GameObject("ExplosionRingFX");
        go.transform.position = worldPos;
        var fx = go.AddComponent<ExplosionRingFX>();
        fx.finalRadius = Mathf.Max(0.01f, radius);
        fx.ringColor = color;
        fx.lineWidth = width;
        fx.duration = Mathf.Max(0.02f, duration);
        fx.segments = Mathf.Max(8, segments);
        fx.startRadiusFraction = Mathf.Clamp01(startFrac);
    }

    private void Awake()
    {
        _lr = gameObject.AddComponent<LineRenderer>();
        _lr.loop = true;
        _lr.useWorldSpace = true;
        _lr.numCornerVertices = 2;
        _lr.numCapVertices = 2;
        _lr.textureMode = LineTextureMode.Stretch;
#if UNITY_EDITOR
        if (_lr.sharedMaterial == null)
            _lr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
#else
        if (_lr.material == null)
            _lr.material = new Material(Shader.Find("Sprites/Default"));
#endif
        _lr.positionCount = Mathf.Max(8, segments);
        _lr.startWidth = lineWidth;
        _lr.endWidth = lineWidth;

        _startRadius = Mathf.Max(0.001f, finalRadius * Mathf.Clamp01(startRadiusFraction));

        // Initialize ring small & semi-transparent
        SetAlpha(ringColor.a);
        BuildCircle(_startRadius);
    }

    private void Update()
    {
        _time += Time.deltaTime;
        float t = Mathf.Clamp01(_time / duration);

        // Ease out expansion
        float ease = 1f - Mathf.Pow(1f - t, 3f);
        float r = Mathf.Lerp(_startRadius, finalRadius, ease);
        BuildCircle(r);

        // Fade out towards end
        float alpha = Mathf.Lerp(ringColor.a, 0f, t);
        SetAlpha(alpha);

        if (_time >= duration)
        {
            Destroy(gameObject);
        }
    }

    private void BuildCircle(float radius)
    {
        if (_lr == null) return;
        int seg = Mathf.Max(8, segments);
        if (_lr.positionCount != seg) _lr.positionCount = seg;

        float step = Mathf.PI * 2f / seg;
        Vector3 center = transform.position;

        for (int i = 0; i < seg; i++)
        {
            float a = i * step;
            Vector3 p = center + new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f);
            _lr.SetPosition(i, p);
        }
    }

    private void SetAlpha(float a)
    {
        if (_lr == null) return;
        var c = ringColor; c.a = a;
        _lr.startColor = c;
        _lr.endColor = c;
    }
}
