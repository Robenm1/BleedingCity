using UnityEngine;
using TMPro;

/// <summary>
/// Spawned by DummyHealth to display a floating damage number above the dummy.
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

    private void Awake()
    {
        _text = GetComponent<TextMeshPro>();
    }

    /// <summary>
    /// Initialises the popup with the damage value to display.
    /// </summary>
    public void Setup(float damage)
    {
        if (_text == null) _text = GetComponent<TextMeshPro>();
        _text.SetText(Mathf.RoundToInt(damage).ToString());
        _elapsed = 0f;
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;

        // Float upward
        transform.position += Vector3.up * FloatSpeed * Time.deltaTime;

        // Fade out after delay
        if (_elapsed >= FadeStartDelay)
        {
            float fadeProgress = (_elapsed - FadeStartDelay) / FadeDuration;
            float alpha = Mathf.Lerp(1f, 0f, fadeProgress);
            Color c = _text.color;
            c.a = alpha;
            _text.color = c;
        }

        if (_elapsed >= TotalLifetime)
        {
            Destroy(gameObject);
        }
    }
}
