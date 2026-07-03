using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerStats))]
public class FireEaterEffect : MonoBehaviour
{
    [Header("Dash Damage")]
    [Tooltip("Damage multiplier based on PlayerStats.GetDamage(). If Pyro has 10 damage and this is 2, dash deals 20 damage.")]
    public float damageScaling = 2f;

    [Tooltip("Small radius around Pyro used to detect enemies while dashing.")]
    public float hitRadius = 0.65f;

    [Tooltip("Enemy layers that can be damaged and passed through during dash.")]
    public LayerMask enemyLayers;

    [Header("Collision")]
    [Tooltip("If true, Pyro ignores collision with enemy layers while dashing.")]
    public bool ignoreEnemyCollisionDuringDash = true;

    [Header("Debug")]
    public bool showDebug = true;

    private PlayerMovement _movement;
    private PlayerStats _stats;

    private readonly HashSet<EnemyHealth> _hitThisDash = new HashSet<EnemyHealth>();

    private bool _registered;
    private bool _dashActive;
    private int _playerLayer;

    private void Awake()
    {
        _movement = GetComponent<PlayerMovement>();
        _stats = GetComponent<PlayerStats>();
        _playerLayer = gameObject.layer;
    }

    private void OnEnable()
    {
        if (_registered) return;

        if (!_movement) _movement = GetComponent<PlayerMovement>();
        if (!_stats) _stats = GetComponent<PlayerStats>();

        if (!_movement)
        {
            Debug.LogWarning("[FireEaterEffect] PlayerMovement was not found on player.");
            return;
        }

        _movement.OnDashStarted += OnDashStarted;
        _registered = true;
    }

    private void OnDisable()
    {
        if (_registered && _movement != null)
            _movement.OnDashStarted -= OnDashStarted;

        SetEnemyCollisionIgnored(false);

        _dashActive = false;
        _hitThisDash.Clear();
        _registered = false;
    }

    private void Update()
    {
        if (!_dashActive) return;

        if (_movement == null || !_movement.IsDashing())
        {
            EndDashEffect();
            return;
        }

        DamageEnemiesDuringDash();
    }

    private void OnDashStarted()
    {
        _dashActive = true;
        _hitThisDash.Clear();

        SetEnemyCollisionIgnored(true);

        if (showDebug)
            Debug.Log("[FireEaterEffect] Fire Eater dash started.");
    }

    private void EndDashEffect()
    {
        SetEnemyCollisionIgnored(false);

        _dashActive = false;
        _hitThisDash.Clear();

        if (showDebug)
            Debug.Log("[FireEaterEffect] Fire Eater dash ended.");
    }

    private void DamageEnemiesDuringDash()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, Mathf.Max(0.05f, hitRadius), enemyLayers);

        foreach (var hit in hits)
        {
            if (hit == null) continue;

            var enemy = hit.GetComponent<EnemyHealth>();
            if (enemy == null) enemy = hit.GetComponentInParent<EnemyHealth>();
            if (enemy == null) continue;

            if (_hitThisDash.Contains(enemy)) continue;

            float damage = (_stats != null ? _stats.GetDamage() : 10f) * Mathf.Max(0f, damageScaling);
            enemy.TakeDamage(damage);

            _hitThisDash.Add(enemy);

            if (showDebug)
                Debug.Log($"[FireEaterEffect] Hit {enemy.name} for {damage:F1} dash damage.");
        }
    }

    private void SetEnemyCollisionIgnored(bool ignored)
    {
        if (!ignoreEnemyCollisionDuringDash) return;

        int mask = enemyLayers.value;
        for (int layer = 0; layer < 32; layer++)
        {
            if ((mask & (1 << layer)) == 0) continue;
            Physics2D.IgnoreLayerCollision(_playerLayer, layer, ignored);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.35f, 0.05f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.05f, hitRadius));
    }
#endif
}