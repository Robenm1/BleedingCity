using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class WispFadeIn : MonoBehaviour
{
    [Header("Fade")]
    [Tooltip("How long the wisp takes to fade in.")]
    public float fadeDuration = 0.35f;

    private SpriteRenderer _sr;
    private float _timer;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        _timer = 0f;
    }

    private void Update()
    {
        if (!_sr)
        {
            Destroy(this);
            return;
        }

        _timer += Time.deltaTime;

        float t = Mathf.Clamp01(_timer / Mathf.Max(0.01f, fadeDuration));

        Color color = _sr.color;
        color.a = t;
        _sr.color = color;

        if (t >= 1f)
            Destroy(this);
    }
}