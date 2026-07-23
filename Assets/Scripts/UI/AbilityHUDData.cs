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
    [Tooltip("For normal characters, use only 1 element. For Pyro, add one per evolution stage.")]
    public AbilityHUDVariant[] ability1Variants = new AbilityHUDVariant[1];

    [Header("Ability 2 Variants")]
    [Tooltip("For normal characters, use only 1 element. For Pyro, add one per evolution stage if needed.")]
    public AbilityHUDVariant[] ability2Variants = new AbilityHUDVariant[1];

    [Header("Starting Variant")]
    public int startingAbility1Variant = 0;
    public int startingAbility2Variant = 0;

    public AbilityHUDVariant GetAbility1Variant(int index)
    {
        return GetVariant(ability1Variants, index);
    }

    public AbilityHUDVariant GetAbility2Variant(int index)
    {
        return GetVariant(ability2Variants, index);
    }

    private AbilityHUDVariant GetVariant(AbilityHUDVariant[] variants, int index)
    {
        if (variants == null || variants.Length == 0)
            return null;

        int safeIndex = Mathf.Clamp(index, 0, variants.Length - 1);
        return variants[safeIndex];
    }
}