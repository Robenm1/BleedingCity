using UnityEngine;

[DisallowMultipleComponent]
public class DropBouncePickup : MonoBehaviour
{
    [Header("Jump")]
    [Tooltip("How high the pickup jumps when it spawns.")]
    public float jumpHeight = 0.7f;

    [Tooltip("How far the pickup moves sideways.")]
    public float jumpDistance = 0.6f;

    [Tooltip("How long the jump animation lasts.")]
    public float jumpDuration = 0.35f;

    [Tooltip("Randomizes the jump direction.")]
    public bool randomDirection = true;

    [Header("Landing")]
    [Tooltip("Small bounce after landing.")]
    public float landingBounceHeight = 0.12f;

    [Tooltip("How long the landing bounce lasts.")]
    public float landingBounceDuration = 0.15f;

    [Header("Collect")]
    [Tooltip("If true, collider is disabled while the pickup is jumping.")]
    public bool disableColliderDuringJump = true;

    private Vector3 _startPos;
    private Vector3 _endPos;
    private Vector3 _jumpDir;

    private float _timer;
    private bool _jumping = true;
    private bool _landingBounce;

    private Collider2D _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
    }

    private void OnEnable()
    {
        _startPos = transform.position;

        if (randomDirection)
        {
            Vector2 dir = Random.insideUnitCircle.normalized;
            if (dir.sqrMagnitude <= 0.01f)
                dir = Vector2.right;

            _jumpDir = new Vector3(dir.x, dir.y, 0f);
        }
        else
        {
            _jumpDir = Vector3.up;
        }

        _endPos = _startPos + _jumpDir * Mathf.Max(0f, jumpDistance);

        _timer = 0f;
        _jumping = true;
        _landingBounce = false;

        if (_collider != null && disableColliderDuringJump)
            _collider.enabled = false;
    }

    private void Update()
    {
        if (_jumping)
        {
            UpdateJump();
            return;
        }

        if (_landingBounce)
        {
            UpdateLandingBounce();
        }
    }

    private void UpdateJump()
    {
        _timer += Time.deltaTime;

        float duration = Mathf.Max(0.01f, jumpDuration);
        float t = Mathf.Clamp01(_timer / duration);

        Vector3 flatPos = Vector3.Lerp(_startPos, _endPos, t);

        // Parabola: 0 at start, 1 at middle, 0 at end.
        float height = 4f * jumpHeight * t * (1f - t);

        transform.position = flatPos + Vector3.up * height;

        if (t >= 1f)
        {
            _jumping = false;
            _landingBounce = true;
            _timer = 0f;

            transform.position = _endPos;
        }
    }

    private void UpdateLandingBounce()
    {
        _timer += Time.deltaTime;

        float duration = Mathf.Max(0.01f, landingBounceDuration);
        float t = Mathf.Clamp01(_timer / duration);

        float height = Mathf.Sin(t * Mathf.PI) * landingBounceHeight;

        transform.position = _endPos + Vector3.up * height;

        if (t >= 1f)
        {
            _landingBounce = false;
            transform.position = _endPos;

            if (_collider != null && disableColliderDuringJump)
                _collider.enabled = true;
        }
    }
}