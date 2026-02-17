using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class FeatherChainOnHit : MonoBehaviour
{
    [Header("Chain Settings")]
    [Tooltip("Maximum total bounces (3 = hits 3 enemies after the first).")]
    public int maxBounces = 3;

    [Tooltip("Damage multiplier relative to PlayerStats.GetDamage().")]
    public float damageMultiplier = 1f;

    [Header("Motion")]
    [Tooltip("Speed while homing between targets.")]
    public float chainSpeed = 16f;

    [Tooltip("How close to the target before we consider we've arrived (meters).")]
    public float arriveDistance = 0.12f;

    [Header("Layers")]
    [Tooltip("Layer(s) considered enemies.")]
    public LayerMask enemyLayers;

    private Rigidbody _rb;
    private Collider _col;
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
        _rb       = GetComponent<Rigidbody>();
        _col      = GetComponent<Collider>();
        _ownerLink = GetComponent<FeatherOwnerLink>();

        if (_col) _col.isTrigger = true;
        if (_rb)
        {
            _rb.useGravity    = false;
            _rb.isKinematic   = true;
        }
        _bouncesUsed = 0;

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
        if (_stuck || _currentTarget == null) return;

        Vector3 to = _currentTarget.position - transform.position;
        if (to.sqrMagnitude > arriveDistance * arriveDistance)
        {
            // Move toward target on XZ plane, preserve Y
            Vector3 dir    = to.normalized;
            Vector3 newPos = transform.position + dir * chainSpeed * Time.deltaTime;
            newPos.y       = transform.position.y;
            transform.position = newPos;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_stuck) return;

        if (((1 << other.gameObject.layer) & enemyLayers.value) == 0) return;

        var eh = other.GetComponent<EnemyHealth>();
        if (!eh || _alreadyHit.Contains(eh)) return;

        float baseDmg = (_ownerLink && _ownerLink.ownerStats) ? _ownerLink.ownerStats.GetDamage() : 10f;
        eh.TakeDamage(baseDmg * Mathf.Max(0f, damageMultiplier));
        _alreadyHit.Add(eh);

        if (_bouncesUsed < maxBounces)
        {
            var next = FindNextTarget();
            if (next != null)
            {
                _bouncesUsed++;
                _currentTarget = next;
                return;
            }
        }

        StickHere();
    }

    private Transform FindNextTarget()
    {
        EnemyHealth[] allEnemies = FindObjectsOfType<EnemyHealth>();
        float bestDistSqr = float.PositiveInfinity;
        Transform best = null;

        foreach (var eh in allEnemies)
        {
            if (!eh || _alreadyHit.Contains(eh)) continue;
            if (((1 << eh.gameObject.layer) & enemyLayers.value) == 0) continue;

            float d2 = (eh.transform.position - transform.position).sqrMagnitude;
            if (d2 < bestDistSqr)
            {
                bestDistSqr = d2;
                best        = eh.transform;
            }
        }
        return best;
    }

    private void StickHere()
    {
        _stuck         = true;
        _currentTarget = null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, 2f);

        if (_currentTarget != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, _currentTarget.position);
        }
    }
}