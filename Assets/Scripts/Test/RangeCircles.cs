using UnityEngine;
using System.Collections;

/// <summary>
/// Shows two circles around the owner:
/// 1) Repulse pulse (Ability1): an expanding semi-transparent ring that fades out.
/// 2) Dune area (Ability2): a semi-transparent filled disc that stays while active.
/// </summary>
[ExecuteAlways]
public class RangeCircles : MonoBehaviour
{
    [Header("Owner/Follow")]
    public Transform followTarget;            // usually Dezzo; default = this.transform
    public bool followEveryFrame = true;

    [Header("Repulse Pulse (Ability1)")]
    public LineRenderer repulseRing;          // outline only (semi-transparent)
    [Range(8, 256)] public int ringSegments = 96;
    public float pulseDuration = 0.25f;       // how long the ring expands/fades
    public float ringStartWidth = 0.05f;
    public float ringEndWidth = 0.05f;
    public Color ringColor = new Color(1f, 0.85f, 0.25f, 0.4f);

    [Header("Dune Area (Ability2)")]
    [Tooltip("Filled disc. Assign a SpriteRenderer with a white circle sprite (pixels per unit = diameter 1).")]
    public SpriteRenderer duneFill;           // semi-transparent fill
    public Color duneFillColor = new Color(1f, 0.9f, 0.3f, 0.15f);
    public bool showDuneOutline = true;
    public LineRenderer duneOutline;          // optional outline circle for Dune area
    public float duneOutlineWidth = 0.05f;
    public Color duneOutlineColor = new Color(1f, 0.8f, 0.25f, 0.35f);

    // internals
    private Coroutine pulseCo;
    private Coroutine duneCo;

    private static Shader sSpritesDefault;

    private void Reset()
    {
        followTarget = transform;

        if (repulseRing == null)
        {
            repulseRing = gameObject.AddComponent<LineRenderer>();
            repulseRing.loop = true;
            repulseRing.useWorldSpace = false;
            repulseRing.widthMultiplier = 1f;
            repulseRing.textureMode = LineTextureMode.Stretch;
            EnsureLineMaterial(repulseRing);
            repulseRing.enabled = false;
        }

        if (duneOutline == null)
        {
            var go = new GameObject("DuneOutline");
            go.transform.SetParent(transform, false);
            duneOutline = go.AddComponent<LineRenderer>();
            duneOutline.loop = true;
            duneOutline.useWorldSpace = false;
            duneOutline.widthMultiplier = 1f;
            duneOutline.textureMode = LineTextureMode.Stretch;
            EnsureLineMaterial(duneOutline);
            duneOutline.enabled = false;
        }
    }

    private void Awake()
    {
        if (followTarget == null) followTarget = transform;

        EnsureLineMaterial(repulseRing);
        EnsureLineMaterial(duneOutline);

        SetupLineRenderer(repulseRing, ringStartWidth, ringColor);
        SetupLineRenderer(duneOutline, duneOutlineWidth, duneOutlineColor);

        HideRepulseImmediate();
        HideDuneImmediate();
    }

    private void Update()
    {
        if (followEveryFrame && followTarget != null)
            transform.position = followTarget.position;
    }

    // -------- Public API (call these from abilities) --------

    /// <summary>Show a quick expanding ring that reaches 'radius' in pulseDuration.</summary>
    public void ShowRepulsePulse(float radius)
    {
        if (repulseRing == null) return;
        if (pulseCo != null) StopCoroutine(pulseCo);
        pulseCo = StartCoroutine(PulseRoutine(radius));
    }

    /// <summary>Show Dune fill + (optional) outline for 'duration' seconds.</summary>
    public void ShowDuneFor(float radius, float duration)
    {
        if (duneCo != null) StopCoroutine(duneCo);
        duneCo = StartCoroutine(DuneRoutine(radius, duration));
    }

    /// <summary>Enable/disable Dune area manually (e.g., if you handle timing elsewhere).</summary>
    public void SetDuneVisible(bool visible, float radius = 0f)
    {
        if (visible) ShowDune(radius);
        else HideDuneImmediate();
    }

    // -------- Internals --------

    private static Shader GetSpritesDefault()
    {
        if (sSpritesDefault == null)
            sSpritesDefault = Shader.Find("Sprites/Default");
        return sSpritesDefault;
    }

    /// <summary>
    /// Ensure a material is assigned without leaking:
    /// - In edit mode: use sharedMaterial (no instancing).
    /// - In play mode: use material (runtime instance is fine).
    /// </summary>
    private void EnsureLineMaterial(LineRenderer lr)
    {
        if (lr == null) return;
        var shader = GetSpritesDefault();
        if (!Application.isPlaying)
        {
            if (lr.sharedMaterial == null)
                lr.sharedMaterial = new Material(shader);
        }
        else
        {
            if (lr.material == null)
                lr.material = new Material(shader);
        }
    }

    private void SetupLineRenderer(LineRenderer lr, float width, Color col)
    {
        if (lr == null) return;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.positionCount = Mathf.Max(8, ringSegments);
        // Don’t set material color; use line renderer color to avoid touching material instance.
        SetLineColor(lr, col);
        lr.enabled = false;
    }

    private void SetLineColor(LineRenderer lr, Color c)
    {
        if (lr == null) return;
        lr.startColor = c;
        lr.endColor = c;
    }

    private void BuildCircle(LineRenderer lr, float radius)
    {
        if (lr == null) return;
        int segments = Mathf.Max(8, ringSegments);
        lr.positionCount = segments;
        float step = Mathf.PI * 2f / segments;

        for (int i = 0; i < segments; i++)
        {
            float a = step * i;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f));
        }
    }

    private IEnumerator PulseRoutine(float targetRadius)
    {
        repulseRing.enabled = true;

        float t = 0f;
        Color baseCol = ringColor;

        while (t < pulseDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / pulseDuration);
            float r = Mathf.Lerp(0.05f, targetRadius, k);

            // Slight fade-out via LineRenderer colors (no material access)
            float alpha = Mathf.Lerp(baseCol.a, 0f, k);
            SetLineColor(repulseRing, new Color(baseCol.r, baseCol.g, baseCol.b, alpha));

            BuildCircle(repulseRing, r);
            yield return null;
        }

        HideRepulseImmediate();
        pulseCo = null;
    }

    private IEnumerator DuneRoutine(float radius, float duration)
    {
        ShowDune(radius);
        float t = duration;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            yield return null;
        }
        HideDuneImmediate();
        duneCo = null;
    }

    private void ShowDune(float radius)
    {
        // Filled disc
        if (duneFill != null)
        {
            duneFill.enabled = true;
            duneFill.color = duneFillColor;

            // Scale so diameter = 2*radius
            float d = Mathf.Max(0f, radius * 2f);
            duneFill.transform.localScale = new Vector3(d, d, 1f);
            duneFill.transform.localPosition = Vector3.zero;
        }

        // Optional outline
        if (showDuneOutline && duneOutline != null)
        {
            duneOutline.enabled = true;
            SetLineColor(duneOutline, duneOutlineColor);
            BuildCircle(duneOutline, radius);
        }
        else if (duneOutline != null)
        {
            duneOutline.enabled = false;
        }
    }

    private void HideRepulseImmediate()
    {
        if (repulseRing != null) repulseRing.enabled = false;
    }

    private void HideDuneImmediate()
    {
        if (duneFill != null) duneFill.enabled = false;
        if (duneOutline != null) duneOutline.enabled = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying && followTarget != null)
            transform.position = followTarget.position;
    }
}
