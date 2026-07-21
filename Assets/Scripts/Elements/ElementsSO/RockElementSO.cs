using UnityEngine;

[CreateAssetMenu(fileName = "RockElement", menuName = "Game/Elements/Rock")]
public class RockElementSO : BaseElementSO
{
    [Header("Rock Passive - Max Health Damage")]
    [Tooltip("Extra damage based on target max HP. 0.05 = 5% of target max HP as bonus damage.")]
    public float targetMaxHealthDamagePercent = 0.05f;

    [Tooltip("Minimum bonus damage added by Rock.")]
    public float minimumMaxHealthDamage = 0f;

    [Header("Rock Passive - Stun")]
    [Tooltip("How many Rock hits are needed before the target gets stunned.")]
    public int hitsRequiredToStun = 3;

    [Tooltip("How long the stun lasts.")]
    public float stunDuration = 3f;

    [Tooltip("If true, hit counter resets after the target gets stunned.")]
    public bool resetHitsAfterStun = true;

    [Header("Debug")]
    public bool showDebug = false;

    public override float ModifyDirectDamage(GameObject attacker, GameObject target, float damage)
    {
        if (damage <= 0f)
            return damage;

        if (target == null)
            return damage;

        GameObject targetRoot = GetTargetRoot(target);
        if (targetRoot == null)
            return damage;

        float targetMaxHealth = GetTargetMaxHealth(targetRoot);

        float bonusDamage = targetMaxHealth * Mathf.Max(0f, targetMaxHealthDamagePercent);
        bonusDamage = Mathf.Max(bonusDamage, minimumMaxHealthDamage);

        float finalDamage = damage + bonusDamage;

        ApplyRockHit(targetRoot);

        if (showDebug)
        {
            Debug.Log(
                $"[RockElementSO] Hit {targetRoot.name}. " +
                $"Base: {damage:F1}, Target Max HP: {targetMaxHealth:F1}, " +
                $"Bonus: {bonusDamage:F1}, Final: {finalDamage:F1}"
            );
        }

        return finalDamage;
    }

    private void ApplyRockHit(GameObject targetRoot)
    {
        RockElementStatus status = targetRoot.GetComponent<RockElementStatus>();

        if (!status)
            status = targetRoot.AddComponent<RockElementStatus>();

        status.RegisterRockHit(
            hitsRequiredToStun,
            stunDuration,
            resetHitsAfterStun,
            showDebug
        );
    }

    private GameObject GetTargetRoot(GameObject target)
    {
        EnemyHealth enemy = target.GetComponent<EnemyHealth>();
        if (enemy == null)
            enemy = target.GetComponentInParent<EnemyHealth>();

        if (enemy != null)
            return enemy.gameObject;

        PlayerHealth player = target.GetComponent<PlayerHealth>();
        if (player == null)
            player = target.GetComponentInParent<PlayerHealth>();

        if (player != null)
            return player.gameObject;

        return target;
    }

    private float GetTargetMaxHealth(GameObject target)
    {
        if (target == null)
            return 0f;

        EnemyHealth enemy = target.GetComponent<EnemyHealth>();
        if (enemy == null)
            enemy = target.GetComponentInParent<EnemyHealth>();

        if (enemy != null)
            return Mathf.Max(1f, enemy.maxHP);

        PlayerStats stats = target.GetComponent<PlayerStats>();
        if (stats == null)
            stats = target.GetComponentInParent<PlayerStats>();

        if (stats != null)
            return Mathf.Max(1f, stats.maxHealth);

        return 0f;
    }
}