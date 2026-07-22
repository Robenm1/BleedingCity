using UnityEngine;

[DisallowMultipleComponent]
public class NatureHealingGrowth : MonoBehaviour
{
    [Header("Runtime")]
    [SerializeField] private float storedBonusDamage;

    public void AddBonusDamage(float amount, float maxStoredBonus, bool showDebug)
    {
        if (amount <= 0f)
            return;

        storedBonusDamage += amount;

        if (maxStoredBonus > 0f)
            storedBonusDamage = Mathf.Min(storedBonusDamage, maxStoredBonus);

        if (showDebug)
        {
            Debug.Log(
                $"[NatureHealingGrowth] {name} stored bonus damage: {storedBonusDamage:F1}"
            );
        }
    }

    public float GetStoredBonusDamage()
    {
        return storedBonusDamage;
    }

    public float ConsumeBonusDamage()
    {
        float value = storedBonusDamage;
        storedBonusDamage = 0f;
        return value;
    }

    public void ClearBonusDamage()
    {
        storedBonusDamage = 0f;
    }
}