using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerStats))]
public class BurningStormEffect : MonoBehaviour
{
    [Header("DoT Settings")]
    [Tooltip("Damage per second dealt to marked enemies.")]
    public float damagePerSecond = 8f;

    [Tooltip("How often to apply ticks (seconds).")]
    public float tickInterval = 0.25f;

    [Tooltip("If true, only enemies marked by THIS player take DoT. Use EnemyMark.SetMarkedBy when applying marks.")]
    public bool requireSameOwner = true;

    private PlayerStats _stats;
    private bool _registered;

    private void Awake()
    {
        _stats = GetComponent<PlayerStats>();
    }

    private void OnEnable()
    {
        if (_registered) return;
        EnemyMark.EnableBurningStormFor(transform, Mathf.Max(0f, damagePerSecond), Mathf.Max(0.05f, tickInterval), requireSameOwner);
        _registered = true;
    }

    private void OnDisable()
    {
        if (!_registered) return;
        // Only disable the per-owner entry. If you used a global setup elsewhere, don't turn it off here.
        EnemyMark.DisableBurningStormFor(transform);
        _registered = false;
    }

#if UNITY_EDITOR
    // Optional: show the effective scan radius (uses PlayerStats.attackRange)
    private void OnDrawGizmosSelected()
    {
        if (!_stats) _stats = GetComponent<PlayerStats>();
        float radius = _stats ? _stats.GetAttackRange() : 6f;
        Gizmos.color = new Color(1f, 0.4f, 0.15f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif
}
