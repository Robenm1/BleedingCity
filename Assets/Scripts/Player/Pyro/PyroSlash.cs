using UnityEngine;
using System.Collections.Generic;

public class PyroSlash : MonoBehaviour
{
    private float damage;
    private float arcAngle;
    private float range;
    private LayerMask enemyLayers;
    private Vector3 sourcePosition;
    private Vector2 direction;
    private HashSet<EnemyHealth> hitEnemies = new HashSet<EnemyHealth>();
    private bool hasDealtDamage = false;

    public void Initialize(float damage, float arcAngle, float range, LayerMask enemyLayers, Vector3 sourcePosition, Vector2 direction)
    {
        this.damage = damage;
        this.arcAngle = arcAngle;
        this.range = range;
        this.enemyLayers = enemyLayers;
        this.sourcePosition = sourcePosition;
        this.direction = direction.normalized;
    }

    private void Start()
    {
        DealDamageInArc();

        Animator animator = GetComponent<Animator>();
        if (animator)
        {
            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
            if (clips.Length > 0)
            {
                float animLength = clips[0].length;
                Destroy(gameObject, animLength);
            }
            else
            {
                Destroy(gameObject, 0.5f);
            }
        }
        else
        {
            Destroy(gameObject, 0.5f);
        }
    }

    private void DealDamageInArc()
    {
        if (hasDealtDamage) return;
        hasDealtDamage = true;

        Collider2D[] hits = Physics2D.OverlapCircleAll(sourcePosition, range, enemyLayers);

        foreach (var hit in hits)
        {
            if (!hit) continue;

            Vector2 toEnemy = (hit.transform.position - sourcePosition).normalized;
            float angleToEnemy = Vector2.Angle(direction, toEnemy);

            if (angleToEnemy <= arcAngle / 2f)
            {
                var enemyHealth = hit.GetComponent<EnemyHealth>();
                if (enemyHealth != null && !hitEnemies.Contains(enemyHealth))
                {
                    enemyHealth.TakeDamage(damage);
                    hitEnemies.Add(enemyHealth);
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!hasDealtDamage) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(sourcePosition, range);

        Vector3 leftBound = Quaternion.Euler(0, 0, arcAngle / 2f) * direction * range;
        Vector3 rightBound = Quaternion.Euler(0, 0, -arcAngle / 2f) * direction * range;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(sourcePosition, sourcePosition + leftBound);
        Gizmos.DrawLine(sourcePosition, sourcePosition + rightBound);
    }
}
