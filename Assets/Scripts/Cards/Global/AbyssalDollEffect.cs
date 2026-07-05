using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerStats))]
public class AbyssalDollEffect : MonoBehaviour, IActiveCardEffect
{
    [Header("Abyssal Doll")]
    [Tooltip("The Abyssal Doll prefab dropped by the player.")]
    public GameObject dollPrefab;

    [Tooltip("How long the doll stays alive.")]
    public float duration = 5f;

    [Tooltip("Maximum waves before the doll disappears.")]
    public int maxWaves = 5;

    [Header("Cooldown")]
    [Tooltip("Cooldown before Abyssal Doll can be used again.")]
    public float cooldown = 8f;

    [Header("Wave")]
    [Tooltip("Radius of each wave.")]
    public float waveRadius = 4f;

    [Tooltip("Damage scaling from PlayerStats.GetDamage(). 1 = 100% attack.")]
    public float damageScaling = 1f;

    [Tooltip("Enemy layers damaged by the wave.")]
    public LayerMask enemyLayers;

    [Header("Slow")]
    [Tooltip("How long enemies are slowed by the wave.")]
    public float slowDuration = 2f;

    [Tooltip("Enemy speed multiplier while slowed. 0.5 = 50% speed.")]
    public float slowMultiplier = 0.5f;

    [Header("Debug")]
    public bool showDebug = true;

    private PlayerStats _stats;
    private float _cooldownTimer;

    private void Awake()
    {
        _stats = GetComponent<PlayerStats>();
    }

    private void OnEnable()
    {
        if (!_stats)
            _stats = GetComponent<PlayerStats>();
    }

    private void Update()
    {
        if (_cooldownTimer <= 0f) return;

        _cooldownTimer -= Time.deltaTime;

        if (_cooldownTimer < 0f)
            _cooldownTimer = 0f;
    }

    public void Activate()
    {
        if (_cooldownTimer > 0f)
        {
            if (showDebug)
                Debug.Log($"[AbyssalDollEffect] On cooldown: {_cooldownTimer:F1}s remaining.");

            return;
        }

        if (!_stats)
            _stats = GetComponent<PlayerStats>();

        if (dollPrefab == null)
        {
            Debug.LogWarning("[AbyssalDollEffect] Doll prefab is not assigned.");
            return;
        }

        SpawnDoll();
        StartCooldown();
    }

    private void SpawnDoll()
    {
        GameObject obj = Instantiate(dollPrefab, transform.position, Quaternion.identity);

        var doll = obj.GetComponent<AbyssalDollObject>();
        if (!doll)
        {
            Debug.LogWarning("[AbyssalDollEffect] Doll prefab does not have AbyssalDollObject on the root.");
            Destroy(obj);
            return;
        }

        doll.playerStats = _stats;

        doll.duration = Mathf.Max(0.1f, duration);
        doll.maxWaves = Mathf.Max(1, maxWaves);

        doll.waveRadius = Mathf.Max(0.1f, waveRadius);
        doll.damageScaling = Mathf.Max(0f, damageScaling);
        doll.enemyLayers = enemyLayers;

        doll.slowDuration = Mathf.Max(0f, slowDuration);
        doll.slowMultiplier = Mathf.Clamp(slowMultiplier, 0.01f, 1f);

        doll.showDebug = showDebug;

        if (showDebug)
            Debug.Log("[AbyssalDollEffect] Abyssal Doll dropped.");
    }

    private void StartCooldown()
    {
        float multiplier = _stats != null ? _stats.GetCooldownMultiplier() : 1f;
        _cooldownTimer = Mathf.Max(0f, cooldown * multiplier);

        if (showDebug)
            Debug.Log($"[AbyssalDollEffect] Cooldown started: {_cooldownTimer:F1}s.");
    }

    public bool IsOnCooldown()
    {
        return _cooldownTimer > 0f;
    }

    public float GetCooldownRemaining()
    {
        return Mathf.Max(0f, _cooldownTimer);
    }

    public float GetCooldownNormalized()
    {
        if (cooldown <= 0f) return 0f;
        return Mathf.Clamp01(_cooldownTimer / cooldown);
    }
}