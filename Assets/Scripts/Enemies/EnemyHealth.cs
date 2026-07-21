using UnityEngine;
using UnityEngine.UI;
using System;
using URandom = UnityEngine.Random;

public class EnemyHealth : MonoBehaviour
{
    public static event Action<EnemyHealth> OnAnyEnemyDied;
    public static event Action<EnemyHealth, float> OnAnyEnemyDamaged;

    [Serializable]
    public class ElementResistance
    {
        [Tooltip("The source element this resistance applies against.")]
        public BaseElementSO element;

        [Tooltip(
            "0 = normal damage\n" +
            "0.25 = takes 25% less damage\n" +
            "0.5 = takes 50% less damage\n" +
            "1 = immune\n" +
            "-0.25 = takes 25% more damage\n" +
            "-0.5 = takes 50% more damage\n" +
            "-1 = takes double damage"
        )]
        [Range(-2f, 1f)]
        public float resistance = 0f;
    }

    [Header("Health")]
    public float maxHP = 50f;
    protected float currentHP;

    [Header("Element Resistance")]
    public bool useElementResistance = true;

    [Range(-2f, 1f)]
    public float defaultElementResistance = 0f;

    public ElementResistance[] elementResistances;

    public bool showElementResistanceDebug = false;

    [Header("XP Drop")]
    public GameObject xpCoinPrefab;
    public int coinsToDrop = 1;
    public float dropScatterRadius = 0.3f;

    [Header("Damage Popup")]
    public GameObject damagePopupPrefab;
    public Vector3 popupOffset = new Vector3(0f, 0.8f, 0f);
    public Color defaultDamageColor = Color.white;
    public float popupRandomXSpread = 0.2f;

    [Header("HP Bar UI")]
    public RectTransform hpUIRoot;
    public Slider hpSlider;
    public float visibleForSecondsAfterHit = 2f;
    public Vector3 worldOffset = new Vector3(0f, 1.2f, 0f);
    public Camera uiCamera;

    [Header("Follow Smoothing")]
    public bool smoothFollow = true;
    [Range(0.01f, 0.3f)] public float followSmoothTime = 0.1f;

    private float hideTimer = 0f;

    protected bool alwaysShowHPBar = false;

    private Canvas hpCanvas;
    private RectTransform canvasRect;
    private bool isWorldSpaceCanvas;

    private Vector2 _uiVel;
    private Vector2 _lastAnchoredPos;
    private bool _convertingToTurret = false;

    private static ElementHolder _cachedPlayerElementHolder;

    protected float _vulnMul = 1f;
    private float _vulnTimer = 0f;

    protected virtual void Awake()
    {
        currentHP = maxHP;

        if (hpUIRoot != null)
        {
            hpCanvas = hpUIRoot.GetComponentInParent<Canvas>();

            if (hpCanvas != null)
            {
                canvasRect = hpCanvas.GetComponent<RectTransform>();
                isWorldSpaceCanvas = hpCanvas.renderMode == RenderMode.WorldSpace;
            }
        }

        if (uiCamera == null)
            uiCamera = Camera.main;

        InitHPUI();
        HideHPUIImmediate();
    }

    private void Update()
    {
        if (_vulnTimer > 0f)
        {
            _vulnTimer -= Time.deltaTime;

            if (_vulnTimer <= 0f)
            {
                _vulnTimer = 0f;
                _vulnMul = 1f;
            }
        }

        if (hpUIRoot != null && hpUIRoot.gameObject.activeSelf)
        {
            hideTimer -= Time.deltaTime;

            if (!alwaysShowHPBar && hideTimer <= 0f && Mathf.Approximately(currentHP, maxHP))
                HideHPUIImmediate();
        }
    }

    private void LateUpdate()
    {
        if (hpUIRoot == null || hpCanvas == null) return;

        Vector3 worldPos = transform.position + worldOffset;

        if (isWorldSpaceCanvas)
        {
            if (smoothFollow)
            {
                hpUIRoot.position = Vector3.Lerp(
                    hpUIRoot.position,
                    worldPos,
                    1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.0001f, followSmoothTime))
                );
            }
            else
            {
                hpUIRoot.position = worldPos;
            }

