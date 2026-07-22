using UnityEngine;

[CreateAssetMenu(fileName = "IllusionElement", menuName = "Game/Elements/Illusion")]
public class IllusionElementSO : BaseElementSO
{
    [Header("Illusion Passive - Dash Cooldown")]
    [Tooltip("Dash cooldown reduction while this element is active. 0.2 = 20% less dash cooldown.")]
    [Range(0f, 0.9f)]
    public float dashCooldownReductionPercent = 0.2f;

    [Header("Illusion Passive - Dash Damage")]
    [Tooltip("Bonus damage per dash stack. 0.2 = +20% damage per dash.")]
    [Range(0f, 5f)]
    public float bonusDamagePercentPerDash = 0.2f;

    [Tooltip("Maximum dash stacks that can be stored.")]
    [Min(1)]
    public int maxDashStacks = 5;

    [Tooltip("If true, all dash stacks are consumed on the next direct attack.")]
    public bool consumeStacksOnAttack = true;

    [Header("Runtime Setup")]
    [Tooltip("Automatically adds the dash tracker when the element is applied.")]
    public bool autoAddTracker = true;

    [Header("Debug")]
    public bool showDebug = false;

    public override void OnElementApplied(GameObject owner)
    {
        if (owner == null)
            return;

        if (!autoAddTracker)
            return;

        IllusionDashDamage tracker = owner.GetComponent<IllusionDashDamage>();

        if (!tracker)
            tracker = owner.AddComponent<IllusionDashDamage>();

        tracker.Configure(
            dashCooldownReductionPercent,
            bonusDamagePercentPerDash,
            maxDashStacks,
            consumeStacksOnAttack,
            showDebug
        );
    }

    public override void OnElementRemoved(GameObject owner)
    {
        if (owner == null)
            return;

        IllusionDashDamage tracker = owner.GetComponent<IllusionDashDamage>();

        if (tracker != null)
        {
            tracker.ClearStacks();
            tracker.RemoveDashCooldownReduction();
        }
    }

    public override float ModifyDirectDamage(GameObject attacker, GameObject target, float damage)
    {
        if (attacker == null || damage <= 0f)
            return damage;

        IllusionDashDamage tracker = attacker.GetComponent<IllusionDashDamage>();

        if (!tracker)
            tracker = attacker.GetComponentInParent<IllusionDashDamage>();

        if (!tracker)
            return damage;

        float finalDamage = tracker.ApplyDashBonusToDamage(damage);

        if (showDebug && !Mathf.Approximately(finalDamage, damage))
        {
            Debug.Log(
                $"[IllusionElementSO] Damage boosted. " +
                $"Base: {damage:F1}, Final: {finalDamage:F1}"
            );
        }

        return finalDamage;
    }
}