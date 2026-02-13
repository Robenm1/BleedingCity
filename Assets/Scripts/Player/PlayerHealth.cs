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
        if (invulnTimer > 0f) invulnTimer -= Time.deltaTime;

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

    public float ApplyDamage(float rawAmount)
    {
        if (rawAmount <= 0f) return 0f;
        if (IsDead() || IsInvulnerable()) return 0f;

        var pyroAbility = GetComponent<PyroAbility1>();
        if (pyroAbility != null && pyroAbility.IsInvulnerable())
        {
            return 0f;
        }

        if (GolemFireCircle.activeCircle != null && GolemFireCircle.activeCircle.currentShield > 0f)
        {
            rawAmount = GolemFireCircle.activeCircle.AbsorbDamage(rawAmount);
            if (rawAmount <= 0f) return 0f;
        }

        float afterFlat = Mathf.Max(rawAmount - Mathf.Max(0f, stats.armor), 0f);
        float final = afterFlat * (1f - Mathf.Clamp01(stats.damageReductionPercent));
        if (final <= 0f) return 0f;

        stats.currentHealth = Mathf.Max(0f, stats.currentHealth - final);
        invulnTimer = invulnDuration;

        OnDamaged?.Invoke(final);
        Broadcast();

        if (stats.currentHealth <= 0f)
            HandleDeath();

        return final;
    }

    public float Heal(float amount)
    {
        if (amount <= 0f || IsDead()) return 0f;
        float before = stats.currentHealth;
        stats.currentHealth = Mathf.Min(stats.currentHealth + amount, stats.maxHealth);
        float healed = stats.currentHealth - before;

        if (healed > 0f)
        {
            OnHealed?.Invoke(healed);
            Broadcast();
        }
        return healed;
    }

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

    public bool IsDead() => stats.currentHealth <= 0f;
    public bool IsInvulnerable() => invulnTimer > 0f;
    public float GetHealthNormalized() => stats.maxHealth > 0f ? stats.currentHealth / stats.maxHealth : 0f;

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
                    if (keepPercentFavorUp && newMax > oldMax && target < cur) target = cur;
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
}
