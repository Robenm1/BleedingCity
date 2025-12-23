using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class SacrificialDummyEffect : MonoBehaviour
{
    [Header("Turret Spawn Chances (set by SO)")]
    public float baseTurretChance = 0.2f;
    public float frostedTurretChance = 1.0f;
    public float shardFrostChance = 0.2f;

    [Header("Turret Settings (set by SO)")]
    public Color turretTint = new Color(0.7f, 0.9f, 1f, 1f);
    public int shardsToFire = 6;
    public float fireInterval = 0.12f;
    public float spreadAngle = 20f;
    public float seekRange = 8f;

    [Header("Projectile (set by SO)")]
    public GameObject frostShardPrefab;
    public float shardSpeed = 18f;
    public float shardLifetime = 2.5f;
    public float shardDamage = 12f;
    public LayerMask enemyLayers;

    [Header("Frost Effect (set by SO)")]
    public float frostSlow = 0.6f;
    public float frostDuration = 2.5f;
    public Sprite frostIcon;
    public Vector2 frostIconPivot = new Vector2(0f, 0.85f);
    public Vector2 frostIconSize = new Vector2(0.35f, 0.35f);

    [Header("Debug")]
    public bool enableDebugLogs = true;

    private bool _enabled;

    private void OnEnable()
    {
        if (!_enabled)
        {
            EnemyHealth.OnAnyEnemyDied += OnEnemyDied;
            _enabled = true;
            if (enableDebugLogs) Debug.Log("[SacrificialDummyEffect] Subscribed to enemy death events.");
        }
    }

    private void OnDisable()
    {
        if (_enabled)
        {
            EnemyHealth.OnAnyEnemyDied -= OnEnemyDied;
            _enabled = false;
            if (enableDebugLogs) Debug.Log("[SacrificialDummyEffect] Unsubscribed from enemy death events.");
        }
    }

    private void OnDestroy()
    {
        if (_enabled)
        {
            EnemyHealth.OnAnyEnemyDied -= OnEnemyDied;
            _enabled = false;
        }
    }

    private void OnEnemyDied(EnemyHealth dead)
    {
        if (enableDebugLogs) Debug.Log($"[SacrificialDummyEffect] Enemy died: {dead.name}");

        if (!dead)
        {
            if (enableDebugLogs) Debug.LogWarning("[SacrificialDummyEffect] Dead enemy is null!");
            return;
        }

        if (!frostShardPrefab)
        {
            if (enableDebugLogs) Debug.LogWarning("[SacrificialDummyEffect] Frost shard prefab is not assigned!");
            return;
        }

        var frosted = dead.GetComponent<FrostedOnEnemy>();
        bool wasFrosted = (frosted != null && frosted.IsActive);

        float chance = wasFrosted ? frostedTurretChance : baseTurretChance;
        float roll = Random.value;

        if (enableDebugLogs)
            Debug.Log($"[SacrificialDummyEffect] Frosted: {wasFrosted}, Chance: {chance * 100}%, Roll: {roll * 100}%");

        if (roll > chance)
        {
            if (enableDebugLogs) Debug.Log("[SacrificialDummyEffect] Roll failed, no turret.");
            return;
        }

        if (enableDebugLogs) Debug.Log($"[SacrificialDummyEffect] Converting {dead.name} to turret!");

        dead.MarkAsConvertingToTurret();
        StartCoroutine(ConvertToTurretCoroutine(dead.gameObject, dead.transform.position, wasFrosted));
    }

    private IEnumerator ConvertToTurretCoroutine(GameObject enemyGO, Vector3 frozenPos, bool wasFrosted)
    {
        yield return null;

        if (!enemyGO)
        {
            if (enableDebugLogs) Debug.LogWarning("[SacrificialDummyEffect] Enemy GameObject destroyed before conversion!");
            yield break;
        }

        if (enableDebugLogs) Debug.Log($"[SacrificialDummyEffect] Converting {enemyGO.name} at {frozenPos}, wasFrosted: {wasFrosted}");

        var eh = enemyGO.GetComponent<EnemyHealth>();
        if (eh)
        {
            eh.enabled = false;
            if (eh.hpUIRoot != null) Destroy(eh.hpUIRoot.gameObject);
        }

        var follow = enemyGO.GetComponent<EnemyFollow>();
        if (follow) follow.enabled = false;

        var rb = enemyGO.GetComponent<Rigidbody2D>();
        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Static;
        }

        var colliders = enemyGO.GetComponents<Collider2D>();
        foreach (var col in colliders)
        {
            if (col) col.enabled = false;
        }

        enemyGO.transform.position = frozenPos;

        var renderers = enemyGO.GetComponentsInChildren<SpriteRenderer>();
        foreach (var rend in renderers)
        {
            rend.color = turretTint;
        }

        Vector2 lockedDirection = FindNearestEnemyDir(frozenPos);

        if (enableDebugLogs) Debug.Log($"[SacrificialDummyEffect] Turret firing {shardsToFire} shards in direction: {lockedDirection}");

        for (int i = 0; i < shardsToFire; i++)
        {
            if (!enemyGO) yield break;

            enemyGO.transform.position = frozenPos;
            FireShardInDirection(frozenPos, lockedDirection, wasFrosted);

            yield return new WaitForSeconds(fireInterval);
        }

        if (enableDebugLogs) Debug.Log($"[SacrificialDummyEffect] Turret finished, destroying");
        if (enemyGO) Destroy(enemyGO);
    }

    private void FireShardInDirection(Vector3 origin, Vector2 direction, bool fromFrostedEnemy)
    {
        if (!frostShardPrefab) return;

        float angle = Random.Range(-spreadAngle * 0.1f, spreadAngle * 0.1f);
        Vector2 finalDir = Quaternion.Euler(0f, 0f, angle) * direction;

        var go = Instantiate(frostShardPrefab, origin, Quaternion.identity);
        var shard = go.GetComponent<FrostShardProjectile>();
        if (!shard) shard = go.AddComponent<FrostShardProjectile>();

        shard.enemyLayers = enemyLayers;
        shard.speed = shardSpeed;
        shard.lifetime = shardLifetime;
        shard.damage = shardDamage;

        bool shouldApplyFrost = fromFrostedEnemy && (Random.value <= shardFrostChance);

        if (enableDebugLogs)
            Debug.Log($"[SacrificialDummyEffect] fromFrostedEnemy: {fromFrostedEnemy}, shardFrostChance: {shardFrostChance}, roll: {Random.value}, shouldApplyFrost: {shouldApplyFrost}");

        shard.frostOnHit = shouldApplyFrost;
        if (shouldApplyFrost)
        {
            shard.frostSlowFactor = frostSlow;
            shard.frostDuration = frostDuration;
            shard.frostIcon = frostIcon;
            shard.iconPivot = frostIconPivot;
            shard.iconSize = frostIconSize;

            if (enableDebugLogs)
                Debug.Log($"[SacrificialDummyEffect] Shard configured with frost: slow={frostSlow}, duration={frostDuration}, icon={(frostIcon != null ? frostIcon.name : "null")}");
        }

        shard.Launch(finalDir);
    }

    private Vector2 FindNearestEnemyDir(Vector3 from)
    {
        EnemyHealth[] allEnemies = FindObjectsOfType<EnemyHealth>();
        Transform best = null;
        float bestDist = float.MaxValue;

        foreach (var enemy in allEnemies)
        {
            if (!enemy || !enemy.enabled) continue;
            if (((1 << enemy.gameObject.layer) & enemyLayers.value) == 0) continue;

            float dist = (enemy.transform.position - from).sqrMagnitude;
            if (dist > 0.25f && dist < bestDist)
            {
                bestDist = dist;
                best = enemy.transform;
            }
        }

        if (best != null)
            return ((Vector2)best.position - (Vector2)from).normalized;

        float randAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(randAngle), Mathf.Sin(randAngle));
    }
}
