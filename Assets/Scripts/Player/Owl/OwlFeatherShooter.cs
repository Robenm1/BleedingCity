using UnityEngine;

public class OwlFeatherShooter : MonoBehaviour
{
    public SnowOwlFeather featherPrefab;
    public Transform firePoint;
    public LayerMask enemyLayers;
    public float fireInterval = 0.35f;

    private float _t;
    private int _sharedOwnerId;
    private PlayerStats _stats;

    private void Awake()
    {
        _stats = GetComponent<PlayerStats>();
        if (_sharedOwnerId == 0) _sharedOwnerId = gameObject.GetInstanceID();
    }

    public void SetSharedOwnerId(int id) => _sharedOwnerId = id;
    public void SetRangeSource(PlayerStats stats) => _stats = stats;

    private void Update()
    {
        _t -= Time.deltaTime;
        if (_t > 0f) return;

        // Auto-shoot within range
        float range = _stats ? _stats.GetAttackRange() : 6f;
        var target = FindClosestEnemyInRange(range);
        if (!target) return;

        FireAt(target.position);

        // Use attack delay from stats
        float delay = _stats ? _stats.GetAttackDelay() : fireInterval;
        _t = delay;
    }

    private Transform FindClosestEnemyInRange(float range)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range, enemyLayers);
        Transform best = null;
        float bestD = float.PositiveInfinity;

        foreach (var h in hits)
        {
            if (!h) continue;
            float d = (h.transform.position - transform.position).sqrMagnitude;
            if (d < bestD) { bestD = d; best = h.transform; }
        }
        return best;
    }

    public void FireAt(Vector3 worldPos)
    {
        if (!featherPrefab || !_stats) return;

        Vector3 from = firePoint ? firePoint.position : transform.position;
        Vector2 dir = (worldPos - from).normalized;

        var f = Instantiate(featherPrefab, from, Quaternion.identity);

        // Get speed from stats
        float speed = _stats.GetProjectileSpeed();

        // Init feather with stats reference (no damage parameter - feather reads from stats)
        f.Init(
            ownerTf: transform,
            ownerKey: _sharedOwnerId,
            direction: dir,
            speed: speed,
            enemies: enemyLayers,
            ownerStats: _stats  // Pass stats reference for dynamic damage
        );
    }
}