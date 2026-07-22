using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(PlayerStats))]
public class PlayerHealth : MonoBehaviour
{
    public event Action<float, float> OnHealthChanged;
    public event Action<float> OnDamaged;
    public event Action<float> OnHealed;
    public event Action OnDeath;

    [Header("Invulnerability (i-frames)")]
    [SerializeField] private float invulnDuration = 0.25f;
    private float invulnTimer = 0f;

    [Header("When Max HP Changes (from buffs or edits)")]
    public MaxHpChangeMode maxHpChangeMode = MaxHpChangeMode.KeepPercent;

    [Tooltip("With KeepPercent, don't reduce HP due to rounding when max increases.")]
    public bool keepPercentFavorUp = true;

    [Header("Element Damage")]
    [Tooltip("If damage source is null, PlayerHealth will search nearby objects for an ElementHolder. This lets old enemy damage calls still use enemy elements.")]
    public bool useNearestElementSourceWhenNoSource = true;

    [Tooltip("Radius used to find the enemy/source ElementHolder when ApplyDamage is called without a source.")]
    public float fallbackElementSearchRadius = 3f;

    [Tooltip("Layers checked when searching for a fallback element source. Recommended: Enemy layer.")]
    public LayerMask fallbackElementSourceLayers = ~0;

    [Header("Debug")]
    [Tooltip("Prints healing info. Useful for testing Nature healing increase.")]
    public bool showHealingDebug = false;

