using UnityEngine;

[CreateAssetMenu(fileName = "FireElement", menuName = "Game/Elements/Fire")]
public class FireElementSO : BaseElementSO
{
    public static bool IsFireDotTickInProgress { get; private set; }

    public static void BeginFireDotTick()
    {
        IsFireDotTickInProgress = true;
    }

    public static void EndFireDotTick()
    {
        IsFireDotTickInProgress = false;
    }

    [Header("Fire Passive - Missing Health Damage")]
    [Tooltip("Maximum bonus damage when the target is almost dead. 0.4 = up to +40% damage.")]
    public float maxMissingHealthBonus = 0.4f;

    [Tooltip("If true, the bonus scales smoothly with missing health.")]
    public bool scaleWithMissingHealth = true;

    [Tooltip("Minimum missing health needed before Fire bonus starts. 0 = always active, 0.35 = starts after target lost 35% HP.")]
    [Range(0f, 1f)] public float missingHealthStartThreshold = 0f;

    [Header("Fire Passive - Burn On Hit")]
    [Tooltip("Extra DoT damage based on the final direct hit damage. 0.33 means a 15 damage hit adds about 5 burn damage.")]
    public float burnDamagePercentOfHit = 0.33f;

    [Tooltip("Minimum total burn damage added by Fire.")]
    public float minimumBurnDamage = 0f;

    [Tooltip("How long the burn lasts.")]
    public float burnDuration = 2f;

    [Tooltip("How often the burn deals damage.")]
    public float burnTickInterval = 0.25f;

    [Tooltip("If true, new burns refresh and add to existing burn damage.")]
    public bool stackBurnDamage = true;

    [Header("Debug")]
    public bool showDebug = false;

    public override float ModifyDirectDamage(GameObject attacker, GameObject target, float damage)
    {
        if (damage <= 0f)
            return damage;

        // Prevent Fire DoT ticks from creating another Fire DoT.
        if (IsFireDotTickInProgress)
            return damage;

        if (target == null)
            return damage;

        float finalDamage = ApplyMissingHealthBonus(target, damage);

        ApplyBurn(attacker, target, finalDamage);

        if (showDebug)
        {
            Debug.Log(
                $"[FireElementSO] Direct damage: {damage:F1} -> {finalDamage:F1}. " +
                $"Burn added from hit: {finalDamage * burnDamagePercentOfHit:F1}"
            );
        }

        return finalDamage;
    }

    private float ApplyMissingHealthBonus(GameObject target, float damage)
    {
        float healthPercent = GetTargetHealthPercent(target);
        float missingHealthPercent = 1f - healthPercent;

        if (missingHealthPercent < missingHealthStartThreshold)
            return damage;

        float bonusMultiplier;

        if (scaleWithMissingHealth)
        {
            float usableMissingHealth = Mathf.InverseLerp(
                missingHealthStartThreshold,
                1f,
                missingHealthPercent
            );

            bonusMultiplier = 1f + Mathf.Max(0f, maxMissingHealthBonus) * usableMissingHealth;
        }
        else
        {
            bonusMultiplier = 1f + Mathf.Max(0f, maxMissingHealthBonus);
        }

        return damage * bonusMultiplier;
    }

    private void ApplyBurn(GameObject attacker, GameObject target, float finalDirectDamage)
    {
        if (target == null)
            return;

        float burnDamage = finalDirectDamage * Mathf.Max(0f, burnDamagePercentOfHit);
        burnDamage = Mathf.Max(burnDamage, minimumBurnDamage);

        if (burnDamage <= 0f)
            return;

        FireElementDot dot = target.GetComponent<FireElementDot>();
        if (!dot)
            dot = target.AddComponent<FireElementDot>();

        dot.ApplyBurn(
            attacker,
            burnDamage,
            burnDuration,
            burnTickInterval,
            stackBurnDamage,
            showDebug
        );
    }

    private float GetTargetHealthPercent(GameObject target)
    {
        if (target == null)
            return 1f;

        EnemyHealth enemy = target.GetComponent<EnemyHealth>();
        if (enemy == null)
            enemy = target.GetComponentInParent<EnemyHealth>();

        if (enemy != null)
            return enemy.GetHealthPercent();

        PlayerHealth player = target.GetComponent<PlayerHealth>();
        if (player == null)
            player = target.GetComponentInParent<PlayerHealth>();

        if (player != null)
            return player.GetHealthPercent();

        return 1f;
    }
}