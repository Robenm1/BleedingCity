using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(PlayerStats))]
public class BuffManager : MonoBehaviour
{
    private PlayerStats stats;

    // Snapshot of original stats so values never drift
    private Baseline baseLine;

    // Active buffs (runtime)
    private readonly List<ActiveBuff> active = new();

    // Max HP policy (how current HP reacts when max changes)
    public enum MaxHpBuffPolicy { KeepCurrent, KeepPercent, HealByDelta, FillToMax }

    [Header("Max HP Change Policy")]
    [Tooltip("Default KeepCurrent: raising MaxHP does NOT heal. Use Heal buff to restore HP.")]
    public MaxHpBuffPolicy maxHpPolicy = MaxHpBuffPolicy.KeepCurrent;

    // Optional events for UI
    public System.Action<BuffData> OnBuffApplied;
    public System.Action<BuffData> OnBuffRemoved;
    public System.Action<BuffData, int> OnBuffStackChanged;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
    }

    private void Start()
    {
        baseLine = new Baseline(stats);
        stats.currentHealth = Mathf.Clamp(stats.currentHealth, 0f, stats.maxHealth);
        Recompute(); // write baseline once
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        bool changed = false;

        for (int i = active.Count - 1; i >= 0; i--)
        {
            var ab = active[i];
            if (ab.data.hasDuration)
            {
                ab.timeLeft -= dt;
                if (ab.timeLeft <= 0f)
                {
                    if (ab.data.stackable && ab.stacks > 1)
                    {
                        ab.stacks--;
                        ab.timeLeft = ab.data.duration;
                        OnBuffStackChanged?.Invoke(ab.data, ab.stacks);
                        active[i] = ab;
                    }
                    else
                    {
                        active.RemoveAt(i);
                        OnBuffRemoved?.Invoke(ab.data);
                    }
                    changed = true;
                }
                else
                {
                    active[i] = ab;
                }
            }
        }

        if (changed) Recompute();
    }

    // ===== Public API =====

    public void ApplyBuff(BuffData data, int stacks = 1)
    {
        if (data == null || stacks <= 0) return;

        // 1) One-time HEAL processing (before adding, so non-duration buffs don't double-apply)
        float totalHeal = 0f;
        if (data.modifiers != null)
        {
            foreach (var m in data.modifiers)
            {
                if (m.stat != BuffData.Stat.Heal) continue;

                float healVal = 0f;
                switch (m.op)
                {
                    case BuffData.Op.Flat:
                        healVal = m.value * stacks;
                        break;
                    case BuffData.Op.PercentAdd:
                        healVal = stats.maxHealth * (m.value * stacks);
                        break;
                    case BuffData.Op.PercentMult:
                        // ambiguous for heal; ignore for safety
                        break;
                }
                totalHeal += Mathf.Max(0f, healVal);
            }
        }
        if (totalHeal > 0f)
            stats.currentHealth = Mathf.Min(stats.currentHealth + totalHeal, stats.maxHealth);

        // 2) Add/stack to active list
        int idx = active.FindIndex(b => b.data == data);
        if (idx >= 0)
        {
            var ab = active[idx];
            if (data.stackable)
            {
                ab.stacks = Mathf.Min(data.maxStacks, ab.stacks + stacks);
                ab.timeLeft = data.hasDuration ? data.duration : float.PositiveInfinity;
                active[idx] = ab;
                OnBuffStackChanged?.Invoke(data, ab.stacks);
            }
            else
            {
                ab.timeLeft = data.hasDuration ? data.duration : float.PositiveInfinity; // refresh
                active[idx] = ab;
            }
        }
        else
        {
            var ab = new ActiveBuff
            {
                data = data,
                stacks = Mathf.Max(1, stacks),
                timeLeft = data.hasDuration ? data.duration : float.PositiveInfinity
            };
            active.Add(ab);
            OnBuffApplied?.Invoke(data);
        }

        Recompute();
    }

    public void RemoveBuff(BuffData data)
    {
        int idx = active.FindIndex(b => b.data == data);
        if (idx >= 0)
        {
            var ab = active[idx];
            active.RemoveAt(idx);
            OnBuffRemoved?.Invoke(ab.data);
            Recompute();
        }
    }

    public bool HasBuff(BuffData data) => active.Exists(b => b.data == data);
    public int GetStacks(BuffData data)
    {
        int idx = active.FindIndex(b => b.data == data);
        return (idx >= 0) ? active[idx].stacks : 0;
    }

    public void ClearAllBuffs(bool keepPersistent = false)
    {
        if (keepPersistent)
            active.RemoveAll(b => b.data.removeOnDeath);
        else
            active.Clear();

        Recompute();
    }

    // ===== Core recompute =====

    private void Recompute()
    {
        // Accumulators: Flat (0), PercentAdd (1+v), PercentMult (v)
        var flat = new Acc();
        var pAdd = new Acc(1f);
        var pMul = new Acc(1f);

        foreach (var ab in active)
        {
            int s = Mathf.Max(1, ab.stacks);
            var mods = ab.data.modifiers;
            if (mods == null) continue;

            for (int i = 0; i < mods.Length; i++)
            {
                var m = mods[i];

                // Heal handled on apply only
                if (m.stat == BuffData.Stat.Heal) continue;

                float v = m.value * s;
                switch (m.op)
                {
                    case BuffData.Op.Flat: flat.Add(m.stat, v); break;
                    case BuffData.Op.PercentAdd: pAdd.Add(m.stat, 1f + v); break;
                    case BuffData.Op.PercentMult: pMul.Add(m.stat, v); break;
                }
            }
        }

        float Comb(BuffData.Stat st, float baseVal)
            => ((baseVal + flat.Get(st)) * pAdd.Get(st)) * pMul.Get(st);

        // Write back to PlayerStats (no edits elsewhere)
        stats.moveSpeed = Comb(BuffData.Stat.MoveSpeed, baseLine.moveSpeed);
        stats.baseDamage = Comb(BuffData.Stat.BaseDamage, baseLine.baseDamage);
        stats.attackDelay = Mathf.Max(0.01f, Comb(BuffData.Stat.AttackDelay, baseLine.attackDelay));
        stats.pickupRange = Comb(BuffData.Stat.PickupRange, baseLine.pickupRange);
        stats.armor = Comb(BuffData.Stat.Armor, baseLine.armor);
        stats.damageReductionPercent = Mathf.Clamp01(Comb(BuffData.Stat.DamageReductionPercent, baseLine.damageReductionPercent));
        stats.cooldownMultiplier = Comb(BuffData.Stat.CooldownMultiplier, baseLine.cooldownMultiplier);
        stats.speedMultiplier = Comb(BuffData.Stat.SpeedMultiplier, baseLine.speedMultiplier);
        stats.dashSpeed = Comb(BuffData.Stat.DashSpeed, baseLine.dashSpeed);
        stats.dashCooldown = Mathf.Max(0f, Comb(BuffData.Stat.DashCooldown, baseLine.dashCooldown));
        stats.attackRange = Comb(BuffData.Stat.AttackRange, baseLine.attackRange);
        stats.projectileSpeed = Comb(BuffData.Stat.ProjectileSpeed, baseLine.projectileSpeed);

        // MaxHealth — apply chosen policy
        float oldMax = stats.maxHealth;
        float oldCur = stats.currentHealth;
        float newMax = Mathf.Max(1f, Comb(BuffData.Stat.MaxHealth, baseLine.maxHealth));

        if (!Mathf.Approximately(oldMax, newMax))
        {
            switch (maxHpPolicy)
            {
                case MaxHpBuffPolicy.KeepCurrent:
                    stats.maxHealth = newMax;
                    stats.currentHealth = Mathf.Min(oldCur, newMax);
                    break;

                case MaxHpBuffPolicy.KeepPercent:
                    {
                        float pct = oldMax > 0f ? oldCur / oldMax : 1f;
                        stats.maxHealth = newMax;
                        stats.currentHealth = Mathf.Clamp(pct * newMax, 0f, newMax);
                        break;
                    }

                case MaxHpBuffPolicy.HealByDelta:
                    {
                        float delta = newMax - oldMax;
                        stats.maxHealth = newMax;
                        stats.currentHealth = Mathf.Clamp(oldCur + Mathf.Max(0f, delta), 0f, newMax);
                        break;
                    }

                case MaxHpBuffPolicy.FillToMax:
                    stats.maxHealth = newMax;
                    stats.currentHealth = newMax;
                    break;
            }
        }
    }

    // ===== Data holders =====

    private struct Baseline
    {
        public float moveSpeed, baseDamage, attackDelay, pickupRange, maxHealth,
                     armor, damageReductionPercent, cooldownMultiplier, speedMultiplier,
                     dashSpeed, /*dashDuration,*/ dashCooldown,
                     attackRange, projectileSpeed;

        public Baseline(PlayerStats s)
        {
            moveSpeed = s.moveSpeed;
            baseDamage = s.baseDamage;
            attackDelay = s.attackDelay;
            pickupRange = s.pickupRange;
            maxHealth = s.maxHealth;
            armor = s.armor;
            damageReductionPercent = s.damageReductionPercent;
            cooldownMultiplier = s.cooldownMultiplier;
            speedMultiplier = s.speedMultiplier;
            dashSpeed = s.dashSpeed;
            // dashDuration intentionally not buffed
            dashCooldown = s.dashCooldown;
            attackRange = s.attackRange;
            projectileSpeed = s.projectileSpeed;
        }
    }

    private struct ActiveBuff
    {
        public BuffData data;
        public int stacks;
        public float timeLeft;
    }

    private class Acc
    {
        private readonly Dictionary<BuffData.Stat, float> map = new();
        private readonly float def;

        public Acc(float def = 0f) { this.def = def; }

        public void Add(BuffData.Stat s, float v)
        {
            if (map.TryGetValue(s, out float cur)) map[s] = cur + v;
            else map[s] = def + v;
        }

        public float Get(BuffData.Stat s)
        {
            if (map.TryGetValue(s, out float cur)) return cur;
            return def == 0f ? 0f : 1f; // for pAdd/pMul default=1
        }
    }
}
