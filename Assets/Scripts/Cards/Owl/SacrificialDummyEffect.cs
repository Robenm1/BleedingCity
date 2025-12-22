using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class SacrificialDummyEffect : MonoBehaviour
{
    [HideInInspector] public float baseTurretChance = 0.2f;
    [HideInInspector] public float frostedTurretChance = 1.0f;
    [HideInInspector] public float shardFrostChance = 0.2f;
    [HideInInspector] public Color turretTint = new Color(0.7f, 0.9f, 1f, 1f);
    [HideInInspector] public int shardsToFire = 6;
    [HideInInspector] public float fireInterval = 0.12f;
    [HideInInspector] public float spreadAngle = 20f;
    [HideInInspector] public float seekRange = 8f;
    [HideInInspector] public GameObject frostShardPrefab;
    [HideInInspector] public float shardSpeed = 18f;
    [HideInInspector] public float shardLifetime = 2.5f;
    [HideInInspector] public float shardDamage = 12f;
    [HideInInspector] public LayerMask enemyLayers;
    [HideInInspector] public float frostSlow = 0.6f;
    [HideInInspector] public float frostDuration = 2.5f;
    [HideInInspector] public Sprite frostIcon;
    [HideInInspector] public Vector2 frostIconPivot = new Vector2(0f, 0.85f);
    [HideInInspector] public Vector2 frostIconSize = new Vector2(0.35f, 0.35f);

    private bool _enabled;

    public void Enable()
    {
        if (_enabled) return;

        Debug.Log("[SacrificialDummy] Enabling effect - subscribing to enemy deaths");

        EnemyHealth.OnAnyEnemyDied += OnEnemyDied;
        _enabled = true;
    }

    private void OnDestroy()
    {
        if (_enabled)
        {
            Debug.Log("[SacrificialDummy] Destroying - unsubscribing from enemy deaths");
            EnemyHealth.OnAnyEnemyDied -= OnEnemyDied;
        }
        _enabled = false;
    }

    private void OnEnemyDied(EnemyHealth dead)
    {
        Debug.Log($"[SacrificialDummy] Enemy died! Checking conversion... Prefab null? {frostShardPrefab == null}");

        if (!dead || !frostShardPrefab)
        {
            Debug.Log("[SacrificialDummy] Skipping - dead enemy or no prefab");
            return;
        }

        // Check if enemy was frosted
        var frosted = dead.GetComponent<FrostedOnEnemy>();
        bool isFrosted = (frosted && frosted.IsActive);

        Debug.Log($"[SacrificialDummy] Enemy frosted? {isFrosted}");

        // Determine chance based on frosted status
        float chance = isFrosted ? frostedTurretChance : baseTurretChance;
        float roll = Random.value;

        Debug.Log($"[SacrificialDummy] Chance: {chance}, Roll: {roll}, Success? {roll <= chance}");

        if (roll > chance)
        {
            Debug.Log("[SacrificialDummy] Failed chance roll - enemy dies normally");
            return;
        }

        Debug.Log("[SacrificialDummy] SUCCESS! Converting to turret!");

        // PREVENT destruction by marking it BEFORE Die() is called
        dead.MarkAsConvertingToTurret();

        // Store reference
        GameObject enemyGO = dead.gameObject;
        Vector3 deathPosition = enemyGO.transform.position;

        // Convert to turret
        StartCoroutine(ConvertToTurretDelayed(enemyGO, deathPosition, isFrosted));
    }

    private IEnumerator ConvertToTurretDelayed(GameObject enemyGO, Vector3 position, bool wasFrosted)
    {
        // Wait one frame for everything to settle
        yield return null;

        if (!enemyGO) yield break;

        ConvertToTurret(enemyGO, position, wasFrosted);
    }

    private void ConvertToTurret(GameObject enemyGO, Vector3 frozenPosition, bool wasFrosted)
    {
        if (!enemyGO) return;

        // CRITICAL: Cancel any pending Destroy on this GameObject
        // This won't work on objects already destroyed, but will work on Destroy() calls with delay

        // Disable EnemyHealth to prevent further damage/death
        var eh = enemyGO.GetComponent<EnemyHealth>();
        if (eh)
        {
            eh.enabled = false;
            // Hide HP bar
            if (eh.hpUIRoot != null)
            {
                Destroy(eh.hpUIRoot.gameObject);
                eh.hpUIRoot = null;
            }
        }

        // Disable movement
        var follow = enemyGO.GetComponent<EnemyFollow>();
        if (follow)
        {
            follow.enabled = false;
            follow.moveSpeed = 0f;
        }

        // Freeze rigidbody completely
        var rb = enemyGO.GetComponent<Rigidbody2D>();
        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Static;
        }

        // Disable colliders
        var colliders = enemyGO.GetComponents<Collider2D>();
        foreach (var col in colliders)
        {
            if (col) col.enabled = false;
        }

        // Lock position
        enemyGO.transform.position = frozenPosition;

        // Change color to light blue
        var renderers = enemyGO.GetComponentsInChildren<SpriteRenderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].color = turretTint;
        }

        // Start firing shards - run on THIS component (player), not the enemy
        StartCoroutine(TurretRoutine(enemyGO, frozenPosition, wasFrosted));
    }

    private IEnumerator TurretRoutine(GameObject turretGO, Vector3 frozenPosition, bool wasFrosted)
    {
        if (!turretGO) yield break;

        // Fire all shards with intervals
        for (int i = 0; i < shardsToFire; i++)
        {
            if (!turretGO) yield break;

            // Keep locked in position
            turretGO.transform.position = frozenPosition;

            FireShard(frozenPosition, wasFrosted);

            yield return new WaitForSeconds(fireInterval);
        }

        // After all shards fired, destroy the turret
        if (turretGO)
        {
            Destroy(turretGO);
        }
    }

    private void FireShard(Vector3 origin, bool fromFrostedEnemy)
    {
        if (!frostShardPrefab) return;

        // Find nearest LIVING enemy to aim at
        Vector2 aimDir = FindNearestLivingEnemyDirection(origin);

        // Add random spread
        float offset = Random.Range(-spreadAngle, spreadAngle);
        aimDir = Quaternion.Euler(0f, 0f, offset) * aimDir;

        // Spawn shard
        var go = Instantiate(frostShardPrefab, origin, Quaternion.identity);
        var shard = go.GetComponent<FrostShardProjectile>();
        if (!shard) shard = go.AddComponent<FrostShardProjectile>();

        shard.enemyLayers = enemyLayers;
        shard.speed = shardSpeed;
        shard.lifetime = shardLifetime;
        shard.damage = shardDamage;

        // 20% chance to apply frost if from frosted enemy
        bool applyFrost = fromFrostedEnemy && (Random.value <= shardFrostChance);

        shard.frostOnHit = applyFrost;
        if (applyFrost)
        {
            shard.frostSlowFactor = frostSlow;
            shard.frostDuration = frostDuration;
            shard.frostIcon = frostIcon;
            shard.iconPivot = frostIconPivot;
            shard.iconSize = frostIconSize;
        }

        shard.Launch(aimDir);
    }

    private Vector2 FindNearestLivingEnemyDirection(Vector3 from)
    {
        // Find ALL enemies in scene
        EnemyHealth[] allEnemies = GameObject.FindObjectsOfType<EnemyHealth>();
        Transform best = null;
        float bestDist = float.PositiveInfinity;

        foreach (var enemy in allEnemies)
        {
            if (!enemy || !enemy.enabled) continue; // Skip disabled enemies (turrets)

            // Check if on enemy layer
            if (((1 << enemy.gameObject.layer) & enemyLayers.value) == 0) continue;

            float d = (enemy.transform.position - from).sqrMagnitude;

            // Must be at least 0.5 units away (not self)
            if (d > 0.25f && d < bestDist)
            {
                bestDist = d;
                best = enemy.transform;
            }
        }

        if (best != null)
        {
            return ((Vector2)best.position - (Vector2)from).normalized;
        }

        // No enemies found - aim at random direction
        float angle = Random.Range(0f, 360f);
        return new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
    }
}