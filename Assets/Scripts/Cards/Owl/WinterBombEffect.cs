using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerStats))]
public class WinterBombEffect : MonoBehaviour
{
    [Header("Explosion (set by SO)")]
    public float damageMultiplier = 1.25f;
    public float explosionRadius = 3.5f;
    public LayerMask enemyLayers;

    [Header("Frosted Debuff (set by SO)")]
    [Range(0.1f, 1f)] public float slowFactor = 0.6f;
    public float slowDuration = 3f;
    public Sprite frostMarkSprite;
    public Vector2 frostMarkOffset = new Vector2(0f, 1f);
    public Vector2 frostMarkSize = new Vector2(0.5f, 0.5f);
    public float frostedDamageMultiplier = 1.15f;

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
            Debug.LogWarning("[WinterBombEffect] OwlCloneAbility not found on player.");
        }
    }

    private void Update()
    {
        if (_cloneAbility == null) return;

        var currentClone = FindNearestCloneLike();
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

    private GameObject FindNearestCloneLike()
    {
        OwlFeatherShooter[] shooters = GameObject.FindObjectsOfType<OwlFeatherShooter>();
        GameObject best = null;
        float bestDist = float.PositiveInfinity;

        foreach (var s in shooters)
        {
            if (!s) continue;
            if (s.gameObject == this.gameObject) continue;
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
            Vector3 pos = clone.transform.position;
            DoExplosion(pos);
        });
    }

    private void DoExplosion(Vector3 center)
    {
        // VFX ring
        if (spawnCircle)
        {
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

        // Damage + apply Frosted status
        float baseDmg = (_stats != null ? _stats.GetDamage() : 10f) * Mathf.Max(0f, damageMultiplier);

        var hits = Physics2D.OverlapCircleAll(center, explosionRadius, enemyLayers);
        for (int i = 0; i < hits.Length; i++)
        {
            var col = hits[i];
            if (!col) continue;

            var eh = col.GetComponent<EnemyHealth>();
            if (eh) eh.TakeDamage(baseDmg);

            // Apply Frosted status
            var frost = col.GetComponent<FrostedOnEnemy>();
            if (!frost) frost = col.gameObject.AddComponent<FrostedOnEnemy>();

            frost.Apply(
                slow: Mathf.Clamp(slowFactor, 0.1f, 1f),
                dur: Mathf.Max(0.01f, slowDuration),
                icon: frostMarkSprite,
                pivot: frostMarkOffset,
                size: frostMarkSize
            );

            // Set vulnerability multiplier so frosted enemies take bonus damage
            frost.vulnerabilityMultiplier = frostedDamageMultiplier;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 0.8f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}