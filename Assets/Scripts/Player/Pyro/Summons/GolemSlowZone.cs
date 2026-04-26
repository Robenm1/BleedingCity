using UnityEngine;
using System.Collections.Generic;

public class GolemSlowZone : MonoBehaviour
{
    public float duration = 5f;
    public float radius = 5f;
    public float slowPercent = 0.5f;
    public LayerMask enemyLayers;

    [Header("Visuals")]
    public SpriteRenderer zoneSprite;
    public Color startColor = new Color(1f, 0.5f, 0f, 0.5f);
    public Color endColor = new Color(1f, 0.5f, 0f, 0f);

    private float timer;
    private HashSet<EnemyFollow> affectedEnemies = new HashSet<EnemyFollow>();

    private void Start()
    {
        timer = duration;

        if (zoneSprite)
        {
            zoneSprite.color = startColor;
            float scale = radius * 2f;
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    private void Update()
    {
        timer -= Time.deltaTime;

        if (zoneSprite)
        {
            float normalizedTime = 1f - (timer / duration);
            zoneSprite.color = Color.Lerp(startColor, endColor, normalizedTime);
        }

        ApplySlowToEnemies();

        if (timer <= 0f)
        {
            RemoveSlowFromAll();
            Destroy(gameObject);
        }
    }

    private void ApplySlowToEnemies()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, enemyLayers);

        HashSet<EnemyFollow> currentEnemies = new HashSet<EnemyFollow>();

        foreach (var hit in hits)
        {
            if (!hit) continue;

            var enemyFollow = hit.GetComponent<EnemyFollow>();
            if (enemyFollow)
            {
                currentEnemies.Add(enemyFollow);

                if (!affectedEnemies.Contains(enemyFollow))
                {
                    enemyFollow.ApplySlow(slowPercent);
                    affectedEnemies.Add(enemyFollow);
                }
            }
        }

        List<EnemyFollow> toRemove = new List<EnemyFollow>();
        foreach (var enemy in affectedEnemies)
        {
            if (!currentEnemies.Contains(enemy))
            {
                toRemove.Add(enemy);
            }
        }

        foreach (var enemy in toRemove)
        {
            if (enemy) enemy.RemoveSlow(slowPercent);
            affectedEnemies.Remove(enemy);
        }
    }

    private void RemoveSlowFromAll()
    {
        foreach (var enemy in affectedEnemies)
        {
            if (enemy) enemy.RemoveSlow(slowPercent);
        }
        affectedEnemies.Clear();
    }

    private void OnDestroy()
    {
        RemoveSlowFromAll();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, radius);
    }
}
