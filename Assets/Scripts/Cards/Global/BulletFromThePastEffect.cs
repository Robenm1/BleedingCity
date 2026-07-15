using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerStats))]
public class BulletFromThePastEffect : MonoBehaviour
{
    public static bool IsBulletDamageInProgress { get; private set; }

    public static void BeginBulletDamage()
    {
        IsBulletDamageInProgress = true;
    }

    public static void EndBulletDamage()
    {
        IsBulletDamageInProgress = false;
    }

    [Header("Bullet from the Past")]
    [Tooltip("Bullet projectile prefab.")]
    public GameObject bulletPrefab;

    [Tooltip("Fire one bullet every X successful enemy hits.")]
    public int attacksRequired = 5;

    [Header("Targeting")]
    [Tooltip("Enemy layers used to find a bullet target.")]
    public LayerMask enemyLayers;

    [Tooltip("How far the bullet searches for a target.")]
    public float targetSearchRadius = 14f;

    [Header("Projectile")]
    public float bulletSpeed = 14f;
    public float bulletLifetime = 3f;
    public float hitRadius = 0.15f;

    [Header("Explosion")]
    public float explosionRadius = 3f;

    [Tooltip("1 = 100% player attack damage.")]
    public float damageScaling = 1f;

    [Header("Slow")]
    public float slowDuration = 2f;
    public float slowMultiplier = 0.5f;

    [Header("Visuals")]
    public string explosionStateName = "Explosion";
    public float explosionDuration = 0.45f;

    [Header("Debug")]
    public bool showDebug = true;

    private PlayerStats _stats;
    private int _attackCounter;
    private bool _registered;

    private void Awake()
    {
        _stats = GetComponent<PlayerStats>();
    }

    private void OnEnable()
    {
        Register();
    }

    private void OnDisable()
    {
        Unregister();
    }

    private void Register()
    {
        if (_registered) return;

        if (!_stats)
            _stats = GetComponent<PlayerStats>();

        EnemyHealth.OnAnyEnemyDamaged += HandleEnemyDamaged;
        _registered = true;

        if (showDebug)
            Debug.Log("[BulletFromThePastEffect] Registered to enemy damage.");
    }

    private void Unregister()
    {
        if (!_registered) return;

        EnemyHealth.OnAnyEnemyDamaged -= HandleEnemyDamaged;
        _registered = false;
    }

    private void HandleEnemyDamaged(EnemyHealth enemy, float damage)
    {
        if (enemy == null) return;
        if (damage <= 0f) return;

        // Important:
        // Do not let Bullet from the Past explosion damage charge another bullet.
        if (IsBulletDamageInProgress)
            return;

        // Do not count Abyssal Doll as a real enemy hit.
        if (enemy.GetComponent<AbyssalDollObject>() != null) return;
        if (enemy.GetComponentInParent<AbyssalDollObject>() != null) return;

        _attackCounter++;

        if (showDebug)
            Debug.Log($"[BulletFromThePastEffect] Hit count: {_attackCounter}/{attacksRequired}");

        if (_attackCounter < Mathf.Max(1, attacksRequired))
            return;

        _attackCounter = 0;
        FireBullet(enemy.transform.position);
    }

    private void FireBullet(Vector3 targetPosition)
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning("[BulletFromThePastEffect] Bullet prefab is not assigned.");
            return;
        }

        Vector3 spawnPos = transform.position;

        Vector2 direction = ((Vector2)targetPosition - (Vector2)spawnPos).normalized;

        if (direction.sqrMagnitude <= 0.01f)
            direction = GetDirectionToNearestEnemy(spawnPos);

        if (direction.sqrMagnitude <= 0.01f)
            direction = Vector2.right;

        direction.Normalize();

        GameObject obj = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        obj.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        var projectile = obj.GetComponent<BulletFromThePastProjectile>();
        if (!projectile)
        {
            Debug.LogWarning("[BulletFromThePastEffect] Bullet prefab missing BulletFromThePastProjectile.");
            Destroy(obj);
            return;
        }

        projectile.ownerStats = _stats;
        projectile.direction = direction;

        projectile.enemyLayers = enemyLayers;
        projectile.speed = Mathf.Max(0.1f, bulletSpeed);
        projectile.lifetime = Mathf.Max(0.1f, bulletLifetime);
        projectile.hitRadius = Mathf.Max(0.01f, hitRadius);

        projectile.explosionRadius = Mathf.Max(0.1f, explosionRadius);
        projectile.damageScaling = Mathf.Max(0f, damageScaling);

        projectile.slowDuration = Mathf.Max(0f, slowDuration);
        projectile.slowMultiplier = Mathf.Clamp(slowMultiplier, 0.01f, 1f);

        projectile.explosionStateName = explosionStateName;
        projectile.explosionDuration = Mathf.Max(0.05f, explosionDuration);

        projectile.showDebug = showDebug;

        if (showDebug)
            Debug.Log("[BulletFromThePastEffect] Bullet fired from player position.");
    }

    private Vector2 GetDirectionToNearestEnemy(Vector3 fromPosition)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            fromPosition,
            Mathf.Max(0.1f, targetSearchRadius),
            enemyLayers
        );

        EnemyHealth nearest = null;
        float bestDistSqr = float.MaxValue;

        foreach (var hit in hits)
        {
            if (hit == null) continue;

            var enemy = hit.GetComponent<EnemyHealth>();
            if (enemy == null) enemy = hit.GetComponentInParent<EnemyHealth>();
            if (enemy == null) continue;

            if (enemy.GetComponent<AbyssalDollObject>() != null) continue;
            if (enemy.GetComponentInParent<AbyssalDollObject>() != null) continue;

            float distSqr = ((Vector2)enemy.transform.position - (Vector2)fromPosition).sqrMagnitude;

            if (distSqr < bestDistSqr)
            {
                bestDistSqr = distSqr;
                nearest = enemy;
            }
        }

        if (nearest != null)
            return ((Vector2)nearest.transform.position - (Vector2)fromPosition).normalized;

        return Vector2.right;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.1f, targetSearchRadius));
    }
#endif
}