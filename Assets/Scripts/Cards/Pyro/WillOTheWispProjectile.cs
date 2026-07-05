using UnityEngine;

[DisallowMultipleComponent]
public class WillOTheWispProjectile : MonoBehaviour
{
    [Header("Target")]
    public EnemyHealth target;

    [Tooltip("Enemy layers this projectile can retarget to if the original target is gone.")]
    public LayerMask enemyLayers;

    [Tooltip("How far the projectile can search for a new target.")]
    public float searchRadius = 12f;

    [Header("Damage")]
    [Tooltip("Damage dealt as percent of the target's max HP. 0.05 = 5%.")]
    public float targetMaxHpDamagePercent = 0.05f;

    [Header("Movement")]
    public float speed = 14f;
    public float hitRadius = 0.2f;
    public float lifetime = 3f;

    [Header("Debug")]
    public bool showDebug = true;

    private float _lifeTimer;

    private void OnEnable()
    {
        _lifeTimer = Mathf.Max(0.1f, lifetime);
    }

    private void Update()
    {
        _lifeTimer -= Time.deltaTime;

        if (_lifeTimer <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        if (target == null)
            target = FindNearestEnemy();

        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 targetPos = target.transform.position;
        Vector3 dir = targetPos - transform.position;

        if (dir.sqrMagnitude <= hitRadius * hitRadius)
        {
            HitTarget(target);
            return;
        }

        Vector3 move = dir.normalized * Mathf.Max(0.1f, speed) * Time.deltaTime;
        transform.position += move;
    }

    private void HitTarget(EnemyHealth enemy)
    {
        if (enemy == null)
        {
            Destroy(gameObject);
            return;
        }

        var doll = enemy.GetComponent<AbyssalDollObject>();
        if (doll == null)
            doll = enemy.GetComponentInParent<AbyssalDollObject>();

        if (doll != null)
        {
            doll.HitByEnemyMaxHealthPercent(Mathf.Max(0f, targetMaxHpDamagePercent));

            if (showDebug)
                Debug.Log("[WillOTheWispProjectile] Hit Abyssal Doll. Doll released max-HP-percent wave.");
        }
        else
        {
            float damage = enemy.maxHP * Mathf.Max(0f, targetMaxHpDamagePercent);
            enemy.TakeDamage(damage);

            if (showDebug)
                Debug.Log($"[WillOTheWispProjectile] Hit {enemy.name} for {damage:F1} max HP damage.");
        }

        Destroy(gameObject);
    }

    private EnemyHealth FindNearestEnemy()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            Mathf.Max(0.1f, searchRadius),
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

            float distSqr = ((Vector2)enemy.transform.position - (Vector2)transform.position).sqrMagnitude;

            if (distSqr < bestDistSqr)
            {
                bestDistSqr = distSqr;
                nearest = enemy;
            }
        }

        return nearest;
    }
}