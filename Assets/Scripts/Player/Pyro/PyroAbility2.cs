using UnityEngine;

/// <summary>
/// Pyro's second ability — Hell Bomb.
/// Plants a bomb at Pyro's feet. The bomb explodes on enemy contact,
/// marking all nearby enemies with Hell's Justice. Marked enemies take
/// extra damage from all of Pyro's summons.
/// </summary>
public class PyroAbility2 : MonoBehaviour
{
    [Header("References")]
    public GameObject hellBombPrefab;

    [Header("Cooldown")]
    public float baseCooldown = 6f;
    private float _cooldownTimer;

    [Header("Debug")]
    public bool showDebug = true;

    private PlayerControls _controls;
    private PlayerStats _stats;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private void Awake()
    {
        _controls = GetComponent<PlayerControls>();
        _stats    = GetComponent<PlayerStats>();
    }

    private void OnEnable()
    {
        if (_controls != null)
            _controls.OnAbility2 += OnAbility2Pressed;
    }

    private void OnDisable()
    {
        if (_controls != null)
            _controls.OnAbility2 -= OnAbility2Pressed;
    }

    private void Update()
    {
        if (_cooldownTimer > 0f)
            _cooldownTimer -= Time.deltaTime;
    }

    // ── Input handler ──────────────────────────────────────────────────────

    private void OnAbility2Pressed()
    {
        if (_cooldownTimer > 0f)
        {
            if (showDebug)
                Debug.Log($"[PyroAbility2] On cooldown: {_cooldownTimer:F1}s remaining.");
            return;
        }

        PlantBomb();
    }

    // ── Core ───────────────────────────────────────────────────────────────

    private void PlantBomb()
    {
        if (hellBombPrefab == null)
        {
            Debug.LogWarning("[PyroAbility2] hellBombPrefab is not assigned.");
            return;
        }

        Instantiate(hellBombPrefab, transform.position, Quaternion.identity);

        float cd = baseCooldown * (_stats != null ? _stats.GetCooldownMultiplier() : 1f);
        _cooldownTimer = cd;

        if (showDebug)
            Debug.Log($"[PyroAbility2] Hell Bomb planted! Cooldown: {cd:F1}s");
    }

    // ── UI helpers ─────────────────────────────────────────────────────────

    /// <summary>Returns 0..1 normalized cooldown progress.</summary>
    public float GetCooldownNormalized()
    {
        return baseCooldown > 0f ? Mathf.Clamp01(_cooldownTimer / baseCooldown) : 0f;
    }

    /// <summary>Returns remaining cooldown in seconds.</summary>
    public float GetCooldownRemaining()
    {
        return Mathf.Max(0f, _cooldownTimer);
    }
}
