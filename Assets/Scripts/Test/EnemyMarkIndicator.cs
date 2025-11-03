using UnityEngine;

/// <summary>
/// Draws a small circular ring above the enemy while it's marked (EnemyMark.isMarked).
/// Add this to the enemy prefab. It will auto-create a LineRenderer child and manage its visibility.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(Transform))]
public class EnemyMarkIndicator : MonoBehaviour
{
    [Header("Appearance")]
    [Tooltip("Vertical offset from enemy position (above head).")]
    public float yOffset = 0.8f;

    [Tooltip("Circle radius for the mark ring.")]
    public float radius = 0.22f;

    [Range(8, 128)] public int segments = 40;

    [Tooltip("Ring width.")]
    public float lineWidth = 0.04f;

    [Tooltip("Base color (alpha animated by fade).")]
    public Color ringColor = new Color(1f, 0.9f, 0.2f, 0.9f);

    [Header("Sorting")]
    public string sortingLayerName = "Default";
    public int sortingOrder = 100; // above sprites

    [Header("Behavior")]
    [Tooltip("How fast the indicator fades in/out.")]
    public float fadeSpeed = 10f;

    [Tooltip("Hide indicator completely when not marked.")]
    public bool disableWhenHidden = true;

    private EnemyMark _mark;
    private LineRenderer _lr;
    private Transform _ringTf;
    private float _targetAlpha = 0f;
    private float _currentAlpha = 0f;

    private static Shader sSpritesDefault;
    private static Shader SpritesDefault => sSpritesDefault ??= Shader.Find("Sprites/Default");

    private void Reset()
    {
        EnsureComponents();
        BuildCircle();
        ApplySorting();
        SetAlphaImmediate(0f);
        SafeEnable(false);
    }

    private void Awake()
    {
        _mark = GetComponent<EnemyMark>();
        EnsureComponents();
        BuildCircle();
        ApplySorting();
        SetAlphaImmediate(0f);
        SafeEnable(false);
    }

    private void OnEnable()
    {
        if (_lr == null) EnsureComponents();
        if (_lr != null) ApplySorting();
    }

    private void Update()
    {
        // Find EnemyMark at edit time too (for scene preview)
        if (_mark == null) _mark = GetComponent<EnemyMark>();

        bool marked = (_mark != null && _mark.isMarked);
        _targetAlpha = marked ? Mathf.Clamp01(ringColor.a) : 0f;

        // Smooth fade
        _currentAlpha = Mathf.MoveTowards(_currentAlpha, _targetAlpha, fadeSpeed * Time.deltaTime);
        SetAlpha(_currentAlpha);

        // Enable/disable
        if (disableWhenHidden)
        {
            SafeEnable(_currentAlpha > 0.01f);
        }
        else
        {
            SafeEnable(true);
        }

        // Follow offset
        if (_ringTf != null)
        {
            _ringTf.position = new Vector3(transform.position.x, transform.position.y + yOffset, transform.position.z);
        }
    }

    // ---------- helpers ----------

    private void EnsureComponents()
    {
        // Child holder
        Transform child = transform.Find("MarkRing");
        if (child == null)
        {
            var go = new GameObject("MarkRing");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, yOffset, 0f);
            child = go.transform;
        }
        _ringTf = child;

        // LineRenderer
        _lr = child.GetComponent<LineRenderer>();
        if (_lr == null)
        {
            _lr = child.gameObject.AddComponent<LineRenderer>();
            _lr.loop = true;
            _lr.useWorldSpace = true; // world-space ring above enemy
            _lr.textureMode = LineTextureMode.Stretch;
        }

        // Material (avoid leaks in edit mode)
#if UNITY_EDITOR
        if (_lr.sharedMaterial == null) _lr.sharedMaterial = new Material(SpritesDefault);
#else
        if (_lr.material == null) _lr.material = new Material(SpritesDefault);
#endif
        _lr.startWidth = lineWidth;
        _lr.endWidth = lineWidth;
        _lr.numCornerVertices = 2;
        _lr.numCapVertices = 2;
        _lr.positionCount = Mathf.Max(8, segments);
    }

    private void BuildCircle()
    {
        if (_lr == null) return;
        int seg = Mathf.Max(8, segments);
        _lr.positionCount = seg;

        // Build around _ringTf position in world space
        Vector3 center = _ringTf != null ? _ringTf.position : (transform.position + new Vector3(0f, yOffset, 0f));
        float step = Mathf.PI * 2f / seg;

        for (int i = 0; i < seg; i++)
        {
            float a = i * step;
            Vector3 p = center + new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f);
            _lr.SetPosition(i, p);
        }
    }

    private void LateUpdate()
    {
        // Rebuild circle each frame in case enemy moves (cheap and reliable).
        BuildCircle();
    }

    private void SetAlpha(float a)
    {
        if (_lr == null) return;
        Color c = ringColor;
        c.a = a;
        _lr.startColor = c;
        _lr.endColor = c;
    }

    private void SetAlphaImmediate(float a)
    {
        _currentAlpha = a;
        SetAlpha(a);
    }

    private void SafeEnable(bool enabled)
    {
        if (_lr != null && _lr.enabled != enabled)
            _lr.enabled = enabled;
    }

    private void ApplySorting()
    {
        if (_lr == null) return;
        _lr.sortingLayerName = sortingLayerName;
        _lr.sortingOrder = sortingOrder;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Reflect inspector changes in editor
        EnsureComponents();
        ApplySorting();
        BuildCircle();
        SetAlphaImmediate(_currentAlpha);
    }
#endif

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.25f);
        Vector3 center = transform.position + new Vector3(0f, yOffset, 0f);
        Gizmos.DrawWireSphere(center, radius);
    }
}
