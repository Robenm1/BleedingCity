using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class GolemCollisionDamageReceiver : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The FireGolem script that will receive the damage.")]
    public FireGolem golem;

    [Header("Damage Settings")]
    [Tooltip("Multiplier for incoming contact damage. 1.0 = full damage, 0.5 = half damage.")]
    [Range(0.1f, 2f)] public float damageMultiplier = 1f;

    [Header("Per-Enemy Cooldown")]
    [Tooltip("Time between damage ticks from each individual enemy.")]
    public float damageTickInterval = 0.5f;

    [Tooltip("Minimum contact time before first damage tick.")]
    public float minimumContactTime = 0.1f;

    private System.Collections.Generic.Dictionary<int, float> enemyContactTimers = new System.Collections.Generic.Dictionary<int, float>();
    private System.Collections.Generic.Dictionary<int, float> enemyNextDamageTimes = new System.Collections.Generic.Dictionary<int, float>();

    private void Awake()
    {
        if (golem == null)
        {
            golem = GetComponent<FireGolem>();
        }

        if (golem == null)
        {
            golem = GetComponentInParent<FireGolem>();
        }

        if (golem == null)
        {
            Debug.LogError($"[GolemCollisionDamageReceiver] No FireGolem found on {name}! This component won't work.");
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (golem == null) return;

        GameObject enemyObj = collision.gameObject;
        if (!enemyObj.CompareTag("Enemy")) return;

        int enemyID = enemyObj.GetInstanceID();

        if (!enemyContactTimers.ContainsKey(enemyID))
        {
            enemyContactTimers[enemyID] = 0f;
            enemyNextDamageTimes[enemyID] = 0f;
        }

        enemyContactTimers[enemyID] += Time.deltaTime;

        if (enemyContactTimers[enemyID] < minimumContactTime) return;

        if (Time.time < enemyNextDamageTimes[enemyID]) return;

        var enemyFollow = enemyObj.GetComponent<EnemyFollow>();
        if (enemyFollow != null)
        {
            float damage = enemyFollow.contactDamage * damageMultiplier;
            golem.TakeDamage(damage);
            enemyNextDamageTimes[enemyID] = Time.time + damageTickInterval;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        GameObject enemyObj = collision.gameObject;
        if (!enemyObj.CompareTag("Enemy")) return;

        int enemyID = enemyObj.GetInstanceID();
        if (enemyContactTimers.ContainsKey(enemyID))
        {
            enemyContactTimers.Remove(enemyID);
        }
        if (enemyNextDamageTimes.ContainsKey(enemyID))
        {
            enemyNextDamageTimes.Remove(enemyID);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (golem == null) return;

        GameObject enemyObj = other.gameObject;
        if (!enemyObj.CompareTag("Enemy")) return;

        int enemyID = enemyObj.GetInstanceID();

        if (!enemyContactTimers.ContainsKey(enemyID))
        {
            enemyContactTimers[enemyID] = 0f;
            enemyNextDamageTimes[enemyID] = 0f;
        }

        enemyContactTimers[enemyID] += Time.deltaTime;

        if (enemyContactTimers[enemyID] < minimumContactTime) return;

        if (Time.time < enemyNextDamageTimes[enemyID]) return;

        var enemyFollow = enemyObj.GetComponent<EnemyFollow>();
        if (enemyFollow != null)
        {
            float damage = enemyFollow.contactDamage * damageMultiplier;
            golem.TakeDamage(damage);
            enemyNextDamageTimes[enemyID] = Time.time + damageTickInterval;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        GameObject enemyObj = other.gameObject;
        if (!enemyObj.CompareTag("Enemy")) return;

        int enemyID = enemyObj.GetInstanceID();
        if (enemyContactTimers.ContainsKey(enemyID))
        {
            enemyContactTimers.Remove(enemyID);
        }
        if (enemyNextDamageTimes.ContainsKey(enemyID))
        {
            enemyNextDamageTimes.Remove(enemyID);
        }
    }
}
