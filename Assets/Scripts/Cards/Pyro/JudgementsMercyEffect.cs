using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerStats))]
public class JudgementsMercyEffect : MonoBehaviour
{
    [Header("Judgement Heal")]
    [Tooltip("Percent of Pyro's current HP restored when killing an enemy marked with Judgement. 0.01 = 1%.")]
    public float healCurrentHpPercent = 0.01f;

    [Tooltip("If true, Pyro cannot heal above max HP.")]
    public bool clampToMaxHp = true;

    [Header("Debug")]
    public bool showDebug = true;

    private PlayerStats _stats;
    private bool _registered;

    private void Awake()
    {
        _stats = GetComponent<PlayerStats>();
    }

    private void OnEnable()
    {
        if (_registered) return;

        if (!_stats)
            _stats = GetComponent<PlayerStats>();

        if (!_stats)
        {
            Debug.LogWarning("[JudgementsMercyEffect] PlayerStats was not found on player.");
            return;
        }

        EnemyHealth.OnAnyEnemyDied += OnAnyEnemyDied;
        _registered = true;
    }

    private void OnDisable()
    {
        if (_registered)
            EnemyHealth.OnAnyEnemyDied -= OnAnyEnemyDied;

        _registered = false;
    }

    private void OnAnyEnemyDied(EnemyHealth enemy)
    {
        if (enemy == null || _stats == null) return;

        var mark = enemy.GetComponent<HellsJusticeMark>();
        if (mark == null || !mark.enabled) return;

        HealPyro();
    }

    private void HealPyro()
    {
        if (_stats.currentHealth <= 0f) return;

        float healAmount = _stats.currentHealth * Mathf.Max(0f, healCurrentHpPercent);
        if (healAmount <= 0f) return;

        _stats.currentHealth += healAmount;

        if (clampToMaxHp)
            _stats.currentHealth = Mathf.Min(_stats.currentHealth, _stats.maxHealth);

        if (showDebug)
            Debug.Log($"[JudgementsMercyEffect] Pyro healed for {healAmount:F2}. Current HP: {_stats.currentHealth:F1}/{_stats.maxHealth:F1}");
    }
}