using UnityEngine;

[DisallowMultipleComponent]
public class FrostedOnEnemy : MonoBehaviour
{
    [Header("State (runtime)")]
    [Range(0.05f, 1f)] public float slow = 0.6f;
    public float durationRemaining = 0f;
    public float vulnerabilityMultiplier = 1.15f;

    [Header("Icon (runtime)")]
    public Sprite frostIcon;
    public Vector2 iconPivot = new Vector2(0f, 0.9f);
    public Vector2 iconSize = new Vector2(0.35f, 0.35f);

    private Transform _tf;
    private Transform _iconHolder;
    private SpriteRenderer _iconSR;

    public bool IsActive => durationRemaining > 0f;

    private void Awake()
    {
        _tf = transform;
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
            else
            {
                UpdateIconTransform();
            }
        }
    }

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
            UpdateIconTransform();
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

    private void UpdateIconTransform()
    {
        if (_iconHolder == null || _iconSR == null) return;
        _iconHolder.position = new Vector3(_tf.position.x + iconPivot.x, _tf.position.y + iconPivot.y, _tf.position.z);
        _iconHolder.localScale = new Vector3(iconSize.x, iconSize.y, 1f);
    }

    private void HideIconImmediate()
    {
        if (_iconSR != null) _iconSR.enabled = false;
        if (_iconHolder != null) _iconHolder.gameObject.SetActive(false);
    }
}