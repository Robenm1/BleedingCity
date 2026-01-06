using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FireGolem : MonoBehaviour
{
    [Header("Owner")]
    public Transform owner;

    [Header("Stats")]
    public float maxHP = 500f;
    public float moveSpeed = 2f;
    public float aggroRange = 20f;

    [Header("Combat")]
    public float attackRange = 3f;
    public float attackCooldown = 2.5f;
    public float hit1Damage = 50f;
    public float hit2Damage = 50f;
    public float hit3Damage = 100f;
    public float hit1KnockbackForce = 2f;
    public float hit2KnockbackForce = 2f;
    public float hit3KnockbackForce = 3f;
    public float knockbackStunDuration = 0.2f;
    public float aoeRadius = 5f;

    [Header("Slow Zone")]
    public GameObject slowZonePrefab;
    public float slowZoneDuration = 5f;
    public float slowZoneRadius = 5f;
    public float slowPercent = 0.5f;

    [Header("Layers")]
    public LayerMask enemyLayers;

    [Header("Visuals")]
    public SpriteRenderer bodySprite;
    public Color damageFlashColor = Color.red;
    public float damageFlashDuration = 0.1f;

    [Header("Debug")]
    public bool showDebug = true;

    private float currentHP;
    private float attackTimer;
    private int comboStep = 0;
    private bool isAttacking;
    private Transform currentTarget;
    private Transform comboTarget;
    private Color originalColor;
    private Rigidbody2D rb;

    private void Awake()
    {
        currentHP = maxHP;
        rb = GetComponent<Rigidbody2D>();

        if (!rb)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        if (bodySprite)
        {
            originalColor = bodySprite.color;
        }
    }

    private void Update()
    {
        if (!owner)
        {
            Destroy(gameObject);
            return;
        }

        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
        }

        if (!isAttacking)
        {
            FindAndMoveToTarget();
        }
    }

    private void FindAndMoveToTarget()
    {
        Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, aggroRange, enemyLayers);

        if (enemies.Length == 0)
        {
            FollowOwner();
            currentTarget = null;
            return;
        }

        Transform closest = null;
        float closestDist = float.MaxValue;

        foreach (var enemy in enemies)
        {
            if (!enemy) continue;
            float dist = Vector2.Distance(transform.position, enemy.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = enemy.transform;
            }
        }

        if (closest)
        {
            currentTarget = closest;
            float distToTarget = Vector2.Distance(transform.position, closest.position);

            if (distToTarget <= attackRange)
            {
                if (attackTimer <= 0f && !isAttacking)
                {
                    StartCoroutine(PerformComboAttack(closest));
                }
                else
                {
                    rb.linearVelocity = Vector2.zero;
                }
            }
            else
            {
                Vector2 direction = (closest.position - transform.position).normalized;
                rb.linearVelocity = direction * moveSpeed;
            }
        }
    }

    private void FollowOwner()
    {
        if (!owner) return;

        float distToOwner = Vector2.Distance(transform.position, owner.position);
        if (distToOwner > 3f)
        {
            Vector2 direction = (owner.position - transform.position).normalized;
            rb.linearVelocity = direction * moveSpeed;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private IEnumerator PerformComboAttack(Transform target)
    {
        isAttacking = true;
        comboStep = 0;
        comboTarget = target;
        rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(0.3f);

        if (!IsTargetValid(comboTarget))
        {
            EndCombo();
            yield break;
        }

        PerformHit1();
        yield return new WaitForSeconds(0.5f);

        if (!IsTargetValid(comboTarget))
        {
            EndCombo();
            yield break;
        }

        PerformHit2();
        yield return new WaitForSeconds(0.5f);

        if (!IsTargetValid(comboTarget))
        {
            EndCombo();
            yield break;
        }

        PerformHit3();

        EndCombo();
    }

    private void EndCombo()
    {
        attackTimer = attackCooldown;
        isAttacking = false;
        comboStep = 0;
        comboTarget = null;
    }

    private bool IsTargetValid(Transform target)
    {
        if (target == null) return false;

        var enemyHealth = target.GetComponent<EnemyHealth>();
        return enemyHealth != null;
    }

    private void PerformHit1()
    {
        comboStep = 1;

        if (comboTarget == null) return;

        var enemyHealth = comboTarget.GetComponent<EnemyHealth>();
        if (enemyHealth)
        {
            enemyHealth.TakeDamage(hit1Damage);
        }

        ApplyKnockback(comboTarget, hit1KnockbackForce, knockbackStunDuration);

        if (showDebug) Debug.Log($"[FireGolem] Hit 1! Damage: {hit1Damage} to {comboTarget.name}");
    }

    private void PerformHit2()
    {
        comboStep = 2;

        if (comboTarget == null) return;

        var enemyHealth = comboTarget.GetComponent<EnemyHealth>();
        if (enemyHealth)
        {
            enemyHealth.TakeDamage(hit2Damage);
        }

        ApplyKnockback(comboTarget, hit2KnockbackForce, knockbackStunDuration);

        if (showDebug) Debug.Log($"[FireGolem] Hit 2! Damage: {hit2Damage} to {comboTarget.name}");
    }

    private void PerformHit3()
    {
        comboStep = 3;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, aoeRadius, enemyLayers);

        foreach (var hit in hits)
        {
            if (!hit) continue;

            var enemyHealth = hit.GetComponent<EnemyHealth>();
            if (enemyHealth)
            {
                enemyHealth.TakeDamage(hit3Damage);
            }

            ApplyKnockback(hit.transform, hit3KnockbackForce, knockbackStunDuration);
        }

        SpawnSlowZone();

        if (showDebug) Debug.Log($"[FireGolem] Hit 3 (AOE)! Damage: {hit3Damage}, Radius: {aoeRadius}");
    }

    private void ApplyKnockback(Transform target, float force, float stunDuration)
    {
        if (!target) return;

        Vector2 knockbackDir = (target.position - transform.position).normalized;

        var enemyRb = target.GetComponent<Rigidbody2D>();
        if (enemyRb)
        {
            enemyRb.linearVelocity = knockbackDir * force;
        }

        var enemyFollow = target.GetComponent<EnemyFollow>();
        if (enemyFollow)
        {
            StartCoroutine(StunEnemy(enemyFollow, stunDuration));
        }
    }

    private IEnumerator StunEnemy(EnemyFollow enemy, float duration)
    {
        if (!enemy) yield break;

        enemy.enabled = false;
        yield return new WaitForSeconds(duration);

        if (enemy)
        {
            enemy.enabled = true;
        }
    }

    private void SpawnSlowZone()
    {
        if (!slowZonePrefab) return;

        GameObject zone = Instantiate(slowZonePrefab, transform.position, Quaternion.identity);
        var slowZone = zone.GetComponent<GolemSlowZone>();
        if (slowZone)
        {
            slowZone.duration = slowZoneDuration;
            slowZone.radius = slowZoneRadius;
            slowZone.slowPercent = slowPercent;
            slowZone.enemyLayers = enemyLayers;
        }
    }

    public void TakeDamage(float damage)
    {
        currentHP -= damage;

        if (bodySprite)
        {
            StartCoroutine(FlashDamage());
        }

        if (showDebug) Debug.Log($"[FireGolem] Took {damage} damage. HP: {currentHP}/{maxHP}");

        if (currentHP <= 0f)
        {
            Die();
        }
    }

    private IEnumerator FlashDamage()
    {
        if (bodySprite)
        {
            bodySprite.color = damageFlashColor;
            yield return new WaitForSeconds(damageFlashDuration);
            bodySprite.color = originalColor;
        }
    }

    private void Die()
    {
        if (showDebug) Debug.Log("[FireGolem] Golem destroyed!");

        var tracker = owner?.GetComponent<SummonEvolutionTracker>();
        if (tracker)
        {
            tracker.OnGolemDied();
        }

        Destroy(gameObject);
    }

    public void ActivateTauntMode(float duration)
    {
        StartCoroutine(TauntCoroutine(duration));
    }

    private IEnumerator TauntCoroutine(float duration)
    {
        if (showDebug) Debug.Log($"[FireGolem] TAUNT MODE for {duration}s - All enemies attack me!");

        float elapsed = 0f;
        while (elapsed < duration)
        {
            Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, aggroRange * 2f, enemyLayers);

            foreach (var enemy in enemies)
            {
                if (!enemy) continue;

                var enemyFollow = enemy.GetComponent<EnemyFollow>();
                if (enemyFollow)
                {
                    enemyFollow.playerTarget = transform;
                }
            }

            elapsed += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }

        if (showDebug) Debug.Log("[FireGolem] Taunt mode ended - Resetting enemy targets!");

        Collider2D[] allEnemies = Physics2D.OverlapCircleAll(transform.position, aggroRange * 2f, enemyLayers);
        foreach (var enemy in allEnemies)
        {
            if (!enemy) continue;

            var enemyFollow = enemy.GetComponent<EnemyFollow>();
            if (enemyFollow && owner)
            {
                enemyFollow.playerTarget = owner;
            }
        }
    }

    public float GetHealthPercent()
    {
        return maxHP > 0f ? currentHP / maxHP : 0f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRange);

        if (comboStep == 3)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, aoeRadius);
        }
    }
}
