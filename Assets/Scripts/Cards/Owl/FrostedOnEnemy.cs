using UnityEngine;

[DisallowMultipleComponent]
public class FrostedOnEnemy : MonoBehaviour, IMarkDisplay
{
    [Header("State (runtime)")]
    [Range(0.05f, 1f)] public float slow = 0.6f;
    public float durationRemaining = 0f;
    public float vulnerabilityMultiplier = 1.15f;

    [Header("Icon (runtime)")]
    public Sprite frostIcon;

    // These are kept for backward compatibility but position/size are now
    // managed by MarkDisplayController.
    public Vector2 iconPivot = new Vector2(0f, 0.9f);
    public Vector2 iconSize = new Vector2(0.35f, 0.35f);

    private Transform _tf;
    private Transform _iconHolder;
    private SpriteRenderer _iconSR;

    public bool IsActive => durationRemaining > 0f;

    // ---- IMarkDisplay ----
    /// <inheritdoc/>
    public bool IsMarkVisible => durationRemaining > 0f && _iconSR != null && _iconSR.enabled;

    /// <inheritdoc/>
    public SpriteRenderer MarkSpriteRenderer => _iconSR;

    private void Awake()
    {
        _tf = transform;
        // Ensure the centralised layout controller is present on this enemy.
        MarkDisplayController.EnsureOn(gameObject);
        EnsureIconObjects();
        HideIconImmediate();
    }

    private void Update()
    {
        if (durationRemaining > 0f)
        {
            durationRemaining -= Time.deltaTime;
            if (durationRemaining <= 0f)
            {
                durationRemaining = 0f;
                HideIconImmediate();
            }
        }
    }

    /// <summary>
    /// Applies the frost effect: slows the enemy, enables the vulnerability window,
    /// and shows the frost icon.
    /// </summary>
    public void Apply(float slow, float dur, Sprite icon, Vector2 pivot, Vector2 size)
    {
        this.slow = Mathf.Clamp(slow, 0.05f, 1f);
        durationRemaining = Mathf.Max(dur, 0f);
        frostIcon = icon;
        iconPivot = pivot;
        iconSize = new Vector2(Mathf.Max(0.01f, size.x), Mathf.Max(0.01f, size.y));

        if (durationRemaining > 0f && frostIcon != null)
        {
            EnsureIconObjects();
            _iconSR.sprite = frostIcon;
            _iconSR.enabled = true;
            _iconHolder.gameObject.SetActive(true);
            // Position/scale are handled by MarkDisplayController each LateUpdate.
        }
        else
        {
            HideIconImmediate();
        }
    }

    private void EnsureIconObjects()
    {
        if (_iconHolder == null)
        {
            var go = new GameObject("FrostIcon");
            go.transform.SetParent(_tf, false);
            _iconHolder = go.transform;
            _iconSR = go.AddComponent<SpriteRenderer>();
            _iconSR.sortingOrder = 500;
            _iconSR.enabled = false;
        }
        else if (_iconSR == null)
        {
            _iconSR = _iconHolder.GetComponent<SpriteRenderer>();
            if (_iconSR == null) _iconSR = _iconHolder.gameObject.AddComponent<SpriteRenderer>();
            _iconSR.sortingOrder = 500;
            _iconSR.enabled = false;
        }
    }

    private void HideIconImmediate()
    {
        if (_iconSR != null) _iconSR.enabled = false;
        if (_iconHolder != null) _iconHolder.gameObject.SetActive(false);
    }
}