    [Header("Optional UI")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI healthText;

    public enum MaxHpChangeMode
    {
        KeepPercent,
        HealByDelta,
        FillToMax,
        ClampOnly
    }

    private PlayerStats stats;
    private float _lastSeenMax;
    private float _lastSeenCur;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();

        if (stats == null)
        {
            Debug.LogError("[PlayerHealth] Missing PlayerStats.");
            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        _lastSeenMax = Mathf.Max(1f, stats.maxHealth);
        _lastSeenCur = Mathf.Clamp(stats.currentHealth, 0f, _lastSeenMax);

        stats.maxHealth = _lastSeenMax;
        stats.currentHealth = _lastSeenCur;

        SetupUI();
        Broadcast();
    }

    private void Update()
    {
        if (invulnTimer > 0f)
            invulnTimer -= Time.deltaTime;

        float seenMax = Mathf.Max(1f, stats.maxHealth);
        float seenCur = Mathf.Clamp(stats.currentHealth, 0f, seenMax);

        if (!Mathf.Approximately(seenMax, _lastSeenMax))
        {
            HandleExternalMaxChange(_lastSeenMax, seenMax);
            seenMax = stats.maxHealth;
            seenCur = stats.currentHealth;
        }

        if (!Mathf.Approximately(seenCur, _lastSeenCur))
        {
            stats.currentHealth = Mathf.Clamp(seenCur, 0f, stats.maxHealth);
            Broadcast();
        }

        _lastSeenMax = stats.maxHealth;
        _lastSeenCur = stats.currentHealth;
    }

    // =========================================================
    // Damage API
    // =========================================================

    public float ApplyDamage(float rawAmount)
    {
        return ApplyDamageFromSource(null, rawAmount);
    }

    public float ApplyDamageFromSource(GameObject damageSource, float rawAmount)
    {
        return ApplyDamageInternal(damageSource, rawAmount, false);
    }

    public float ApplyDamageDirectFromSource(GameObject damageSource, float rawAmount)
    {
        return ApplyDamageInternal(damageSource, rawAmount, true);
    }

    private float ApplyDamageInternal(GameObject damageSource, float rawAmount, bool ignoreNormalIFrames)
    {
        if (rawAmount <= 0f) return 0f;
        if (IsDead()) return 0f;

        if (!ignoreNormalIFrames && IsInvulnerable())
            return 0f;

        var pyroAbility = GetComponent<PyroAbility1>();
        if (pyroAbility != null && pyroAbility.IsInvulnerable())
            return 0f;

        if (GolemFireCircle.activeCircle != null && GolemFireCircle.activeCircle.currentShield > 0f)
        {
            rawAmount = GolemFireCircle.activeCircle.AbsorbDamage(rawAmount);
            if (rawAmount <= 0f) return 0f;
        }

        float afterFlat = Mathf.Max(rawAmount - Mathf.Max(0f, stats.armor), 0f);
        float final = afterFlat * (1f - Mathf.Clamp01(stats.damageReductionPercent));

        if (final <= 0f) return 0f;

        // Enemy/source element modifies damage dealt to the player.
        // Example: enemy Fire burns player.
        final = ApplySourceElementDamage(damageSource, final);
        if (final <= 0f) return 0f;

        // Player's own element modifies incoming damage.
        // Example: Water reduces damage when low HP.
        final = ApplyOwnElementIncomingDamage(damageSource, final);
        if (final <= 0f) return 0f;

        stats.currentHealth = Mathf.Max(0f, stats.currentHealth - final);

        if (!ignoreNormalIFrames)
            invulnTimer = invulnDuration;

        OnDamaged?.Invoke(final);
        Broadcast();

        if (stats.currentHealth <= 0f)
            HandleDeath();

        return final;
    }

    // =========================================================
    // Healing API
    // =========================================================

    public float Heal(float amount)
    {
        return HealFromSource(null, amount);
    }

    public float HealFromSource(GameObject healer, float amount)
    {
        if (amount <= 0f || IsDead()) return 0f;

        float originalHealing = amount;

        // IMPORTANT:
        // Nature modifies healing BEFORE HP is restored.
        // Example:
        // 50 healing with Nature +200% becomes 150 healing.
        float modifiedHealing = ApplyOwnElementHealingReceived(healer, amount);

        if (modifiedHealing <= 0f)
            return 0f;

        float before = stats.currentHealth;

        stats.currentHealth = Mathf.Min(stats.currentHealth + modifiedHealing, stats.maxHealth);

        float actualHealed = stats.currentHealth - before;

        if (showHealingDebug)
        {
            Debug.Log(
                $"[PlayerHealth] Heal. " +
                $"Original: {originalHealing:F1}, " +
                $"Modified: {modifiedHealing:F1}, " +
                $"Actual healed: {actualHealed:F1}, " +
                $"HP: {before:F1} -> {stats.currentHealth:F1}"
            );
        }

        if (actualHealed > 0f)
        {
            OnHealed?.Invoke(actualHealed);
            Broadcast();
        }

        return actualHealed;
    }

    // =========================================================
    // Element helpers
    // =========================================================

    private float ApplySourceElementDamage(GameObject damageSource, float damage)
    {
        ElementHolder holder = GetElementHolderFromSourceOrFallback(damageSource);

        if (holder == null || !holder.HasElement())
            return damage;

        return holder.ModifyDirectDamage(gameObject, damage);
    }

    private float ApplyOwnElementIncomingDamage(GameObject damageSource, float damage)
    {
        ElementHolder ownHolder = GetOwnElementHolder();

        if (ownHolder == null || !ownHolder.HasElement())
            return damage;

        return ownHolder.ModifyIncomingDirectDamage(damageSource, damage);
    }

    private float ApplyOwnElementHealingReceived(GameObject healer, float healing)
    {
        ElementHolder ownHolder = GetOwnElementHolder();

        if (ownHolder == null || !ownHolder.HasElement())
            return healing;

        return ownHolder.ModifyHealingReceived(healer, healing);
    }

    private ElementHolder GetOwnElementHolder()
    {
        ElementHolder ownHolder = GetComponent<ElementHolder>();

        if (ownHolder == null)
            ownHolder = GetComponentInParent<ElementHolder>();

        if (ownHolder == null)
            ownHolder = GetComponentInChildren<ElementHolder>();

        return ownHolder;
    }

    private ElementHolder GetElementHolderFromSourceOrFallback(GameObject damageSource)
    {
        if (damageSource != null)
        {
            ElementHolder sourceHolder = damageSource.GetComponent<ElementHolder>();

            if (sourceHolder == null)
                sourceHolder = damageSource.GetComponentInParent<ElementHolder>();

            if (sourceHolder != null && sourceHolder.HasElement())
                return sourceHolder;
        }

        if (!useNearestElementSourceWhenNoSource)
            return null;

        return FindNearestFallbackElementHolder();
    }

    private ElementHolder FindNearestFallbackElementHolder()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            Mathf.Max(0.1f, fallbackElementSearchRadius),
            fallbackElementSourceLayers
        );

        ElementHolder bestHolder = null;
        float bestDistSqr = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (!hit) continue;

            if (hit.transform == transform || hit.transform.IsChildOf(transform))
                continue;

            ElementHolder holder = hit.GetComponent<ElementHolder>();

            if (holder == null)
                holder = hit.GetComponentInParent<ElementHolder>();

            if (holder == null || !holder.HasElement())
                continue;

