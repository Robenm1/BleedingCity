using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(TrailRenderer))]
public class RetroSmoothBulletTrail : MonoBehaviour
{
    [Header("Trail Shape")]
    [Tooltip("How long the trail stays behind the bullet.")]
    public float trailTime = 0.18f;

    [Tooltip("Main width of the trail near the bullet.")]
    public float trailWidth = 0.25f;

    [Tooltip("How smooth the trail is. Lower = smoother.")]
    public float minVertexDistance = 0.02f;

    [Tooltip("If the pointy side appears near the bullet instead of the tail, toggle this.")]
    public bool invertWidthCurve = false;

    [Header("Colors")]
    [Tooltip("Choose as many colors as you want. They blend along the trail.")]
    public Color[] trailColors =
    {
        new Color(0.2f, 0.9f, 1f, 1f),
        new Color(0.2f, 0.4f, 1f, 0.9f),
        new Color(0.8f, 0.2f, 1f, 0.6f)
    };

    [Header("Fade")]
    public float startAlpha = 1f;
    public float endAlpha = 0f;

    private TrailRenderer _trail;

    private void Awake()
    {
        _trail = GetComponent<TrailRenderer>();
        ApplyTrail();
    }

    private void OnEnable()
    {
        if (!_trail)
            _trail = GetComponent<TrailRenderer>();

        ApplyTrail();

        _trail.Clear();
        _trail.emitting = true;
    }

    private void ApplyTrail()
    {
        if (!_trail) return;

        _trail.time = Mathf.Max(0.01f, trailTime);
        _trail.minVertexDistance = Mathf.Max(0.001f, minVertexDistance);

        _trail.numCornerVertices = 4;
        _trail.numCapVertices = 4;
        _trail.alignment = LineAlignment.View;
        _trail.textureMode = LineTextureMode.Stretch;
        _trail.autodestruct = false;
        _trail.emitting = true;

        ApplyWidthCurve();
        ApplyColorGradient();
    }

    private void ApplyWidthCurve()
    {
        AnimationCurve curve = new AnimationCurve();

        if (!invertWidthCurve)
        {
            // Cartoon trail:
            // Thick body, then sharp point at the end.
            curve.AddKey(0f, trailWidth);
            curve.AddKey(0.35f, trailWidth);
            curve.AddKey(0.75f, trailWidth * 0.75f);
            curve.AddKey(1f, 0f);
        }
        else
        {
            curve.AddKey(0f, 0f);
            curve.AddKey(0.25f, trailWidth * 0.75f);
            curve.AddKey(0.65f, trailWidth);
            curve.AddKey(1f, trailWidth);
        }

        _trail.widthCurve = curve;
        _trail.widthMultiplier = 1f;
    }

    private void ApplyColorGradient()
    {
        Gradient gradient = new Gradient();

        if (trailColors == null || trailColors.Length == 0)
        {
            trailColors = new Color[]
            {
                Color.cyan,
                Color.blue,
                Color.magenta
            };
        }

        GradientColorKey[] colorKeys = new GradientColorKey[trailColors.Length];
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[trailColors.Length];

        for (int i = 0; i < trailColors.Length; i++)
        {
            float time = trailColors.Length == 1 ? 0f : (float)i / (trailColors.Length - 1);

            Color c = trailColors[i];

            colorKeys[i] = new GradientColorKey(c, time);

            float alpha = Mathf.Lerp(startAlpha, endAlpha, time);
            alphaKeys[i] = new GradientAlphaKey(alpha * c.a, time);
        }

        gradient.SetKeys(colorKeys, alphaKeys);
        _trail.colorGradient = gradient;
    }
}