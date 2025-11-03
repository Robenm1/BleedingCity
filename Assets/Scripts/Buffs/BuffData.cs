using UnityEngine;
using System;

[CreateAssetMenu(fileName = "NewBuff", menuName = "Game/Player Buff", order = 0)]
public class BuffData : ScriptableObject
{
    [Header("Info")]
    public string buffId;
    public string buffName;
    [TextArea(2, 4)] public string description;

    [Header("Visuals")]
    public Sprite icon;

    [Header("Meta")]
    public Rarity rarity = Rarity.Common;

    // ===== EFFECT SETTINGS =====
    [Header("Timing & Stacking")]
    public bool hasDuration = false;
    public float duration = 5f;
    public bool stackable = true;
    [Min(1)] public int maxStacks = 5;
    public bool removeOnDeath = true;

    [Header("Stat Modifiers")]
    public Modifier[] modifiers;

    [Serializable]
    public struct Modifier
    {
        public Stat stat;
        public Op op;
        public float value;
        /*
         * Op:
         *  Flat        => base + value
         *  PercentAdd  => base * (1 + value)      (0.20 => +20%)
         *  PercentMult => base * value            (1.20 => +20%)
         *
         * Heal (special, one-time on apply):
         *  Flat        => heal 'value' HP
         *  PercentAdd  => heal value * MaxHP
         */
    }

    public enum Stat
    {
        // One-time / external
        Heal,

        // PlayerStats-backed
        MoveSpeed,
        BaseDamage,
        AttackDelay,              // smaller = faster
        PickupRange,
        MaxHealth,
        Armor,
        DamageReductionPercent,   // 0..1
        CooldownMultiplier,       // smaller = faster
        SpeedMultiplier,
        DashSpeed,
        DashCooldown,
        AttackRange,
        ProjectileSpeed           // NEW: affects PlayerStats.projectileSpeed
    }

    public enum Op { Flat, PercentAdd, PercentMult }
}

public enum Rarity { Common, Rare, Epic, Legendary }
