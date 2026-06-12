using UnityEngine;
using TMPro;

/// <summary>
/// HP system for the training dummy. Inherits EnemyHealth so all existing
/// damage sources (Bullet, PlayerShooting, summons, etc.) that do
/// GetComponent&lt;EnemyHealth&gt;() still find it.
///
/// Differences from EnemyHealth:
///   - Never dies: HP clamps at 0 instead of calling Die().
///   - After 2 seconds of no damage the HP resets to full.
///   - Spawns a floating damage number popup on every hit.
/// </summary>
public class DummyHealth : EnemyHealth
{
    private const float ResetDelay = 2f;

    [Header("Dummy Settings")]
    [Tooltip("Prefab with TextMeshPro and DamagePopup.cs on it.")]
    public GameObject damagePopupPrefab;

    [Tooltip("World-space offset above the dummy where popups spawn.")]
    public Vector3 popupOffset = new Vector3(0f, 0.8f, 0f);

    [Tooltip("TextMeshProUGUI element displayed above the HP bar to show cumulative damage taken.")]
    public TextMeshProUGUI damageCounterText;

    private float _totalDamage;
    private float _timeSinceLastHit;
    private bool _isAtZero;

    protected override void Awake()
    {
        alwaysShowHPBar = true;
        base.Awake();
        ShowHPUI();
        _totalDamage = 0f;
        _timeSinceLastHit = ResetDelay; // start satisfied so no reset fires before first hit
        _isAtZero = false;
        UpdateDamageCounter();
    }

    private void Update()
    {
        if (_timeSinceLastHit < ResetDelay)
        {
            _timeSinceLastHit += Time.deltaTime;
            if (_timeSinceLastHit >= ResetDelay)
            {
                ResetHP();
            }
        }
    }

    /// <summary>
    /// Overrides base damage handling: the dummy never dies, HP clamps at 0,
    /// and a floating damage number is spawned for each hit.
    /// </summary>
    public override void TakeDamage(float dmg)
    {
        if (dmg <= 0f) return;

        // Apply multipliers manually so the popup shows the true damage value
        dmg *= _vulnMul;

        var frosted = GetComponent<FrostedOnEnemy>();
        if (frosted != null && frosted.IsActive)
            dmg *= frosted.vulnerabilityMultiplier;

        // Always show popup and refresh the reset timer, even when HP is already 0
        SpawnDamagePopup(dmg);
        _totalDamage += dmg;
        _timeSinceLastHit = 0f;
        UpdateDamageCounter();

        if (currentHP <= 0f)
        {
            // Already pinned at zero — just keep resetting the timer
            _isAtZero = true;
            return;
        }

        currentHP -= dmg;
        if (currentHP < 0f) currentHP = 0f;

        _isAtZero = (currentHP <= 0f);

        UpdateHPUI();
        ShowHPUI();
    }

    /// <summary>
    /// Override: dummy never dies.
    /// </summary>
    protected override void Die() { }

    // ===== Internals =====

    private void ResetHP()
    {
        currentHP = maxHP;
        _totalDamage = 0f;
        _isAtZero = false;
        _timeSinceLastHit = ResetDelay; // prevent immediate re-trigger
        UpdateHPUI();
        ShowHPUI();
        UpdateDamageCounter();
    }

    /// <summary>
    /// Refreshes the damage counter text. Shows accumulated damage rounded to the nearest integer.
    /// </summary>
    private void UpdateDamageCounter()
    {
        if (damageCounterText == null) return;
        damageCounterText.SetText(Mathf.RoundToInt(_totalDamage).ToString());
    }

    private void SpawnDamagePopup(float dmg)
    {
        if (damagePopupPrefab == null) return;

        Vector3 spawnPos = transform.position + popupOffset;
        spawnPos.x += Random.Range(-0.2f, 0.2f);

        GameObject popupObj = Instantiate(damagePopupPrefab, spawnPos, Quaternion.identity);
        DamagePopup popup = popupObj.GetComponent<DamagePopup>();
        if (popup != null)
            popup.Setup(dmg);
    }
}
