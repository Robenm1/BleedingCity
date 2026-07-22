using UnityEngine;

[CreateAssetMenu(fileName = "DarkElement", menuName = "Game/Elements/Dark")]
public class DarkElementSO : BaseElementSO
{
    [Header("Dark Passive - HP Cost")]
    [Tooltip("How much max HP is consumed per direct attack. 0.03 = 3% max HP.")]
    [Range(0f, 0.5f)]
    public float hpCostPercentOfMaxHealth = 0.03f;

    [Tooltip("Holder HP will never go below this percent. 0.01 = 1% HP.")]
    [Range(0.001f, 0.5f)]
    public float minimumHealthPercent = 0.01f;

    [Header("Dark Passive - Low HP Damage")]
    [Tooltip("Maximum bonus damage when holder is at minimum HP. 1 = +100% damage.")]
    [Range(0f, 5f)]
    public float maxDamageBonusAtLowHealth = 1f;

    [Tooltip("If true, damage bonus is calculated after HP is consumed.")]
    public bool calculateBonusAfterHpCost = true;

    [Header("Debug")]
    public bool showDebug = false;

    public override float ModifyDirectDamage(GameObject attacker, GameObject target, float damage)
    {
        if (attacker == null || damage <= 0f)
            return damage;

        PlayerHealth playerHealth = GetPlayerHealth(attacker);

        // For now Dark HP sacrifice is player-focused.
        // If the holder has no PlayerHealth, only return normal damage.
        if (playerHealth == null)
            return damage;

        float maxHealth = Mathf.Max(1f, playerHealth.GetMaxHealth());
        float currentHealthBefore = playerHealth.GetCurrentHealth();

        float minAllowedHealth = maxHealth * Mathf.Clamp(minimumHealthPercent, 0.001f, 0.5f);
        float hpCost = maxHealth * Mathf.Max(0f, hpCostPercentOfMaxHealth);

        if (!calculateBonusAfterHpCost)
        {
            damage = ApplyLowHealthDamageBonus(
                damage,
                currentHealthBefore,
                maxHealth,
                minAllowedHealth
            );
        }

        float healthAfterCost = Mathf.Max(minAllowedHealth, currentHealthBefore - hpCost);
        float actualHpConsumed = Mathf.Max(0f, currentHealthBefore - healthAfterCost);

        if (actualHpConsumed > 0f)
            playerHealth.SetHealth(healthAfterCost);

        if (calculateBonusAfterHpCost)
        {
            damage = ApplyLowHealthDamageBonus(
                damage,
                healthAfterCost,
                maxHealth,
                minAllowedHealth
            );
        }

        if (showDebug)
        {
            Debug.Log(
                $"[DarkElementSO] Dark attack. " +
                $"HP: {currentHealthBefore:F1} -> {healthAfterCost:F1}, " +
                $"Consumed: {actualHpConsumed:F1}, " +
                $"Final Damage: {damage:F1}"
            );
        }

        return damage;
    }

    private float ApplyLowHealthDamageBonus(
        float damage,
        float currentHealth,
        float maxHealth,
        float minAllowedHealth
    )
    {
        if (damage <= 0f)
            return damage;

        maxHealth = Mathf.Max(1f, maxHealth);
        minAllowedHealth = Mathf.Clamp(minAllowedHealth, 0.001f, maxHealth);

        float healthPercent = Mathf.Clamp01(currentHealth / maxHealth);
        float minHealthPercent = Mathf.Clamp01(minAllowedHealth / maxHealth);

        float lowHealthPower = Mathf.InverseLerp(
            1f,
            minHealthPercent,
            healthPercent
        );

        float bonusPercent = maxDamageBonusAtLowHealth * lowHealthPower;
        float multiplier = 1f + bonusPercent;

        return damage * multiplier;
    }

    private PlayerHealth GetPlayerHealth(GameObject holder)
    {
        if (holder == null)
            return null;

        PlayerHealth health = holder.GetComponent<PlayerHealth>();

        if (health == null)
            health = holder.GetComponentInParent<PlayerHealth>();

        if (health == null)
            health = holder.GetComponentInChildren<PlayerHealth>();

        return health;
    }
}