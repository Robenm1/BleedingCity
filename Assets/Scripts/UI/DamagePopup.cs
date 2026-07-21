using UnityEngine;
using TMPro;

/// <summary>
/// Displays a floating damage number.
/// Floats upward, fades out, then destroys itself.
/// </summary>
public class DamagePopup : MonoBehaviour
{
    private const float FloatSpeed = 1.5f;
    private const float FadeStartDelay = 0.4f;
    private const float FadeDuration = 0.4f;
    private const float TotalLifetime = FadeStartDelay + FadeDuration;

    private TextMeshPro _text;
    private float _elapsed;
    private Color _baseColor = Color.white;

    private void Awake()
    {
        _text = GetComponent<TextMeshPro>();
    }

    /// <summary>
    /// Old setup. Keeps compatibility with old scripts.
    /// </summary>
    public void Setup(float damage)
    {
        Setup(damage, Color.white);
    }

    /// <summary>
    /// New setup with element color.
    /// </summary>
    public void Setup(float damage, Color damageColor)
    {
        if (_text == null)
            _text = GetComponent<TextMeshPro>();

        if (_text == null)
            return;

        _baseColor = damageColor;
        _baseColor.a = 1f;

        _text.color = _baseColor;
        _text.SetText(Mathf.RoundToInt(damage).ToString());

        _elapsed = 0f;
    }

    private void Update()
    {
        if (_text == null)
            return;

        _elapsed += Time.deltaTime;

        transform.position += Vector3.up * FloatSpeed * Time.deltaTime;

        if (_elapsed >= FadeStartDelay)
        {
            float fadeProgress = (_elapsed - FadeStartDelay) / FadeDuration;
            float alpha = Mathf.Lerp(1f, 0f, fadeProgress);

            Color c = _baseColor;
            c.a = alpha;

            _text.color = c;
        }

        if (_elapsed >= TotalLifetime)
        {
            Destroy(gameObject);
        }
    }
}