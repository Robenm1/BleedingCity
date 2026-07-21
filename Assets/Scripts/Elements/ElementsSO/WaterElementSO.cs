using UnityEngine;

[CreateAssetMenu(fileName = "WaterElement", menuName = "Game/Elements/Water")]
public class WaterElementSO : BaseElementSO
{
    [Header("Water Passive - Max HP Damage Scaling")]
    [Tooltip("How much of the original damage stays as normal damage. 0.5 = 50% normal damage.")]
    [Range(0f, 1f)]
    public float normalDamagePortion = 0.5f;

    [Tooltip("How much of the holder max HP becomes damage. 0.05 = 5% max HP.")]
    public float maxHealthDamageScaling = 0.05f;

    [Tooltip("Minimum bonus damage from max HP scaling.")]
    public float minimumHpScaledDamage = 0f;

    [Header("Water Passive - Low HP Protection")]
    [Tooltip("Maximum damage reduction when holder is almost dead. 0.5 = up to 50% less damage.")]
    [Range(0f, 0.95f)]
    public float maxLowHealthDamageReduction = 0.5f;

    [Tooltip("Protection starts when health is at or below this percent. 0.5 = starts below 50% HP.")]
    [Range(0f, 1f)]
    public float protectionStartHealthPercent = 0.5f;

    [Tooltip("If true, protection scales gradually as HP gets lower.")]
    public bool scaleProtectionWithMissingHealth = true;

    [Header("Debug")]
    public bool showDebug = false;

    public override float ModifyDirectDamage(GameObject attacker, GameObject target, float damage)
    {
        if (damage <= 0f)
            return damage;

        float holderMaxHealth = GetHolderMaxHealth(attacker);

        if (holderMaxHealth <= 0f)
            return damage;

        float normalPart = damage * Mathf.Clamp01(normalDamagePortion);

        float hpScaledPart = holderMaxHealth * Mathf.Max(0f, maxHealthDamageScaling);
        hpScaledPart = Mathf.Max(hpScaledPart, minimumHpScaledDamage);

        float finalDamage = normalPart + hpScaledPart;

        if (showDebug)
        {
            Debug.Log(
                $"[WaterElementSO] Damage modified. " +
                $"Original: {damage:F1}, Normal part: {normalPart:F1}, " +
                $"HP scaled part: {hpScaledPart:F1}, Final: {finalDamage:F1}"
            );
        }

        return finalDamage;
    }

    public override float ModifyIncomingDirectDamage(GameObject holder, GameObject attacker, float damage)
    {
        if (damage <= 0f)
            return damage;

        float healthPercent = GetHolderHealthPercent(holder);

        if (healthPercent > protectionStartHealthPercent)
            return damage;

        float reduction;

        if (scaleProtectionWithMissingHealth)
        {
            float lowHpPower = Mathf.InverseLerp(
                protectionStartHealthPercent,
                0f,
                healthPercent
            );

            reduction = maxLowHealthDamageReduction * lowHpPower;
        }
        else
        {
            reduction = maxLowHealthDamageReduction;
        }

        reduction = Mathf.Clamp(reduction, 0f, 0.95f);

        float finalDamage = damage * (1f - reduction);

        if (showDebug)
        {
            Debug.Log(
                $"[WaterElementSO] Incoming damage reduced. " +
                $"HP: {healthPercent:P0}, Reduction: {reduction:P0}, " +
                $"Damage: {damage:F1} -> {finalDamage:F1}"
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

        return 0f;
    }

    private float GetHolderHealthPercent(GameObject holder)
    {
        if (holder == null)
            return 1f;

        PlayerHealth player = holder.GetComponent<PlayerHealth>();
        if (player == null)
            player = holder.GetComponentInParent<PlayerHealth>();

        if (player != null)
            return player.GetHealthPercent();

        EnemyHealth enemy = holder.GetComponent<EnemyHealth>();
        if (enemy == null)
            enemy = holder.GetComponentInParent<EnemyHealth>();

        if (enemy != null)
            return enemy.GetHealthPercent();

        return 1f;
    }
}