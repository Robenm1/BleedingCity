using System;
using UnityEngine;

public static class BulletFromThePastHitReporter
{
    public static event Action<EnemyHealth> OnEnemyHit;

    public static void ReportHit(EnemyHealth enemy)
    {
        if (enemy == null)
            return;

        OnEnemyHit?.Invoke(enemy);
    }

    public static void ReportHit(GameObject enemyObject)
    {
        if (enemyObject == null)
            return;

        EnemyHealth enemy = enemyObject.GetComponent<EnemyHealth>();

        if (enemy == null)
            enemy = enemyObject.GetComponentInParent<EnemyHealth>();

        ReportHit(enemy);
    }

    public static void ReportHit(Collider2D enemyCollider)
    {
        if (enemyCollider == null)
            return;

        EnemyHealth enemy = enemyCollider.GetComponent<EnemyHealth>();

        if (enemy == null)
            enemy = enemyCollider.GetComponentInParent<EnemyHealth>();

        ReportHit(enemy);
    }
}