            float distSqr = (holder.transform.position - transform.position).sqrMagnitude;

            if (distSqr < bestDistSqr)
            {
                bestDistSqr = distSqr;
                bestHolder = holder;
            }
        }

        return bestHolder;
    }

    // =========================================================
    // HP API
    // =========================================================

    public void SetHealth(float value)
    {
        stats.currentHealth = Mathf.Clamp(value, 0f, Mathf.Max(1f, stats.maxHealth));
        Broadcast();
    }

    public void AddMaxHealth(float delta)
    {
        if (Mathf.Approximately(delta, 0f)) return;

        float oldMax = Mathf.Max(1f, stats.maxHealth);
        float newMax = Mathf.Max(1f, oldMax + delta);

        ApplyMaxChangePolicy(oldMax, newMax);
        Broadcast();
    }

    public bool IsDead()
    {
        return stats.currentHealth <= 0f;
    }

    public bool IsInvulnerable()
    {
        return invulnTimer > 0f;
    }

    public float GetHealthNormalized()
    {
        return stats.maxHealth > 0f ? stats.currentHealth / stats.maxHealth : 0f;
    }

    public float GetHealthPercent()
    {
        if (stats == null || stats.maxHealth <= 0f) return 0f;

        return Mathf.Clamp01(stats.currentHealth / stats.maxHealth);
    }

    public float GetCurrentHealth()
    {
        return stats != null ? stats.currentHealth : 0f;
    }

    public float GetMaxHealth()
    {
        return stats != null ? stats.maxHealth : 0f;
    }

    // =========================================================
    // Max HP handling
    // =========================================================

    private void HandleExternalMaxChange(float oldMax, float newMax)
    {
        ApplyMaxChangePolicy(oldMax, newMax);
        Broadcast();
    }

    private void ApplyMaxChangePolicy(float oldMax, float newMax)
    {
        oldMax = Mathf.Max(1f, oldMax);
        newMax = Mathf.Max(1f, newMax);

        float cur = Mathf.Clamp(stats.currentHealth, 0f, oldMax);

        switch (maxHpChangeMode)
        {
            case MaxHpChangeMode.KeepPercent:
                {
                    float pct = oldMax > 0f ? cur / oldMax : 1f;
                    float target = newMax * pct;

                    if (keepPercentFavorUp && newMax > oldMax && target < cur)
                        target = cur;

                    stats.maxHealth = newMax;
                    stats.currentHealth = Mathf.Clamp(target, 0f, newMax);
                    break;
                }

            case MaxHpChangeMode.HealByDelta:
                {
                    float delta = newMax - oldMax;

                    stats.maxHealth = newMax;
                    stats.currentHealth = Mathf.Clamp(cur + Mathf.Max(0f, delta), 0f, newMax);
                    break;
                }

            case MaxHpChangeMode.FillToMax:
                {
                    stats.maxHealth = newMax;
                    stats.currentHealth = newMax;
                    break;
                }

            case MaxHpChangeMode.ClampOnly:
                {
                    stats.maxHealth = newMax;
                    stats.currentHealth = Mathf.Min(cur, newMax);
                    break;
                }
        }
    }

    // =========================================================
    // Death / UI
    // =========================================================

    private void HandleDeath()
    {
        OnDeath?.Invoke();
        Debug.Log("[PlayerHealth] Player died.");
    }

    private void SetupUI()
    {
        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = stats.maxHealth;
            healthSlider.value = stats.currentHealth;
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (healthSlider != null)
        {
            if (!Mathf.Approximately(healthSlider.maxValue, stats.maxHealth))
                healthSlider.maxValue = stats.maxHealth;

            healthSlider.value = stats.currentHealth;
        }

        if (healthText != null)
        {
            healthText.text = $"{Mathf.CeilToInt(stats.currentHealth)} / {Mathf.CeilToInt(stats.maxHealth)}";
        }
    }

    private void Broadcast()
    {
        stats.maxHealth = Mathf.Max(1f, stats.maxHealth);
        stats.currentHealth = Mathf.Clamp(stats.currentHealth, 0f, stats.maxHealth);

        OnHealthChanged?.Invoke(stats.currentHealth, stats.maxHealth);
        UpdateUI();

        _lastSeenMax = stats.maxHealth;
        _lastSeenCur = stats.currentHealth;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!useNearestElementSourceWhenNoSource)
            return;

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, fallbackElementSearchRadius);
    }
#endif
}