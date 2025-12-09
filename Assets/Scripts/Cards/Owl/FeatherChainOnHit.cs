using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class FeatherChainOnHit : MonoBehaviour
{
    [Header("Chain Settings")]
    [Tooltip("Maximum total bounces (3 = hits 3 enemies after the first).")]
    public int maxBounces = 3;

    [Tooltip("Damage multiplier relative to PlayerStats.GetDamage(). 1 = base damage.")]
    public float damageMultiplier = 1f;

    [Header("Motion")]
    [Tooltip("Speed while homing between targets.")]
    public float chainSpeed = 16f;

    [Tooltip("How close to the target before we consider we've arrived (meters).")]
    public float arriveDistance = 0.12f;

    [Header("Layers")]
    [Tooltip("Layer(s) considered enemies. Set to only 'Enemy'.")]
    public LayerMask enemyLayers;

    private Rigidbody2D _rb;
    private Collider2D _col;
    private FeatherOwnerLink _ownerLink;

    private readonly HashSet<EnemyHealth> _alreadyHit = new HashSet<EnemyHealth>();
    private Transform _currentTarget;
    private int _bouncesUsed;
    private bool _stuck;

    private void Reset()
    {
        enemyLayers = LayerMask.GetMask("Enemy");
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();
        _ownerLink = GetComponent<FeatherOwnerLink>();

        if (_col) _col.isTrigger = true;
        _bouncesUsed = 0;

        // Default to "Enemy" layer if empty
        if (enemyLayers.value == 0)
            enemyLayers = LayerMask.GetMask("Enemy");
    }

    private void Start()
    {
        if (_ownerLink == null)
            _ownerLink = gameObject.AddComponent<FeatherOwnerLink>();

        if (_ownerLink.ownerStats == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player)
            {
                var ps = player.GetComponent<PlayerStats>();
                if (ps) _ownerLink.StampOwner(player.transform, ps);
            }
        }
    }

    private void Update()
    {
        if (_stuck) return;

        // Home toward current target (if any)
        if (_currentTarget != null)
        {
            Vector2 pos = transform.position;
            Vector2 tgt = _currentTarget.position;
            Vector2 to = tgt - pos;

            // Keep moving toward target until we arrive
            if (to.sqrMagnitude > arriveDistance * arriveDistance)
            {
                Vector2 v = to.normalized * chainSpeed;
                _rb.linearVelocity = v;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_stuck) return;

        // Ignore non-enemy layers
        if (((1 << other.gameObject.layer) & enemyLayers.value) == 0)
            return;

        var eh = other.GetComponent<EnemyHealth>();
        if (!eh) return;
        if (_alreadyHit.Contains(eh)) return;

        // Deal damage using player stats
        float baseDmg = (_ownerLink && _ownerLink.ownerStats) ? _ownerLink.ownerStats.GetDamage() : 10f;
        eh.TakeDamage(baseDmg * Mathf.Max(0f, damageMultiplier));
        _alreadyHit.Add(eh);

        // Check if we can bounce to another enemy
        if (_bouncesUsed < maxBounces)
        {
            var next = FindNextTarget();
            if (next != null)
            {
                _bouncesUsed++;
                _currentTarget = next;
                // Set initial velocity toward next target
                Vector2 dir = ((Vector2)next.position - (Vector2)transform.position).normalized;
                _rb.linearVelocity = dir * chainSpeed;
                return;
            }
        }

        // No more bounces or no targets found: stick to ground
        StickHere();
    }

    private Transform FindNextTarget()
    {
        // Find ALL enemies in the scene (no distance limit)
        EnemyHealth[] allEnemies = FindObjectsOfType<EnemyHealth>();
        float bestDistSqr = float.PositiveInfinity;
        Transform best = null;

        foreach (var eh in allEnemies)
        {
            if (!eh || _alreadyHit.Contains(eh)) continue;

            // Check if this enemy is on the correct layer
            if (((1 << eh.gameObject.layer) & enemyLayers.value) == 0)
                continue;

            float d2 = (eh.transform.position - transform.position).sqrMagnitude;
            if (d2 < bestDistSqr)
            {
                bestDistSqr = d2;
                best = eh.transform;
            }
        }
        return best;
    }

    private void StickHere()
    {
        _stuck = true;
        _currentTarget = null;
        if (_rb)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Visual indicator showing unlimited range
        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, 2f);

        if (_currentTarget != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, _currentTarget.position);
        }
    }
}