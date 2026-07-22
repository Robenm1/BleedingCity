using UnityEngine;

[CreateAssetMenu(fileName = "NatureElement", menuName = "Game/Elements/Nature")]
public class NatureElementSO : BaseElementSO
{
    [Header("Nature Passive - Healing Increase")]
    [Tooltip("Increases all healing received. 0.3 = 30% more healing.")]
    [Range(0f, 2f)]
    public float healingIncreasePercent = 0.3f;

    [Header("Nature Passive - Healing Growth")]
    [Tooltip("How much of healing received becomes bonus damage for the next attack. 0.3 = 30%.")]
    [Range(0f, 2f)]
    public float healingToDamagePercent = 0.3f;

    [Tooltip("Maximum stored bonus damage based on holder max HP. 0.5 = max stored bonus is 50% max HP.")]
    [Range(0f, 5f)]
    public float maxStoredBonusPercentOfMaxHealth = 0.5f;

    [Tooltip("If true, the stored bonus is consumed on the next direct attack.")]
    public bool consumeBonusOnAttack = true;

    [Header("Debug")]
    public bool showDebug = false;

    public override float ModifyHealingReceived(GameObject holder, GameObject healer, float healing)
    {
        if (holder == null || healing <= 0f)
            return healing;

        float finalHealing = healing * (1f + healingIncreasePercent);

        NatureHealingGrowth growth = holder.GetComponent<NatureHealingGrowth>();
        if (!growth)
            growth = holder.AddComponent<NatureHealingGrowth>();

        float bonusToStore = finalHealing * Mathf.Max(0f, healingToDamagePercent);
        float maxStoredBonus = GetHolderMaxHealth(holder) * Mathf.Max(0f, maxStoredBonusPercentOfMaxHealth);

        growth.AddBonusDamage(
            bonusToStore,
            maxStoredBonus,
            showDebug
        );

        if (showDebug)
        {
            Debug.Log(
                $"[NatureElementSO] Healing modified. " +
                $"Base healing: {healing:F1}, Final healing: {finalHealing:F1}, " +
                $"Stored bonus: {bonusToStore:F1}"
            );
        }

        return finalHealing;
    }

    public override float ModifyDirectDamage(GameObject attacker, GameObject target, float damage)
    {
        if (attacker == null || damage <= 0f)
            return damage;

        NatureHealingGrowth growth = attacker.GetComponent<NatureHealingGrowth>();
        if (!growth)
            growth = attacker.GetComponentInParent<NatureHealingGrowth>();

        if (!growth)
            return damage;

        float bonusDamage = consumeBonusOnAttack
            ? growth.ConsumeBonusDamage()
            : growth.GetStoredBonusDamage();

        if (bonusDamage <= 0f)
            return damage;

        float finalDamage = damage + bonusDamage;

        if (showDebug)
        {
            Debug.Log(
                $"[NatureElementSO] Damage boosted. " +
                $"Base: {damage:F1}, Bonus: {bonusDamage:F1}, Final: {finalDamage:F1}"
            );
        }

        return finalDamage;
    }

    private float GetHolderMaxHealth(GameObject holder)
    {
        if (holder == null)
            return 0f;

        PlayerStats stats = holder.GetComponent<PlayerStats>();
        if (stats == null)
            stats = holder.GetComponentInParent<PlayerStats>();

        if (stats != null)
            return Mathf.Max(1f, stats.maxHealth);

        EnemyHealth enemy = holder.GetComponent<EnemyHealth>();
        if (enemy == null)
            enemy = holder.GetComponentInParent<EnemyHealth>();

        if (enemy != null)
            return Mathf.Max(1f, enemy.maxHP);

        return 1f;
    }
}