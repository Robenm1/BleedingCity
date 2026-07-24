using UnityEngine;

[DisallowMultipleComponent]
public class AbilityHUDData : MonoBehaviour
{
    [System.Serializable]
    public class AbilityHUDVariant
    {
        public string variantName;
        public Sprite icon;
        public float cooldown = 5f;
    }

    [Header("Ability 1 Variants")]
    [Tooltip("If only 1 variant exists, it stays the same. If multiple variants exist, the HUD can switch between them.")]
    public AbilityHUDVariant[] ability1Variants = new AbilityHUDVariant[1];

    [Header("Ability 2 Variants")]
    [Tooltip("If only 1 variant exists, it stays the same. If multiple variants exist, the HUD can switch between them.")]
    public AbilityHUDVariant[] ability2Variants = new AbilityHUDVariant[1];

    [Header("Starting Variant")]
    public int startingAbility1Variant = 0;
    public int startingAbility2Variant = 0;

    [Header("Auto Variant Change")]
    [Tooltip("If true, Ability 1 variant follows SummonEvolutionTracker.currentLevel when there are multiple variants.")]
    public bool ability1FollowsSummonLevel = true;

    [Tooltip("If true, Ability 2 variant follows SummonEvolutionTracker.currentLevel when there are multiple variants.")]
    public bool ability2FollowsSummonLevel = false;

    public AbilityHUDVariant GetAbility1Variant(int index)
    {
        return GetVariant(ability1Variants, index);
    }

    public AbilityHUDVariant GetAbility2Variant(int index)
    {
        return GetVariant(ability2Variants, index);
    }

    public int GetAbility1VariantCount()
    {
        return ability1Variants != null ? ability1Variants.Length : 0;
    }

    public int GetAbility2VariantCount()
    {
        return ability2Variants != null ? ability2Variants.Length : 0;
    }

    public bool Ability1HasMultipleVariants()
    {
        return GetAbility1VariantCount() > 1;
    }

    public bool Ability2HasMultipleVariants()
    {
        return GetAbility2VariantCount() > 1;
    }

    private AbilityHUDVariant GetVariant(AbilityHUDVariant[] variants, int index)
    {
        if (variants == null || variants.Length == 0)
            return null;

        int safeIndex = Mathf.Clamp(index, 0, variants.Length - 1);
        return variants[safeIndex];
    }
}