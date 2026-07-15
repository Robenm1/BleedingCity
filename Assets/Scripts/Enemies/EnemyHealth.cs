using UnityEngine;
using UnityEngine.UI;
using System;
using URandom = UnityEngine.Random;

public class EnemyHealth : MonoBehaviour
{
    // ===== Global events =====
    public static event Action<EnemyHealth> OnAnyEnemyDied;

    // Added for Bullet from the Past.
    // Fires whenever any enemy successfully takes damage.
    public static event Action<EnemyHealth, float> OnAnyEnemyDamaged;

    [Header("Health")]
    public float maxHP = 50f;
    protected float currentHP;

    [Header("XP Drop")]
    [Tooltip("The coin prefab to drop on death. This prefab should have XPCoin.cs on it.")]
    public GameObject xpCoinPrefab;

    [Tooltip("How many coins to drop on death.")]
    public int coinsToDrop = 1;

    [Tooltip("Random drop scatter distance so coins don't all stack perfectly.")]
    public float dropScatterRadius = 0.3f;

    [Header("HP Bar UI")]
    [Tooltip("Root RectTransform of the enemy HP UI (can be the Slider parent).")]
    public RectTransform hpUIRoot;

    [Tooltip("Slider that displays enemy HP (0..1).")]
    public Slider hpSlider;

    [Tooltip("How long (seconds) the bar stays visible after taking damage.")]
    public float visibleForSecondsAfterHit = 2f;

    [Tooltip("Vertical world offset for the bar above the enemy.")]
    public Vector3 worldOffset = new Vector3(0f, 1.2f, 0f);

    [Tooltip("For Screen Space - Camera canvases, set the UI camera here. For Overlay you can leave null.")]
    public Camera uiCamera;

    [Header("Follow Smoothing")]
    [Tooltip("Smooth the UI movement to avoid jitter when the enemy/camera moves.")]
    public bool smoothFollow = true;

    [Tooltip("Time (seconds) to reach the target UI position. 0.08-0.15 is a good range.")]
    [Range(0.01f, 0.3f)] public float followSmoothTime = 0.1f;

    private float hideTimer = 0f;

    /// <summary>
    /// When true the HP bar is never auto-hidden. Set by subclasses such as DummyHealth.
    /// </summary>
    protected bool alwaysShowHPBar = false;

    private Canvas hpCanvas;
    private RectTransform canvasRect;
    private bool isWorldSpaceCanvas;

    private Vector2 _uiVel;
    private Vector2 _lastAnchoredPos;
    private bool _convertingToTurret = false;

    // Temporary vulnerability support.
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
            {
                HideHPUIImmediate();
            }
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

    // ===== Public damage API =====

    /// <summary>
    /// Same as TakeDamage but applies the Hell's Justice mark multiplier when active.
    /// Use this for all summon damage calls.
    /// </summary>
    public virtual void TakeSummonDamage(float dmg)
    {
        var mark = GetComponent<HellsJusticeMark>();
        if (mark != null)
            dmg *= mark.GetDamageMultiplier();

        TakeDamage(dmg);
    }

    public virtual void TakeDamage(float dmg)
    {
        if (DeathTouchEffect.TryConvertToDot(this, dmg))
            return;

        TakeDamageDirect(dmg);
    }

    public virtual void TakeDamageDirect(float dmg)
    {
        if (dmg <= 0f || currentHP <= 0f) return;

        dmg *= _vulnMul;

        var frosted = GetComponent<FrostedOnEnemy>();
        if (frosted != null && frosted.IsActive)
        {
            dmg *= frosted.vulnerabilityMultiplier;
        }

        float finalDamage = Mathf.Max(0f, dmg);
        if (finalDamage <= 0f) return;

        currentHP -= finalDamage;
        if (currentHP < 0f)
            currentHP = 0f;

        // Added for Bullet from the Past.
        NotifyEnemyDamaged(finalDamage);

        UpdateHPUI();
        ShowHPUI();

        if (currentHP <= 0f)
        {
            Die();
        }
    }

    /// <summary>
    /// Notifies global listeners that this enemy took damage.
    /// Used by cards like Bullet from the Past.
    /// </summary>
    protected void NotifyEnemyDamaged(float damage)
    {
        if (damage <= 0f) return;

        OnAnyEnemyDamaged?.Invoke(this, damage);
    }

    // ===== Internals =====

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

    /// <summary>
    /// Makes this enemy take more damage for a duration.
    /// multiplier = 1.25 means +25% damage taken.
    /// Duration stacks by time, but multiplier is replaced.
    /// </summary>
    public void ApplyVulnerability(float multiplier, float duration)
    {
        _vulnMul = Mathf.Max(0.01f, multiplier);
        _vulnTimer = Mathf.Max(_vulnTimer, duration);
    }
}