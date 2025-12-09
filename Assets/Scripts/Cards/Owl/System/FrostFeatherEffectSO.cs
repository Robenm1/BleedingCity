using UnityEngine;

[CreateAssetMenu(fileName = "FrostFeatherEffect", menuName = "Game/Cards/Effects/Owl/Frost Feather")]
public class FrostFeatherEffectSO : CardEffectSO
{
    [Header("Chain Settings")]
    [Tooltip("Maximum number of bounces after the first enemy hit (3 = hits 4 enemies total).")]
    public int maxBounces = 3;

    [Tooltip("Damage multiplier per chain hit relative to PlayerStats.GetDamage(). 1 = base damage.")]
    public float chainDamageMultiplier = 1.0f;

    public override void Apply(GameObject player)
    {
        if (!player) return;
        var eff = player.GetComponent<FrostFeatherEffect>();
        if (!eff) eff = player.AddComponent<FrostFeatherEffect>();

        eff.maxBounces = Mathf.Max(0, maxBounces);
        eff.chainDamageMultiplier = Mathf.Max(0f, chainDamageMultiplier);
        eff.EnableEffect();
    }

    // (Optional) If you add removal later, call eff.DisableEffect() here.
    // public override void Remove(GameObject player) { ... }
}