            hpUIRoot.rotation = Quaternion.identity;
            return;
        }

        Camera cam = hpCanvas.renderMode == RenderMode.ScreenSpaceCamera ? uiCamera : null;

        Vector2 screenPoint = cam != null
            ? (Vector2)cam.WorldToScreenPoint(worldPos)
            : Camera.main != null ? (Vector2)Camera.main.WorldToScreenPoint(worldPos) : (Vector2)worldPos;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, cam, out var localPoint))
        {
            if (smoothFollow && hpUIRoot.gameObject.activeInHierarchy)
            {
                Vector2 target = localPoint;
                Vector2 smoothed = Vector2.SmoothDamp(_lastAnchoredPos, target, ref _uiVel, followSmoothTime);

                hpUIRoot.anchoredPosition = smoothed;
                _lastAnchoredPos = smoothed;
            }
            else
            {
                hpUIRoot.anchoredPosition = localPoint;
                _lastAnchoredPos = localPoint;
                _uiVel = Vector2.zero;
            }
        }
    }

    public virtual void TakeSummonDamage(float dmg)
    {
        TakeSummonDamageFromSource(null, dmg);
    }

    public virtual void TakeSummonDamageFromSource(GameObject damageSource, float dmg)
    {
        var mark = GetComponent<HellsJusticeMark>();

        if (mark != null)
            dmg *= mark.GetDamageMultiplier();

        TakeDamageFromSource(damageSource, dmg);
    }

    public virtual void TakeDamage(float dmg)
    {
        TakeDamageFromSource(null, dmg);
    }

    public virtual void TakeDamageFromSource(GameObject damageSource, float dmg)
    {
        if (DeathTouchEffect.TryConvertToDot(this, dmg))
            return;

        TakeDamageDirectFromSource(damageSource, dmg);
    }

    public virtual void TakeDamageDirect(float dmg)
    {
        TakeDamageDirectFromSource(null, dmg);
    }

    public virtual void TakeDamageDirectFromSource(GameObject damageSource, float dmg)
    {
        if (dmg <= 0f || currentHP <= 0f) return;

        dmg = ApplySourceElementDirectDamage(damageSource, dmg);

        dmg = ApplyTargetElementResistance(damageSource, dmg);

        dmg = ApplyOwnElementIncomingDamage(damageSource, dmg);

        dmg *= _vulnMul;

        var frosted = GetComponent<FrostedOnEnemy>();
        if (frosted != null && frosted.IsActive)
            dmg *= frosted.vulnerabilityMultiplier;

        float finalDamage = Mathf.Max(0f, dmg);
        if (finalDamage <= 0f) return;

        currentHP -= finalDamage;

        if (currentHP < 0f)
            currentHP = 0f;

        NotifyEnemyDamaged(finalDamage);

        Color popupColor = GetDamageSourceColor(damageSource);
        SpawnDamagePopup(finalDamage, popupColor);

        UpdateHPUI();
        ShowHPUI();

        if (currentHP <= 0f)
            Die();
    }

    protected void NotifyEnemyDamaged(float damage)
    {
        if (damage <= 0f) return;

        OnAnyEnemyDamaged?.Invoke(this, damage);
    }

    protected float ApplySourceElementDirectDamage(GameObject damageSource, float damage)
    {
        ElementHolder holder = GetElementHolderFromSourceOrFallback(damageSource);

        if (holder == null || !holder.HasElement())
            return damage;

        return holder.ModifyDirectDamage(gameObject, damage);
    }

    protected float ApplyTargetElementResistance(GameObject damageSource, float damage)
    {
        if (!useElementResistance)
            return damage;

        ElementHolder sourceHolder = GetElementHolderFromSourceOrFallback(damageSource);

        if (sourceHolder == null || !sourceHolder.HasElement())
        {
            if (showElementResistanceDebug)
                Debug.Log($"[Resistance] {name}: No source element. Damage stays {damage:F1}");

            return damage;
        }

        BaseElementSO sourceElement = sourceHolder.GetElement();

        if (sourceElement == null)
        {
            if (showElementResistanceDebug)
                Debug.Log($"[Resistance] {name}: Source holder has no element. Damage stays {damage:F1}");

            return damage;
        }

        float resistance = defaultElementResistance;
        bool foundSpecificResistance = false;

        if (elementResistances != null)
        {
            for (int i = 0; i < elementResistances.Length; i++)
            {
                ElementResistance entry = elementResistances[i];

                if (entry == null || entry.element == null)
                    continue;

                if (ElementsMatch(entry.element, sourceElement))
                {
                    resistance = entry.resistance;
                    foundSpecificResistance = true;
                    break;
                }
            }
        }

        resistance = Mathf.Clamp(resistance, -2f, 1f);

        float multiplier = 1f - resistance;
        float finalDamage = damage * multiplier;

        if (showElementResistanceDebug)
        {
            string sourceName = !string.IsNullOrEmpty(sourceElement.elementName)
                ? sourceElement.elementName
                : sourceElement.name;

            Debug.Log(
                $"[Resistance] {name} hit by {sourceName}. " +
                $"Specific: {foundSpecificResistance}, " +
                $"Resistance: {resistance:F2}, " +
                $"Multiplier: {multiplier:F2}, " +
                $"Damage: {damage:F1} -> {finalDamage:F1}"
            );
        }

        return finalDamage;
    }

    protected float ApplyOwnElementIncomingDamage(GameObject damageSource, float damage)
    {
        ElementHolder ownHolder = GetComponent<ElementHolder>();

        if (ownHolder == null)
            ownHolder = GetComponentInParent<ElementHolder>();

        if (ownHolder == null || !ownHolder.HasElement())
            return damage;

        return ownHolder.ModifyIncomingDirectDamage(damageSource, damage);
    }

    private bool ElementsMatch(BaseElementSO resistanceElement, BaseElementSO sourceElement)
    {
        if (resistanceElement == null || sourceElement == null)
            return false;

        if (resistanceElement == sourceElement)
            return true;

        if (!string.IsNullOrEmpty(resistanceElement.elementName) &&
            !string.IsNullOrEmpty(sourceElement.elementName) &&
            resistanceElement.elementName == sourceElement.elementName)
            return true;

        if (resistanceElement.GetType() == sourceElement.GetType())
            return true;

        return false;
    }

    protected Color GetDamageSourceColor(GameObject damageSource)
    {
        ElementHolder holder = GetElementHolderFromSourceOrFallback(damageSource);

        if (holder == null || !holder.HasElement())
            return defaultDamageColor;

        return holder.GetElementColor();
    }

    protected ElementHolder GetElementHolderFromSourceOrFallback(GameObject damageSource)
    {
        if (damageSource != null)
        {
            ElementHolder sourceHolder = damageSource.GetComponent<ElementHolder>();

            if (sourceHolder == null)
                sourceHolder = damageSource.GetComponentInParent<ElementHolder>();

            if (sourceHolder != null && sourceHolder.HasElement())
                return sourceHolder;
        }

        return GetPlayerElementHolderFallback();
    }

    private ElementHolder GetPlayerElementHolderFallback()
    {
        if (_cachedPlayerElementHolder != null)
            return _cachedPlayerElementHolder;

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
            return null;

        _cachedPlayerElementHolder = player.GetComponent<ElementHolder>();

        if (_cachedPlayerElementHolder == null)
            _cachedPlayerElementHolder = player.GetComponentInChildren<ElementHolder>();

        return _cachedPlayerElementHolder;
    }

    private void InitHPUI()
    {
        if (hpSlider != null)
        {
            hpSlider.minValue = 0f;
            hpSlider.maxValue = 1f;
            hpSlider.value = 1f;
        }
    }

    protected void UpdateHPUI()
    {
        if (hpSlider != null)
        {
            float t = maxHP > 0f ? currentHP / maxHP : 0f;
            hpSlider.value = Mathf.Clamp01(t);
        }
    }

    protected void ShowHPUI()
    {
        if (hpUIRoot == null) return;

        if (!hpUIRoot.gameObject.activeSelf)
            hpUIRoot.gameObject.SetActive(true);

        _lastAnchoredPos = hpUIRoot.anchoredPosition;
        _uiVel = Vector2.zero;

        hideTimer = visibleForSecondsAfterHit;
    }

    private void HideHPUIImmediate()
    {
        if (hpUIRoot == null) return;

        if (hpUIRoot.gameObject.activeSelf)
            hpUIRoot.gameObject.SetActive(false);
    }

    protected void SpawnDamagePopup(float dmg, Color popupColor)
    {
        if (damagePopupPrefab == null) return;

        Vector3 spawnPos = transform.position + popupOffset;
        spawnPos.x += URandom.Range(-popupRandomXSpread, popupRandomXSpread);

        GameObject popupObj = Instantiate(damagePopupPrefab, spawnPos, Quaternion.identity);

        DamagePopup popup = popupObj.GetComponent<DamagePopup>();

        if (popup != null)
            popup.Setup(dmg, popupColor);
    }

    protected virtual void Die()
    {
        DropXP();
        OnAnyEnemyDied?.Invoke(this);

        if (_convertingToTurret)
        {
            Debug.Log($"[EnemyHealth] {name} marked for turret conversion, skipping destruction.");
            return;
        }

        if (hpUIRoot != null)
            Destroy(hpUIRoot.gameObject);

        Destroy(gameObject);
    }

    public void MarkAsConvertingToTurret()
    {
        _convertingToTurret = true;
    }

    private void DropXP()
    {
        if (xpCoinPrefab == null) return;

        for (int i = 0; i < coinsToDrop; i++)
        {
            Vector2 offset = URandom.insideUnitCircle * dropScatterRadius;
            Vector3 spawnPos = transform.position + (Vector3)offset;

            Instantiate(xpCoinPrefab, spawnPos, Quaternion.identity);
        }
    }

    public void Heal(float amount)
    {
        if (amount <= 0f || currentHP <= 0f) return;

        currentHP = Mathf.Min(currentHP + amount, maxHP);

        UpdateHPUI();
        ShowHPUI();
    }

    public void ApplyVulnerability(float multiplier, float duration)
    {
        _vulnMul = Mathf.Max(0.01f, multiplier);
        _vulnTimer = Mathf.Max(_vulnTimer, duration);
    }

    public float GetHealthPercent()
    {
        if (maxHP <= 0f) return 0f;
        return Mathf.Clamp01(currentHP / maxHP);
    }
}