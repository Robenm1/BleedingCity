using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Continuously damages all enemies inside its trigger collider.
/// Attach to a child GameObject on the FireDragon prefab.
/// </summary>
public class FireDragonBreath : MonoBehaviour
{
    [Tooltip("Damage dealt per second to each enemy inside the breath zone.")]
    public float damagePerSecond = 80f;

    public LayerMask enemyLayers;

    private readonly HashSet<EnemyHealth> enemiesInBreath = new HashSet<EnemyHealth>();

    private void Update()
    {
        if (enemiesInBreath.Count == 0) return;

        float damageThisFrame = damagePerSecond * Time.deltaTime;

        foreach (var enemy in enemiesInBreath)
        {
            if (enemy != null)
            {
                enemy.TakeSummonDamage(damageThisFrame);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & enemyLayers) == 0) return;

        var health = other.GetComponent<EnemyHealth>();
        if (health != null)
        {
            enemiesInBreath.Add(health);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var health = other.GetComponent<EnemyHealth>();
        if (health != null)
        {
            enemiesInBreath.Remove(health);
        }
    }
}
