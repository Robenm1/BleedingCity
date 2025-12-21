using UnityEngine;

/// <summary>
/// Simple frost debuff:
/// - Optionally slows enemies that use EnemyFollow by scaling its moveSpeed.
/// - Shows an optional sprite "mark" above the enemy while active.
/// - Auto-expires after duration and restores speed / removes mark.
/// </summary>
[DisallowMultipleComponent]
public class FrostedOnEnemy : MonoBehaviour
{
    [Header("Runtime (read-only)")]
    public bool isActive;
    public float remaining;

    // Current applied values
    private float _slowFactor = 1f;   // 0.6 => 40% slow
    private float _duration = 0f;

    // Visual mark
    private Sprite _markSprite;
    private Vector2 _markOffset = new Vector2(0f, 1f);
    private Vector2 _markSize = new Vector2(0.5f, 0.5f);
    private GameObject _markGO;
    private SpriteRenderer _markSR;

    // Movement hook (best effort)
    private EnemyFollow _enemyFollow;
    private float _originalMoveSpeed;
    private bool _speedOverridden;

    // When we last sized the mark (guard against 0 bounds)
    private bool _markSizedOnce;

    /// <summary>
    /// Apply/refresh the frost. If already active, duration is refreshed and the stronger slow wins
    /// (i.e., smaller slowFactor value replaces a larger one).
    /// </summary>
    public void Apply(float slowFactor, float duration, Sprite markSprite, Vector2 offset, Vector2 size)
    {
        slowFactor = Mathf.Clamp(slowFactor, 0.1f, 1f);
        duration = Mathf.Max(0.01f, duration);

        // Cache visuals
        _markSprite = markSprite;
        _markOffset = offset;
        _markSize = new Vector2(Mathf.Max(0.001f, size.x), Mathf.Max(0.001f, size.y));

        if (!isActive)
        {
            // First-time enable
            _slowFactor = slowFactor;
            _duration = duration;
            remaining = duration;
            isActive = true;

            TryHookMovement();
            ApplySpeed();

            EnsureMark();
            UpdateMarkTransform(forceRescale: true);
        }
        else
        {
            // Refresh logic: keep the stronger slow (smaller factor) and extend time
            if (slowFactor < _slowFactor)
            {
                // Update slow strength
                RestoreSpeed();         // restore old speed first
                _slowFactor = slowFactor;
                ApplySpeed();           // then re-apply with new factor
            }

            _duration = Mathf.Max(_duration, duration);
            remaining = Mathf.Max(remaining, duration);

            // Update mark visuals if sprite/size changed
            EnsureMark();
            UpdateMarkTransform(forceRescale: !_markSizedOnce);
        }
    }

    private void Update()
    {
        if (!isActive) return;

        remaining -= Time.deltaTime;
        if (remaining <= 0f)
        {
            ClearAndDestroy();
            return;
        }

        // Keep the mark following the enemy
        if (_markGO)
        {
            _markGO.transform.position = (Vector2)transform.position + _markOffset;
        }
    }

    private void OnDisable()
    {
        // Safety: restore speed and clean mark when component is disabled externally
        if (isActive)
        {
            RestoreSpeed();
            DestroyMark();
            isActive = false;
        }
    }

    private void OnDestroy()
    {
        // Safety on object destruction
        RestoreSpeed();
        DestroyMark();
    }

    // ===== Movement slow (EnemyFollow-based) =====

    private void TryHookMovement()
    {
        if (_enemyFollow == null)
            _enemyFollow = GetComponent<EnemyFollow>();

        if (_enemyFollow != null && !_speedOverridden)
        {
            _originalMoveSpeed = _enemyFollow.moveSpeed;
            _speedOverridden = true;
        }
    }

    private void ApplySpeed()
    {
        if (_enemyFollow != null && _speedOverridden)
        {
            _enemyFollow.moveSpeed = _originalMoveSpeed * _slowFactor;
        }
    }

    private void RestoreSpeed()
    {
        if (_enemyFollow != null && _speedOverridden)
        {
            _enemyFollow.moveSpeed = _originalMoveSpeed;
        }
        _speedOverridden = false;
    }

    // ===== Mark sprite =====

    private void EnsureMark()
    {
        if (_markSprite == null)
        {
            DestroyMark();
            return;
        }

        if (_markGO == null)
        {
            _markGO = new GameObject("FrostMark");
            _markGO.transform.SetParent(null); // world-space follower
            _markGO.transform.position = (Vector2)transform.position + _markOffset;

            _markSR = _markGO.AddComponent<SpriteRenderer>();
            _markSR.sortingOrder = 999; // draw above most sprites
        }

        if (_markSR != null)
        {
            _markSR.sprite = _markSprite;
            _markSR.enabled = true;
            _markSizedOnce = false; // force a resize next update
            UpdateMarkTransform(forceRescale: true);
        }
    }

    private void UpdateMarkTransform(bool forceRescale)
    {
        if (_markGO == null || _markSR == null) return;

        _markGO.transform.position = (Vector2)transform.position + _markOffset;

        // Scale to requested size (approximate, based on bounds)
        if (forceRescale || !_markSizedOnce)
        {
            var sr = _markSR;
            if (sr.sprite != null)
            {
                var b = sr.bounds;
                // bounds can be zero right after assignment; guard against divide-by-zero
                Vector2 curSize = new Vector2(Mathf.Max(1e-4f, b.size.x), Mathf.Max(1e-4f, b.size.y));
                Vector3 scale = _markGO.transform.localScale;

                // Convert desired world size to scale multiplier
                scale.x = _markSize.x / curSize.x * scale.x;
                scale.y = _markSize.y / curSize.y * scale.y;
                scale.z = 1f;

                _markGO.transform.localScale = scale;
                _markSizedOnce = true;
            }
        }
    }

    private void DestroyMark()
    {
        if (_markGO != null)
        {
            Destroy(_markGO);
            _markGO = null;
            _markSR = null;
        }
        _markSizedOnce = false;
    }

    // ===== Cleanup =====

    private void ClearAndDestroy()
    {
        RestoreSpeed();
        DestroyMark();
        isActive = false;
        Destroy(this);
    }
}
