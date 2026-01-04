using UnityEngine;

public class PyroSlashAutoAttack : MonoBehaviour
{
    [Header("Slash Settings")]
    [Tooltip("Slash VFX prefab to spawn")]
    public GameObject slashPrefab;

    [Tooltip("Where slash spawns (in front of player)")]
    public Transform firePoint;

    [Tooltip("Enemy layers to detect and damage")]
    public LayerMask enemyLayers;

    [Header("Timing")]
    [Tooltip("Base attack interval (overridden by PlayerStats)")]
    public float baseAttackInterval = 0.8f;

    [Header("Slash Properties")]
    [Tooltip("How far in front of player to spawn slash")]
    public float slashDistance = 1.5f;

    [Tooltip("Slash arc angle (degrees)")]
    public float slashArcAngle = 120f;

    [Tooltip("Slash range/radius")]
    public float slashRange = 2f;

    private float attackTimer;
    private PlayerStats stats;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
        if (!stats)
        {
            Debug.LogError("[PyroSlashAutoAttack] Missing PlayerStats component!");
            enabled = false;
        }
    }

    private void Update()
    {
        attackTimer -= Time.deltaTime;
        if (attackTimer > 0f) return;

        float attackRange = stats ? stats.GetAttackRange() : 5f;
        var target = FindClosestEnemyInRange(attackRange);

        if (target != null)
        {
            SlashTowards(target.position);

            float delay = stats ? stats.GetAttackDelay() : baseAttackInterval;
            attackTimer = delay;
        }
    }

    private Transform FindClosestEnemyInRange(float range)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range, enemyLayers);
        Transform closest = null;
        float closestDistance = float.PositiveInfinity;

        foreach (var hit in hits)
        {
            if (!hit) continue;

            float distance = (hit.transform.position - transform.position).sqrMagnitude;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = hit.transform;
            }
        }

        return closest;
    }

    private void SlashTowards(Vector3 targetPosition)
    {
        if (!slashPrefab) return;

        Vector3 spawnPos = firePoint ? firePoint.position : transform.position;
        Vector2 direction = (targetPosition - transform.position).normalized;

        Vector3 slashPosition = spawnPos + (Vector3)direction * slashDistance;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion slashRotation = Quaternion.Euler(0f, 0f, angle);

        GameObject slashObj = Instantiate(slashPrefab, slashPosition, slashRotation);

        var slashComponent = slashObj.GetComponent<PyroSlash>();
        if (!slashComponent)
        {
            slashComponent = slashObj.AddComponent<PyroSlash>();
        }

        float damage = stats ? stats.GetDamage() : 10f;

        slashComponent.Initialize(
            damage: damage,
            arcAngle: slashArcAngle,
            range: slashRange,
            enemyLayers: enemyLayers,
            sourcePosition: slashPosition,
            direction: direction
        );
    }
}
