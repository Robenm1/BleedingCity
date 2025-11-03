using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(BuffManager))]
public class PlayerBuffInventory : MonoBehaviour
{
    [Tooltip("All buffs the player has taken this run (for UI/history).")]
    public List<BuffData> ownedBuffs = new List<BuffData>();

    [Header("Duplicate Rules")]
    [Tooltip("Prevent picking the same Epic buff more than once.")]
    public bool blockDuplicateEpics = true;

    [Tooltip("Prevent picking the same Legendary buff more than once.")]
    public bool blockDuplicateLegendaries = true;

    private BuffManager buffManager;

    private void Awake()
    {
        buffManager = GetComponent<BuffManager>();
    }

    public bool CanTake(BuffData buff)
    {
        if (buff == null) return false;

        if (blockDuplicateLegendaries && buff.rarity == Rarity.Legendary && buffManager.HasBuff(buff))
            return false;

        if (blockDuplicateEpics && buff.rarity == Rarity.Epic && buffManager.HasBuff(buff))
            return false;

        return true;
    }

    public void AddBuff(BuffData buff)
    {
        if (buff == null) return;

        if (!CanTake(buff))
        {
            Debug.Log($"[PlayerBuffInventory] Blocked duplicate pick: {buff.buffName} ({buff.rarity})");
            return;
        }

        ownedBuffs.Add(buff);
        buffManager.ApplyBuff(buff, 1);

        Debug.Log($"[PlayerBuffInventory] Added buff: {buff.buffName} ({buff.rarity}). Stacks now: {buffManager.GetStacks(buff)}");
    }

    // Optional helpers
    public bool HasBuff(BuffData buff) => buffManager.HasBuff(buff);
    public int GetStacks(BuffData buff) => buffManager.GetStacks(buff);

    public void RemoveBuff(BuffData buff)
    {
        if (buff == null) return;
        ownedBuffs.Remove(buff); // removes one entry from history list
        buffManager.RemoveBuff(buff);
    }
}
