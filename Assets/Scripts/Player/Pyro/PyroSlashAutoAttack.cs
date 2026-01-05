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

    [Header("Gizmo Settings")]
    [Tooltip("Show slash radius in editor")]
    public bool showSlashGizmo = true;

    [Tooltip("Show attack detection range")]
    public bool showDetectionGizmo = true;

    [Tooltip("Gizmo color for slash area")]
    public Color slashGizmoColor = new Color(1f, 0.5f, 0f, 0.3f);

    [Tooltip("Gizmo color for detection range")]
    public Color detectionGizmoColor = new Color(1f, 1f, 0f, 0.2f);

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

    private void OnDrawGizmosSelected()
    {
        Vector3 center = transform.position;
        Vector3 spawnPos = firePoint ? firePoint.position : center;

        if (showDetectionGizmo)
        {
            float attackRange = stats ? stats.GetAttackRange() : 5f;
            Gizmos.color = detectionGizmoColor;
            Gizmos.DrawWireSphere(center, attackRange);

            Gizmos.color = new Color(detectionGizmoColor.r, detectionGizmoColor.g, detectionGizmoColor.b, 1f);
            Gizmos.DrawLine(center, center + Vector3.right * attackRange * 0.3f);
            Gizmos.DrawLine(center, center + Vector3.up * attackRange * 0.3f);
        }

        if (showSlashGizmo)
        {
            Vector3 facingDirection = transform.right;
            Vector3 slashCenter = spawnPos + facingDirection * slashDistance;

            Gizmos.color = slashGizmoColor;
            Gizmos.DrawWireSphere(slashCenter, slashRange);

            Gizmos.color = new Color(slashGizmoColor.r, slashGizmoColor.g, slashGizmoColor.b, 1f);
            DrawArc(slashCenter, facingDirection, slashArcAngle, slashRange);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(spawnPos, slashCenter);
            Gizmos.DrawWireSphere(slashCenter, 0.1f);
        }
    }

    private void DrawArc(Vector3 center, Vector3 forward, float arcAngle, float radius)
    {
        float halfAngle = arcAngle * 0.5f;
        int segments = 20;

        Vector3 previousPoint = Vector3.zero;

        for (int i = 0; i <= segments; i++)
        {
            float angle = Mathf.Lerp(-halfAngle, halfAngle, i / (float)segments);
            float angleRad = (Mathf.Atan2(forward.y, forward.x) + angle * Mathf.Deg2Rad);

            Vector3 point = center + new Vector3(
                Mathf.Cos(angleRad) * radius,
                Mathf.Sin(angleRad) * radius,
                0f
            );

            if (i > 0)
            {
                Gizmos.DrawLine(previousPoint, point);
            }

            if (i == 0 || i == segments)
            {
                Gizmos.DrawLine(center, point);
            }

            previousPoint = point;
        }
    }
}
