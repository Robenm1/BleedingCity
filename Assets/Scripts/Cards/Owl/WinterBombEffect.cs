using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Attach to the Owl player (via SO.Apply). It detects newly spawned clones,
/// hooks a destroy callback, then explodes on expiration. Also spawns a circle ring using ExplosionRingFX.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerStats))]
public class WinterBombEffect : MonoBehaviour
{
    [Header("Explosion (set by SO)")]
    public float damageMultiplier = 1.25f;
    public float explosionRadius = 3.5f;
    public LayerMask enemyLayers;

    [Header("Frosted Debuff (set by SO)")]
    public bool applyFrost = true;
    [Range(0.1f, 1f)] public float slowFactor = 0.6f;
    public float slowDuration = 3f;
    public Sprite frostMarkSprite;
    public Vector2 frostMarkOffset = new Vector2(0f, 1f);
    public Vector2 frostMarkSize = new Vector2(0.5f, 0.5f);

    [Header("Circle Pulse VFX (set by SO)")]
    public bool spawnCircle = true;
    public Color circleColor = new Color(0.6f, 0.8f, 1f, 0.55f);
    public float circleDuration = 0.5f;
    public float circleLineWidth = 0.08f;
    [Range(8, 256)] public int circleSegments = 72;
    [Range(0f, 1f)] public float circleStartRadiusFraction = 0.55f;

    private PlayerStats _stats;
    private OwlCloneAbility _cloneAbility;
    private GameObject _lastHookedClone;

    private void Awake()
    {
        _stats = GetComponent<PlayerStats>();
        _cloneAbility = GetComponent<OwlCloneAbility>();
        if (_cloneAbility == null)
        {
            Debug.LogWarning("[WinterBombEffect] OwlCloneAbility not found on player. Effect will be idle.");
        }
    }

    private void Update()
    {
        // Poll for a newly spawned clone and wire it.
        if (_cloneAbility == null) return;

        var currentClone = GetActiveClone();
        if (currentClone != null && currentClone != _lastHookedClone)
        {
            HookClone(currentClone);
            _lastHookedClone = currentClone;
        }
        else if (currentClone == null)
        {
            _lastHookedClone = null;
        }
    }

    private GameObject GetActiveClone()
    {
        // OwlCloneAbility exposes swap and spawn, but we didn’t modify it.
        // We’ll find the clone by looking for a SimpleLifetime tagged sibling spawned by the ability near the player.
        // Better approach: expose a method on OwlCloneAbility to return the active clone.
        // If you already have one, replace this with that call.
        // Fallback here: try to find a clone with an OnDestroyCallback created recently and same owner project (optional).
        // To keep it simple: we search for the nearest object with SimpleLifetime that has OwlFeatherShooter and is not the player.

        // NOTE: if you already have a public accessor in OwlCloneAbility, use that instead.
        return FindNearestCloneLike();
    }

    private GameObject FindNearestCloneLike()
    {
        OwlFeatherShooter[] shooters = GameObject.FindObjectsOfType<OwlFeatherShooter>();
        GameObject best = null;
        float bestDist = float.PositiveInfinity;

        foreach (var s in shooters)
        {
            if (!s) continue;
            if (s.gameObject == this.gameObject) continue; // skip player

            // Heuristic: treat non-player shooters with SimpleLifetime as clones
            if (!s.GetComponent<SimpleLifetime>()) continue;

            float d = (s.transform.position - transform.position).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = s.gameObject;
            }
        }
        return best;
    }

    private void HookClone(GameObject clone)
    {
        if (!clone) return;

        var cb = clone.GetComponent<OnDestroyCallback>();
        if (!cb) cb = clone.AddComponent<OnDestroyCallback>();
        cb.Init(() =>
        {
            // Clone is destroying -> explode at last position
            Vector3 pos = clone.transform.position;
            DoExplosion(pos);
        });
    }

    private void DoExplosion(Vector3 center)
    {
        // 1) VFX ring (if assigned)
        if (spawnCircle)
        {
            // Requires ExplosionRingFX.cs in your project (the utility you just added).
            ExplosionRingFX.Spawn(
                worldPos: center,
                radius: explosionRadius,
                color: circleColor,
                width: circleLineWidth,
                duration: circleDuration,
                segments: circleSegments,
                startFrac: circleStartRadiusFraction
            );
        }

        // 2) Gameplay: damage + optional frost
        float dmg = (_stats != null ? _stats.GetDamage() : 10f) * Mathf.Max(0f, damageMultiplier);

        var hits = Physics2D.OverlapCircleAll(center, explosionRadius, enemyLayers);
        for (int i = 0; i < hits.Length; i++)
        {
            var col = hits[i];
            if (!col) continue;

            var eh = col.GetComponent<EnemyHealth>();
            if (eh) eh.TakeDamage(dmg);

            if (applyFrost)
            {
                var frost = col.GetComponent<FrostedOnEnemy>();
                if (!frost) frost = col.gameObject.AddComponent<FrostedOnEnemy>();

                frost.Apply(
                    slowFactor: Mathf.Clamp(slowFactor, 0.1f, 1f),
                    duration: Mathf.Max(0.01f, slowDuration),
                    markSprite: frostMarkSprite,
                    offset: frostMarkOffset,
                    size: frostMarkSize
                );
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 0.8f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
