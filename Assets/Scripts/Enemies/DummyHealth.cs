using UnityEngine;
using TMPro;

/// <summary>
/// HP system for the training dummy. Inherits EnemyHealth so all existing
/// damage sources that do GetComponent<EnemyHealth>() still find it.
/// 
/// Differences from EnemyHealth:
/// - Never dies.
/// - HP clamps at 0 instead of calling Die().
/// - After 2 seconds of no damage the HP resets to full.
/// - Spawns a floating damage number popup on every hit.
/// - Moves the damage counter text upward while marks are active.
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

    [Tooltip("How far up (in canvas units) the damage counter shifts when marks are active on the dummy.")]
    public float markTextYOffset = 40f;

    private float _totalDamage;
    private float _timeSinceLastHit;
    private bool _isAtZero;

    private MarkDisplayController _markDisplay;
    private Vector2 _baseCounterTextPos;
    private bool _basePosCaptured;

    private MarkDisplayController MarkDisplay
    {
        get
        {
            if (_markDisplay == null)
                _markDisplay = GetComponent<MarkDisplayController>();

            return _markDisplay;
        }
    }

    protected override void Awake()
    {
        alwaysShowHPBar = true;

        base.Awake();

        ShowHPUI();

        _totalDamage = 0f;
        _timeSinceLastHit = ResetDelay;
        _isAtZero = false;

        UpdateDamageCounter();
    }

    private void Start()
    {
        if (damageCounterText != null)
        {
            _baseCounterTextPos = damageCounterText.rectTransform.anchoredPosition;
            _basePosCaptured = true;
        }
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

        UpdateCounterTextPosition();
    }

    public override void TakeDamage(float dmg)
    {
        if (DeathTouchEffect.TryConvertToDot(this, dmg))
            return;

        TakeDamageDirect(dmg);
    }

    public override void TakeDamageDirect(float dmg)
    {
        if (dmg <= 0f) return;

        dmg *= _vulnMul;

        var frosted = GetComponent<FrostedOnEnemy>();
        if (frosted != null && frosted.IsActive)
            dmg *= frosted.vulnerabilityMultiplier;

        float finalDamage = Mathf.Max(0f, dmg);
        if (finalDamage <= 0f) return;

        // Added for Bullet from the Past.
        // Dummy hits now count as global enemy damage too.
        NotifyEnemyDamaged(finalDamage);

        SpawnDamagePopup(finalDamage);

        _totalDamage += finalDamage;
        _timeSinceLastHit = 0f;

        UpdateDamageCounter();

        if (currentHP <= 0f)
        {
            _isAtZero = true;
            return;
        }

        currentHP -= finalDamage;
        if (currentHP < 0f)
            currentHP = 0f;

        _isAtZero = currentHP <= 0f;

        UpdateHPUI();
        ShowHPUI();
    }

    protected override void Die()
    {
        // Dummy never dies.
    }

    // ===== PyroHellBomb integration =====

    private void OnTriggerEnter2D(Collider2D other)
    {
        other.GetComponent<HellBomb>()?.TriggerIfArmed();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        other.GetComponent<HellBomb>()?.TriggerIfArmed();
    }

    // ===== Internals =====

    private void ResetHP()
    {
        currentHP = maxHP;
        _totalDamage = 0f;
        _isAtZero = false;
        _timeSinceLastHit = ResetDelay;

        UpdateHPUI();
        ShowHPUI();
        UpdateDamageCounter();
    }

    private void UpdateDamageCounter()
    {
        if (damageCounterText == null) return;

        damageCounterText.SetText(Mathf.RoundToInt(_totalDamage).ToString());
    }

    private void UpdateCounterTextPosition()
    {
        if (damageCounterText == null || !_basePosCaptured) return;

        bool hasMarks = MarkDisplay != null && MarkDisplay.ActiveMarkCount > 0;
        float targetY = _baseCounterTextPos.y + (hasMarks ? markTextYOffset : 0f);

        var rt = damageCounterText.rectTransform;
        Vector2 current = rt.anchoredPosition;

        if (!Mathf.Approximately(current.y, targetY))
            rt.anchoredPosition = new Vector2(_baseCounterTextPos.x, targetY);
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