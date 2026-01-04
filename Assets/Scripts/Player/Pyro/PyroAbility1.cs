using UnityEngine;

public class PyroAbility1 : MonoBehaviour
{
    [Header("References")]
    private PlayerControls controls;
    private PlayerStats stats;
    private PlayerHealth health;
    private PlayerMovement movement;
    private SummonEvolutionTracker summonTracker;
    private Rigidbody2D rb;
    private Collider2D playerCollider;

    [Header("Cooldown")]
    public float baseCooldown = 8f;
    private float cooldownTimer;

    [Header("Level 1: Fire Spirit - Blazing Rush")]
    public float rushDuration = 3f;
    public float rushSpeedMultiplier = 2.5f;
    public LayerMask enemyLayers;
    public Color rushVisualTint = new Color(1f, 0.5f, 0f, 1f);

    [Header("Level 2: Fire Eagle - Dual Barrage")]
    public float barrageDuration = 5f;

    [Header("Shield Visual")]
    [Tooltip("Shield sprite GameObject (child of player)")]
    public GameObject shieldVisual;

    [Tooltip("Pulse the shield while active")]
    public bool enableShieldPulse = true;

    [Tooltip("Shield pulse speed")]
    public float shieldPulseSpeed = 3f;

    [Tooltip("Shield pulse scale range (min/max)")]
    public Vector2 shieldPulseScale = new Vector2(0.95f, 1.05f);

    [Header("Debug")]
    public bool showDebug = true;

    private bool isRushing;
    private float rushTimer;
    private SpriteRenderer playerSprite;
    private Color originalColor;
    private int originalLayer;
    private Vector3 shieldOriginalScale;
    private float shieldPulseTimer;

    private void Awake()
    {
        controls = GetComponent<PlayerControls>();
        stats = GetComponent<PlayerStats>();
        health = GetComponent<PlayerHealth>();
        movement = GetComponent<PlayerMovement>();
        summonTracker = GetComponent<SummonEvolutionTracker>();
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        playerSprite = GetComponentInChildren<SpriteRenderer>();

        if (playerSprite)
        {
            originalColor = playerSprite.color;
        }

        originalLayer = gameObject.layer;

        if (shieldVisual)
        {
            shieldOriginalScale = shieldVisual.transform.localScale;
            shieldVisual.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (controls != null)
        {
            controls.OnAbility1 += OnAbility1Pressed;
        }
    }

    private void OnDisable()
    {
        if (controls != null)
        {
            controls.OnAbility1 -= OnAbility1Pressed;
        }

        if (isRushing)
        {
            EndBlazingRush();
        }
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }

        if (isRushing)
        {
            rushTimer -= Time.deltaTime;

            UpdateShieldVisual();

            if (rushTimer <= 0f)
            {
                EndBlazingRush();
            }
        }
    }

    private void UpdateShieldVisual()
    {
        if (!shieldVisual || !enableShieldPulse) return;

        shieldPulseTimer += Time.deltaTime * shieldPulseSpeed;
        float pulseValue = Mathf.Lerp(shieldPulseScale.x, shieldPulseScale.y, (Mathf.Sin(shieldPulseTimer) + 1f) * 0.5f);
        shieldVisual.transform.localScale = shieldOriginalScale * pulseValue;
    }

    private void OnAbility1Pressed()
    {
        if (cooldownTimer > 0f)
        {
            if (showDebug) Debug.Log($"[PyroAbility1] On cooldown: {cooldownTimer:F1}s remaining");
            return;
        }

        if (!summonTracker)
        {
            if (showDebug) Debug.LogWarning("[PyroAbility1] No SummonEvolutionTracker found!");
            return;
        }

        int currentLevel = summonTracker.currentLevel;

        switch (currentLevel)
        {
            case 1:
                ActivateBlazingRush();
                break;
            case 2:
                ActivateDualBarrage();
                break;
            case 3:
                if (showDebug) Debug.Log("[PyroAbility1] Dog ability - Not implemented yet");
                break;
            case 4:
                if (showDebug) Debug.Log("[PyroAbility1] Golem ability - Not implemented yet");
                break;
            default:
                if (showDebug) Debug.Log("[PyroAbility1] No summon active!");
                break;
        }
    }

    private void ActivateBlazingRush()
    {
        if (isRushing) return;

        isRushing = true;
        rushTimer = rushDuration;
        shieldPulseTimer = 0f;

        float cd = baseCooldown * (stats ? stats.GetCooldownMultiplier() : 1f);
        cooldownTimer = cd;

        IgnoreEnemyCollisions(true);

        if (playerSprite)
        {
            playerSprite.color = rushVisualTint;
        }

        if (shieldVisual)
        {
            shieldVisual.SetActive(true);
        }

        if (showDebug)
        {
            Debug.Log($"[PyroAbility1] Blazing Rush! Speed: x{rushSpeedMultiplier}, Duration: {rushDuration}s - IMMUNE TO DAMAGE!");
        }
    }

    private void EndBlazingRush()
    {
        if (!isRushing) return;

        isRushing = false;

        IgnoreEnemyCollisions(false);

        if (playerSprite)
        {
            playerSprite.color = originalColor;
        }

        if (shieldVisual)
        {
            shieldVisual.SetActive(false);
            shieldVisual.transform.localScale = shieldOriginalScale;
        }

        if (showDebug) Debug.Log("[PyroAbility1] Blazing Rush ended!");
    }

    private void ActivateDualBarrage()
    {
        var eagle = summonTracker.GetCurrentSummon()?.GetComponent<FireEagle>();
        if (eagle == null)
        {
            if (showDebug) Debug.LogWarning("[PyroAbility1] No Fire Eagle found!");
            return;
        }

        eagle.ActivateEmpoweredMode(barrageDuration);

        float cd = baseCooldown * (stats ? stats.GetCooldownMultiplier() : 1f);
        cooldownTimer = cd;

        if (showDebug)
        {
            Debug.Log($"[PyroAbility1] Dual Barrage activated! Duration: {barrageDuration}s - Eagle shoots 2 fireballs!");
        }
    }

    private void IgnoreEnemyCollisions(bool ignore)
    {
        if (!playerCollider) return;

        Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, 50f, enemyLayers);

        foreach (var enemy in enemies)
        {
            if (enemy && enemy != playerCollider)
            {
                Physics2D.IgnoreCollision(playerCollider, enemy, ignore);
            }
        }

        if (ignore)
        {
            InvokeRepeating(nameof(UpdateEnemyCollisions), 0.5f, 0.5f);
        }
        else
        {
            CancelInvoke(nameof(UpdateEnemyCollisions));
        }
    }

    private void UpdateEnemyCollisions()
    {
        if (!isRushing) return;

        Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, 50f, enemyLayers);

        foreach (var enemy in enemies)
        {
            if (enemy && enemy != playerCollider)
            {
                Physics2D.IgnoreCollision(playerCollider, enemy, true);
            }
        }
    }

    public float GetSpeedMultiplier()
    {
        return isRushing ? rushSpeedMultiplier : 1f;
    }

    public bool IsRushing()
    {
        return isRushing;
    }

    public bool IsInvulnerable()
    {
        return isRushing;
    }

    public float GetCooldownNormalized()
    {
        if (baseCooldown <= 0f) return 0f;
        return Mathf.Clamp01(cooldownTimer / baseCooldown);
    }

    public float GetCooldownRemaining()
    {
        return Mathf.Max(0f, cooldownTimer);
    }
}
