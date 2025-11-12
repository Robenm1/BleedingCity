using UnityEngine;

[CreateAssetMenu(fileName = "BurningStormEffect", menuName = "Game/Cards/Effects/Burning Storm")]
public class BurningStormEffectSO : CardEffectSO
{
    [Header("Burning DoT")]
    public float damagePerSecond = 8f;
    public float tickInterval = 0.25f;
    public bool requireSameOwner = true;

    [Header("Ability1 Cooldown Penalty (SandRepulseAbility)")]
    [Tooltip("Must match the class name of your Ability 1 component.")]
    public string ability1ComponentName = "SandRepulseAbility";

    [Tooltip("Must match the float field/property that stores the base cooldown.")]
    public string cooldownMemberName = "baseCooldown";

    [Tooltip("Multiply Ability1's cooldown by this (e.g., 1.25 = +25%).")]
    public float cooldownMultiplier = 1.15f;

    public override void Apply(GameObject player)
    {
        // Enable DoT through EnemyMark (centralized logic).
        EnemyMark.EnableBurningStormFor(
            player.transform,
            Mathf.Max(0f, damagePerSecond),
            Mathf.Max(0.05f, tickInterval),
            requireSameOwner
        );

        // Apply a reversible multiplier to SandRepulseAbility.baseCooldown.
        var applier = player.GetComponent<AbilityCooldownMultiplierApplier>();
        if (!applier) applier = player.AddComponent<AbilityCooldownMultiplierApplier>();

        applier.ApplyFor(
            targetComponentName: ability1ComponentName, // "SandRepulseAbility"
            cooldownMemberName: cooldownMemberName,     // "baseCooldown"
            multiplier: Mathf.Max(0.01f, cooldownMultiplier)
        );
    }
}
