using UnityEngine;
using TMPro;

public class DummyHealth : EnemyHealth
{
    private const float ResetDelay = 2f;

    [Header("Dummy Settings")]
    public TextMeshProUGUI damageCounterText;
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
                ResetHP();
        }

        UpdateCounterTextPosition();
    }

    public override void TakeDamage(float dmg)
    {
        TakeDamageFromSource(null, dmg);
    }

    public override void TakeDamageFromSource(GameObject damageSource, float dmg)
    {
        if (DeathTouchEffect.TryConvertToDot(this, dmg))
            return;

        TakeDamageDirectFromSource(damageSource, dmg);
    }

    public override void TakeDamageDirect(float dmg)
    {
        TakeDamageDirectFromSource(null, dmg);
    }

    public override void TakeDamageDirectFromSource(GameObject damageSource, float dmg)
    {
        if (dmg <= 0f) return;

        dmg = ApplySourceElementDirectDamage(damageSource, dmg);

        dmg = ApplyTargetElementResistance(damageSource, dmg);

        dmg = ApplyOwnElementIncomingDamage(damageSource, dmg);

        dmg *= _vulnMul;

        var frosted = GetComponent<FrostedOnEnemy>();
        if (frosted != null && frosted.IsActive)
            dmg *= frosted.vulnerabilityMultiplier;

        float finalDamage = Mathf.Max(0f, dmg);
        if (finalDamage <= 0f) return;

        Color popupColor = GetDamageSourceColor(damageSource);

        NotifyEnemyDamaged(finalDamage);
        SpawnDamagePopup(finalDamage, popupColor);

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

    private void OnTriggerEnter2D(Collider2D other)
    {
        other.GetComponent<HellBomb>()?.TriggerIfArmed();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        other.GetComponent<HellBomb>()?.TriggerIfArmed();
    }

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

        RectTransform rt = damageCounterText.rectTransform;
        Vector2 current = rt.anchoredPosition;

        if (!Mathf.Approximately(current.y, targetY))
            rt.anchoredPosition = new Vector2(_baseCounterTextPos.x, targetY);
    }
}