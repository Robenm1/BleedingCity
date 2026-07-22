using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LightElement", menuName = "Game/Elements/Light")]
public class LightElementSO : BaseElementSO
{
    public static bool IsLightChainDamageInProgress { get; private set; }

    [Header("Light Passive - Chain Damage")]
    [Tooltip("How much of the direct hit damage is dealt to chained enemies. 0.5 = 50%.")]
    [Range(0f, 2f)]
    public float chainDamagePercent = 0.5f;

    [Tooltip("Radius used to find the next enemy from the current chain target.")]
    public float chainRadius = 4f;

    [Tooltip("Maximum jumps after the direct hit. 2 = Direct Enemy -> Enemy 2 -> Enemy 3.")]
    [Min(1)]
    public int maxChainJumps = 2;

    [Tooltip("Enemy layers checked for chain targets.")]
    public LayerMask enemyLayers = ~0;

    [Header("Visual")]
    [Tooltip("Prefab with LineRenderer + LightChainVisual.cs.")]
    public GameObject chainVisualPrefab;

    [Tooltip("Where the chain line starts from on each enemy.")]
    public Vector3 visualStartOffset = new Vector3(0f, 0.4f, 0f);

    [Tooltip("Where the chain line ends on each enemy.")]
    public Vector3 visualEndOffset = new Vector3(0f, 0.4f, 0f);

    [Tooltip("Color of the chain lightning line.")]
    public Color chainColor = new Color(1f, 0.95f, 0.3f, 1f);

    [Header("Rules")]
    [Tooltip("If true, chain damage uses TakeDamageDirectFromSource so it keeps resistance, popup colors, and source element rules.")]
    public bool useDirectDamageSource = true;

    [Tooltip("If true, the same enemy cannot be hit twice by the same Light chain.")]
    public bool preventDuplicateTargets = true;

    [Tooltip("Optional delay between jumps. 0 = instant. 0.03 feels nice if you later make it coroutine-based.")]
    public float jumpDelay = 0f;

    [Header("Debug")]
    public bool showDebug = false;

    public override float ModifyDirectDamage(GameObject attacker, GameObject target, float damage)
    {
        if (attacker == null || target == null || damage <= 0f)
            return damage;

        // Prevent chain damage from creating another chain.
        if (IsLightChainDamageInProgress)
            return damage;

        EnemyHealth directEnemy = GetEnemyHealth(target);

        if (directEnemy == null)
            return damage;

        ChainDamageSequential(attacker, directEnemy, damage);

        return damage;
    }

    private void ChainDamageSequential(GameObject attacker, EnemyHealth directEnemy, float directHitDamage)
    {
        float chainDamage = directHitDamage * Mathf.Max(0f, chainDamagePercent);

        if (chainDamage <= 0f)
            return;

        HashSet<EnemyHealth> usedTargets = new HashSet<EnemyHealth>();

        if (directEnemy != null)
            usedTargets.Add(directEnemy);

        EnemyHealth currentEnemy = directEnemy;
        int jumpsDone = 0;

        BeginLightChainDamage();

        try
        {
            while (jumpsDone < maxChainJumps)
            {
                EnemyHealth nextEnemy = FindNextChainTarget(currentEnemy, usedTargets);

                if (nextEnemy == null)
                    break;

                SpawnChainVisual(currentEnemy.transform, nextEnemy.transform);

                if (useDirectDamageSource)
                    nextEnemy.TakeDamageDirectFromSource(attacker, chainDamage);
                else
                    nextEnemy.TakeDamageDirect(chainDamage);

                usedTargets.Add(nextEnemy);

                jumpsDone++;

                if (showDebug)
                {
                    Debug.Log(
                        $"[LightElementSO] Chain jump {jumpsDone}/{maxChainJumps}. " +
                        $"{currentEnemy.name} -> {nextEnemy.name}. " +
                        $"Damage: {chainDamage:F1}"
                    );
                }

                // Important:
                // Next jump starts from the enemy that was just hit.
                currentEnemy = nextEnemy;
            }
        }
        finally
        {
            EndLightChainDamage();
        }

        if (showDebug)
        {
            Debug.Log(
                $"[LightElementSO] Direct target: {directEnemy.name}. " +
                $"Direct damage: {directHitDamage:F1}, Chain damage: {chainDamage:F1}, " +
                $"Jumps done: {jumpsDone}"
            );
        }
    }

    private EnemyHealth FindNextChainTarget(EnemyHealth fromEnemy, HashSet<EnemyHealth> usedTargets)
    {
        if (fromEnemy == null)
            return null;

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            fromEnemy.transform.position,
            Mathf.Max(0.1f, chainRadius),
            enemyLayers
        );

        if (hits == null || hits.Length == 0)
            return null;

        EnemyHealth bestEnemy = null;
        float bestDistSqr = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];

            if (hit == null)
                continue;

            EnemyHealth enemy = hit.GetComponent<EnemyHealth>();

            if (enemy == null)
                enemy = hit.GetComponentInParent<EnemyHealth>();

            if (enemy == null)
                continue;

            if (enemy.GetHealthPercent() <= 0f)
                continue;

            if (enemy.GetComponent<AbyssalDollObject>() != null)
                continue;

            // Prevent going back to direct target or any previous chain target.
            if (preventDuplicateTargets && usedTargets.Contains(enemy))
                continue;

            float distSqr = Vector2.SqrMagnitude(enemy.transform.position - fromEnemy.transform.position);

            if (distSqr < bestDistSqr)
            {
                bestDistSqr = distSqr;
                bestEnemy = enemy;
            }
        }

        return bestEnemy;
    }

    private void SpawnChainVisual(Transform startTarget, Transform endTarget)
    {
        if (chainVisualPrefab == null)
            return;

        if (startTarget == null || endTarget == null)
            return;

        Vector3 start = startTarget.position + visualStartOffset;
        Vector3 end = endTarget.position + visualEndOffset;

        GameObject obj = Instantiate(chainVisualPrefab, start, Quaternion.identity);

        LightChainVisual visual = obj.GetComponent<LightChainVisual>();

        if (visual != null)
        {
            visual.Play(start, end, chainColor);
        }
        else
        {
            Debug.LogWarning("[LightElementSO] Chain visual prefab is missing LightChainVisual.cs.");
            Destroy(obj, 0.2f);
        }
    }

    private EnemyHealth GetEnemyHealth(GameObject target)
    {
        if (target == null)
            return null;

        EnemyHealth enemy = target.GetComponent<EnemyHealth>();

        if (enemy == null)
            enemy = target.GetComponentInParent<EnemyHealth>();

        return enemy;
    }

    private static void BeginLightChainDamage()
    {
        IsLightChainDamageInProgress = true;
    }

    private static void EndLightChainDamage()
    {
        IsLightChainDamageInProgress = false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        chainRadius = Mathf.Max(0.1f, chainRadius);
        maxChainJumps = Mathf.Clamp(maxChainJumps, 1, 2);
        chainDamagePercent = Mathf.Max(0f, chainDamagePercent);
    }
#endif
}