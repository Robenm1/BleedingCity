using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Stationary slow area: applies EnemySlowModifier to any enemy inside its radius
/// for the lifetime of the zone. Spawns optional visuals (fill + outline).
/// Destroy this object to end the zone.
/// </summary>
public class SlowZone : MonoBehaviour
{
    [Header("Zone")]
    public float radius = 4.5f;
    [Range(0.05f, 1f)] public float slowFactor = 0.4f; // 0.4 = 60% slow
    public float duration = 4f;
    public LayerMask enemyLayers;

    [Header("Visuals")]
    public bool showCircle = true;
    [Tooltip("Semi-transparent fill sprite (white circle recommended). Optional.")]
    public SpriteRenderer fillSprite;
    [Tooltip("Optional outline, auto-created if null.")]
    public LineRenderer outline;
    [Range(16, 256)] public int outlineSegments = 96;
    public float outlineWidth = 0.05f;
    public Color fillColor = new Color(1f, 0.9f, 0.3f, 0.15f);
    public Color outlineColor = new Color(1f, 0.8f, 0.25f, 0.35f);
    public string sortingLayerName = "Default";
    public int sortingOrder = 50;

    // internal
    private float _endTime;

    private static Shader sSpritesDefault;
    private static Shader SpritesDefault
    {
        get { return sSpritesDefault ??= Shader.Find("Sprites/Default"); }
    }

    public void Init(float radius, float slowFactor, float duration, LayerMask layers, bool show)
    {
        this.radius = radius;
        this.slowFactor = Mathf.Clamp(slowFactor, 0.05f, 1f);
        this.duration = Mathf.Max(0.05f, duration);
        this.enemyLayers = layers;
        this.showCircle = show;
    }

    private void Awake()
    {
        _endTime = Time.time + duration;
        SetupVisuals();
    }

    private void Update()
    {
        // Keep applying slow to anything currently in the zone
        ApplySlowTick(0.2f); // refresh window so enemies staying inside remain slowed

        if (Time.time >= _endTime)
            Destroy(gameObject);
    }

    private void ApplySlowTick(float tickDuration)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, enemyLayers);
        for (int i = 0; i < hits.Length; i++)
        {
            var col = hits[i];
            if (!col) continue;

            var slow = col.GetComponent<EnemySlowModifier>();
            if (!slow) slow = col.gameObject.AddComponent<EnemySlowModifier>();

            // Re-apply with a small duration so it stays while inside the zone
            // and falls off shortly after leaving.
            slow.Apply(slowFactor, tickDuration);
        }
    }

    // ---------- Visuals ----------

    private void SetupVisuals()
    {
        if (!showCircle)
        {
            if (fillSprite) fillSprite.enabled = false;
            if (outline) outline.enabled = false;
            return;
        }

        if (fillSprite)
        {
            fillSprite.enabled = true;
            fillSprite.color = fillColor;
            // scale so diameter = 2 * radius
            float d = radius * 2f;
            fillSprite.transform.localScale = new Vector3(d, d, 1f);
            fillSprite.transform.localPosition = Vector3.zero;
            ApplySorting(fillSprite);
        }

        if (!outline)
        {
            var go = new GameObject("SlowZoneOutline");
            go.transform.SetParent(transform, false);
            outline = go.AddComponent<LineRenderer>();
            outline.loop = true;
            outline.useWorldSpace = false;
            outline.widthMultiplier = 1f;
            outline.textureMode = LineTextureMode.Stretch;
#if UNITY_EDITOR
            outline.sharedMaterial = new Material(SpritesDefault);
#else
            outline.material = new Material(SpritesDefault);
#endif
        }

        outline.enabled = true;
        outline.startWidth = outlineWidth;
        outline.endWidth = outlineWidth;
        outline.startColor = outlineColor;
        outline.endColor = outlineColor;
        outline.positionCount = Mathf.Max(16, outlineSegments);
        BuildCircle(outline, radius);
        ApplySorting(outline);
    }

    private void BuildCircle(LineRenderer lr, float r)
    {
        int seg = Mathf.Max(16, outlineSegments);
        lr.positionCount = seg;
        float step = Mathf.PI * 2f / seg;
        for (int i = 0; i < seg; i++)
        {
            float a = i * step;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f));
        }
    }

    private void ApplySorting(Renderer r)
    {
        if (!r) return;
        r.sortingLayerName = sortingLayerName;
        r.sortingOrder = sortingOrder;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.85f, 0.75f, 0.2f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
