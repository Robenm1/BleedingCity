using UnityEngine;

[RequireComponent(typeof(PlayerStats))]
public class PlayerAutoShoot : MonoBehaviour
{
    [Header("Targeting")]
    [Tooltip("Which layers count as enemies for targeting.")]
    public LayerMask enemyLayers;

    [Header("Firing")]
    [Tooltip("Bullet prefab to spawn (must have Bullet.cs).")]
    public GameObject bulletPrefab;

    [Tooltip("Where bullets spawn from. If null, we'll use the player's position.")]
    public Transform firePoint;

    private PlayerStats stats;
    private float shootTimer = 0f;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
    }

    private void Update()
    {
        if (shootTimer > 0f)
        {
            shootTimer -= Time.deltaTime;
            if (shootTimer < 0f) shootTimer = 0f;
        }

        TryAutoShoot();
    }

    private void TryAutoShoot()
    {
        if (shootTimer > 0f) return;
        if (bulletPrefab == null) return;

        Transform target = FindClosestEnemyInRange();
        if (target == null) return;

        ShootAt(target);

        shootTimer = stats.GetAttackDelay();
    }

    private void ShootAt(Transform target)
    {
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        Vector2 dir = ((Vector2)target.position - (Vector2)spawnPos).normalized;

        GameObject bulletObj = Object.Instantiate(bulletPrefab, spawnPos, Quaternion.identity);

        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        bulletObj.transform.rotation = Quaternion.Euler(0f, 0f, ang);

        Bullet b = bulletObj.GetComponent<Bullet>();
        if (b != null)
        {
            float dmg = stats != null ? stats.GetDamage() : 10f;
            float projSpeed = stats != null ? stats.GetProjectileSpeed() : 12f; // from PlayerStats
            b.Init(dir, dmg, projSpeed);
        }
    }

    private Transform FindClosestEnemyInRange()
    {
        float range = stats != null ? stats.GetAttackRange() : 6f;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range, enemyLayers);
        if (hits.Length == 0) return null;

        float closestDistSqr = Mathf.Infinity;
        Transform closestTarget = null;
        Vector2 myPos = transform.position;

        for (int i = 0; i < hits.Length; i++)
        {
            EnemyHealth eh = hits[i].GetComponent<EnemyHealth>();
            if (eh == null) continue;

            float dSqr = ((Vector2)hits[i].transform.position - myPos).sqrMagnitude;
            if (dSqr < closestDistSqr)
            {
                closestDistSqr = dSqr;
                closestTarget = hits[i].transform;
            }
        }

        return closestTarget;
    }

    private void OnDrawGizmosSelected()
    {
        float range = 6f;
        var ps = Application.isPlaying ? stats : GetComponent<PlayerStats>();
        if (ps != null) range = ps.GetAttackRange();

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
