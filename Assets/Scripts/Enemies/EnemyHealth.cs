using UnityEngine;
using UnityEngine.UI;
using System;                 // for Action
using URandom = UnityEngine.Random;  // <-- alias Unity's Random

public class EnemyHealth : MonoBehaviour
{
    // ===== Global death event =====
    public static event Action<EnemyHealth> OnAnyEnemyDied;

    [Header("Health")]
    public float maxHP = 50f;
    private float currentHP;

    [Header("XP Drop")]
    [Tooltip("The coin prefab to drop on death. This prefab should have XPCoin.cs on it.")]
    public GameObject xpCoinPrefab;

    [Tooltip("How many coins to drop on death.")]
    public int coinsToDrop = 1;

    [Tooltip("Random drop scatter distance so coins don't all stack perfectly.")]
    public float dropScatterRadius = 0.3f;

    // ===== HP BAR =====
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

    [Tooltip("Time (seconds) to reach the target UI position. 0.08–0.15 is a good range.")]
    [Range(0.01f, 0.3f)] public float followSmoothTime = 0.1f;

    private float hideTimer = 0f;

    // Cached canvas info
    private Canvas hpCanvas;
    private RectTransform canvasRect;
    private bool isWorldSpaceCanvas;

    // Smoothing state
    private Vector2 _uiVel;
    private Vector2 _lastAnchoredPos;

    // ===== Added: temporary vulnerability support =====
    // Multiplies incoming damage; returns to 1f when timer ends.
    private float _vulnMul = 1f;
    private float _vulnTimer = 0f;

    private void Awake()
    {
        currentHP = maxHP;

        if (hpUIRoot != null)
        {
            hpCanvas = hpUIRoot.GetComponentInParent<Canvas>();
            if (hpCanvas != null)
            {
                canvasRect = hpCanvas.GetComponent<RectTransform>();
                isWorldSpaceCanvas = (hpCanvas.renderMode == RenderMode.WorldSpace);
            }
        }

        if (uiCamera == null) uiCamera = Camera.main;

        InitHPUI();
        HideHPUIImmediate(); // start hidden
    }

    private void Update()
    {
        // ===== Added: tick vulnerability timer =====
        if (_vulnTimer > 0f)
        {
            _vulnTimer -= Time.deltaTime;
            if (_vulnTimer <= 0f)
            {
                _vulnTimer = 0f;
                _vulnMul = 1f; // back to normal damage
            }
        }

        // Handle timed hiding (only when full)
        if (hpUIRoot != null && hpUIRoot.gameObject.activeSelf)
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0f && Mathf.Approximately(currentHP, maxHP))
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
            // World Space: position directly in world
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

            hpUIRoot.rotation = Quaternion.identity; // keep upright; billboard if desired
            return;
        }

        // Screen Space – Overlay or Camera: convert to canvas local space
        Camera cam = (hpCanvas.renderMode == RenderMode.ScreenSpaceCamera) ? uiCamera : null;

        Vector2 screenPoint = (cam != null)
            ? (Vector2)cam.WorldToScreenPoint(worldPos)
            : (Camera.main != null ? (Vector2)Camera.main.WorldToScreenPoint(worldPos) : (Vector2)worldPos);

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
    public void TakeDamage(float dmg)
    {
        if (dmg <= 0f || currentHP <= 0f) return;

        // ===== Added: apply temporary vulnerability multiplier =====
        dmg *= _vulnMul;

        currentHP -= dmg;
        if (currentHP < 0f) currentHP = 0f;

        UpdateHPUI();
        ShowHPUI();

        if (currentHP <= 0f)
        {
            Die();
        }
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

    private void UpdateHPUI()
    {
        if (hpSlider != null)
        {
            float t = (maxHP > 0f) ? (currentHP / maxHP) : 0f;
            hpSlider.value = Mathf.Clamp01(t);
        }
    }

    private void ShowHPUI()
    {
        if (hpUIRoot == null) return;

        if (!hpUIRoot.gameObject.activeSelf)
            hpUIRoot.gameObject.SetActive(true);

        // seed smoothing so first frame doesn't pop
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

    private void Die()
    {
        DropXP();

        // Announce death to listeners (cards, etc.)
        OnAnyEnemyDied?.Invoke(this);

        // Clean up UI (disable if pooling)
        if (hpUIRoot != null)
        {
            Destroy(hpUIRoot.gameObject);
        }

        Destroy(gameObject);
    }

    private void DropXP()
    {
        if (xpCoinPrefab == null) return;

        for (int i = 0; i < coinsToDrop; i++)
        {
            // NOTE: use the alias so we call Unity's Random, not System.Random
            Vector2 offset = URandom.insideUnitCircle * dropScatterRadius;
            Vector3 spawnPos = transform.position + (Vector3)offset;
            Instantiate(xpCoinPrefab, spawnPos, Quaternion.identity);
        }
    }

    // Optional: heal for testing
    public void Heal(float amount)
    {
        if (amount <= 0f || currentHP <= 0f) return;
        currentHP = Mathf.Min(currentHP + amount, maxHP);
        UpdateHPUI();

        // Show briefly, then auto-hide if full
        ShowHPUI();
    }

    // ===== Added: API to apply temporary extra damage taken =====
    /// <summary>
    /// Makes this enemy take more damage for a duration.
    /// multiplier = 1.25 means +25% damage taken.
    /// Duration stacks by time (extends), but multiplier is simply replaced.
    /// </summary>
    public void ApplyVulnerability(float multiplier, float duration)
    {
        _vulnMul = Mathf.Max(0.01f, multiplier);
        _vulnTimer = Mathf.Max(_vulnTimer, duration);
    }
}
