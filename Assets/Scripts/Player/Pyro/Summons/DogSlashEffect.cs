using UnityEngine;

public class SlashEffect : MonoBehaviour
{
    [Tooltip("How long before the effect destroys itself (seconds)")]
    public float lifetime = 0.5f;

    [Tooltip("Optional: Scale animation curve over lifetime")]
    public AnimationCurve scaleCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

    [Tooltip("Optional: Fade out sprite over lifetime")]
    public bool fadeOut = true;

    private SpriteRenderer spriteRenderer;
    private float timer;
    private Vector3 initialScale;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        initialScale = transform.localScale;
        timer = 0f;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        float normalizedTime = timer / lifetime;

        if (scaleCurve != null && scaleCurve.length > 0)
        {
            float scaleMultiplier = scaleCurve.Evaluate(normalizedTime);
            transform.localScale = initialScale * scaleMultiplier;
        }

        if (fadeOut && spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = 1f - normalizedTime;
            spriteRenderer.color = color;
        }

        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}
