using UnityEngine;
using System.Collections.Generic;

public class GolemFireCircle : MonoBehaviour
{
    public static GolemFireCircle activeCircle;

    [Header("Circle Settings")]
    public float duration = 5f;
    [Tooltip("Radius for DoT damage to enemies")]
    public float radius = 5f;
    [Tooltip("Radius for player shield protection")]
    public float shieldRadius = 5f;
    public float shieldAmount = 100f;
    public float dotDamagePerSecond = 15f;

    [Header("References")]
    public Transform golem;
    public Transform player;
    public LayerMask enemyLayers;
    public SpriteRenderer circleSprite;
    public Animator circleAnimator;

    [Header("Shield Visual")]
    public GameObject playerShieldVisual;

    [Header("Visual Effects")]
    public bool pulseEffect = true;
    public float pulseSpeed = 2f;
    public Vector2 pulseScale = new Vector2(0.95f, 1.05f);
    [Tooltip("Y offset from golem position (negative = below)")]
    public float yOffset = -0.5f;

    [Header("Fade Out")]
    public bool fadeOut = true;
    public float fadeOutDuration = 1f;

    [Header("Debug")]
    public bool showDebug = true;

    public float currentShield { get; private set; }

    private float timer;
    private float dotTickInterval = 0.5f;
    private Dictionary<Transform, float> enemyDotTimers = new Dictionary<Transform, float>();
    private Vector3 originalScale;
    private float pulseTimer;
    private Color originalColor;
    private bool isFadingOut;
    private Vector3 shieldOriginalScale;
    private float shieldPulseTimer;
    private bool playerHasShield;

    private void Awake()
    {
        if (!player)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj)
            {
                player = playerObj.transform;
            }
        }

        if (!golem)
        {
            golem = transform.parent;
        }

        currentShield = shieldAmount;
        activeCircle = this;
        playerHasShield = false;

        if (circleSprite)
        {
            originalColor = circleSprite.color;
            originalScale = transform.localScale;
        }

        if (golem)
        {
            Vector3 newPos = golem.position;
            newPos.y += yOffset;
            transform.position = newPos;
        }

        if (circleAnimator)
        {
            circleAnimator.SetTrigger("Spawn");
        }

        if (playerShieldVisual)
        {
            shieldOriginalScale = playerShieldVisual.transform.localScale;
            playerShieldVisual.SetActive(false);
        }

        if (showDebug) Debug.Log($"[GolemFireCircle] Created at {transform.position}! Shield: {shieldAmount}, DoT Radius: {radius}, Shield Radius: {shieldRadius}");
    }

    private void Start()
    {
        if (!playerShieldVisual && player)
        {
            var pyroPlayer = player.GetComponent<PyroAbility1>();
            if (pyroPlayer && pyroPlayer.shieldVisual)
            {
                playerShieldVisual = pyroPlayer.shieldVisual;
                shieldOriginalScale = playerShieldVisual.transform.localScale;
                playerShieldVisual.SetActive(false);
            }
        }
    }

    private void Update()
    {
        if (golem)
        {
            Vector3 newPos = golem.position;
            newPos.y += yOffset;
            transform.position = newPos;
        }

        timer += Time.deltaTime;

        if (timer >= duration && !isFadingOut)
        {
            StartFadeOut();
        }

        if (isFadingOut)
        {
            UpdateFadeOut();
        }
        else
        {
            if (pulseEffect && circleSprite)
            {
                UpdatePulse();
            }

            ApplyDotToEnemies();
            UpdatePlayerShield();
        }
    }

    private void UpdatePulse()
    {
        pulseTimer += Time.deltaTime * pulseSpeed;
        float pulseValue = Mathf.Lerp(pulseScale.x, pulseScale.y, (Mathf.Sin(pulseTimer) + 1f) * 0.5f);
        transform.localScale = originalScale * pulseValue;
    }

    private void UpdatePlayerShield()
    {
        if (!player || !playerShieldVisual) return;

        bool isPlayerInside = IsPlayerInCircle();

        if (showDebug && Time.frameCount % 30 == 0)
        {
            float dist = Vector2.Distance(new Vector2(transform.position.x, transform.position.y),
                                         new Vector2(player.position.x, player.position.y));
            Debug.Log($"[GolemFireCircle] Distance: {dist:F2}, Shield Radius: {shieldRadius}, Inside: {isPlayerInside}, Circle: {transform.position}, Player: {player.position}");
        }

        if (isPlayerInside)
        {
            if (!playerHasShield)
            {
                currentShield = shieldAmount;
                playerHasShield = true;
                playerShieldVisual.SetActive(true);
                if (showDebug) Debug.Log($"[GolemFireCircle] Player GAINED shield: {shieldAmount}");
            }

            shieldPulseTimer += Time.deltaTime * 3f;
            float pulseValue = Mathf.Lerp(0.95f, 1.05f, (Mathf.Sin(shieldPulseTimer) + 1f) * 0.5f);
            playerShieldVisual.transform.localScale = shieldOriginalScale * pulseValue;
        }
        else
        {
            if (playerHasShield)
            {
                currentShield = 0f;
                playerHasShield = false;
                playerShieldVisual.SetActive(false);
                playerShieldVisual.transform.localScale = shieldOriginalScale;
                if (showDebug) Debug.Log("[GolemFireCircle] Player LEFT shield area - shield removed");
            }
        }
    }

    private void StartFadeOut()
    {
        isFadingOut = true;
        timer = 0f;

        if (playerHasShield)
        {
            currentShield = 0f;
            playerHasShield = false;
        }

        if (playerShieldVisual && playerShieldVisual.activeSelf)
        {
            playerShieldVisual.SetActive(false);
            playerShieldVisual.transform.localScale = shieldOriginalScale;
        }

        if (circleAnimator)
        {
            circleAnimator.SetTrigger("FadeOut");
        }

        if (showDebug) Debug.Log("[GolemFireCircle] Starting fade out");
    }

    private void UpdateFadeOut()
    {
        if (!fadeOut || !circleSprite)
        {
            Destroy(gameObject);
            return;
        }

        float fadeProgress = timer / fadeOutDuration;
        Color color = originalColor;
        color.a = Mathf.Lerp(originalColor.a, 0f, fadeProgress);
        circleSprite.color = color;

        if (fadeProgress >= 1f)
        {
            Destroy(gameObject);
        }
    }

    private void ApplyDotToEnemies()
    {
        Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, radius, enemyLayers);

        List<Transform> currentEnemies = new List<Transform>();

        foreach (var enemy in enemies)
        {
            if (!enemy) continue;

            currentEnemies.Add(enemy.transform);

            if (!enemyDotTimers.ContainsKey(enemy.transform))
            {
                enemyDotTimers[enemy.transform] = 0f;
            }

            enemyDotTimers[enemy.transform] += Time.deltaTime;

            if (enemyDotTimers[enemy.transform] >= dotTickInterval)
            {
                enemyDotTimers[enemy.transform] = 0f;

                var enemyHealth = enemy.GetComponent<EnemyHealth>();
                if (enemyHealth)
                {
                    float damage = dotDamagePerSecond * dotTickInterval;
                    enemyHealth.TakeDamage(damage);

                    if (showDebug) Debug.Log($"[GolemFireCircle] DoT tick: {damage} to {enemy.name}");
                }
            }
        }

        List<Transform> toRemove = new List<Transform>();
        foreach (var kvp in enemyDotTimers)
        {
            if (!currentEnemies.Contains(kvp.Key))
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var enemy in toRemove)
        {
            enemyDotTimers.Remove(enemy);
        }
    }

    public bool IsPlayerInCircle()
    {
        if (!player) return false;
        float distance = Vector2.Distance(new Vector2(transform.position.x, transform.position.y),
                                         new Vector2(player.position.x, player.position.y));
        return distance <= shieldRadius;
    }

    public float AbsorbDamage(float incomingDamage)
    {
        if (!playerHasShield || currentShield <= 0f) return incomingDamage;

        float absorbed = Mathf.Min(currentShield, incomingDamage);
        currentShield -= absorbed;
        float remaining = incomingDamage - absorbed;

        if (showDebug) Debug.Log($"[GolemFireCircle] Shield absorbed {absorbed} damage. Shield remaining: {currentShield}");

        if (currentShield <= 0f)
        {
            currentShield = 0f;
            playerHasShield = false;

            if (playerShieldVisual && playerShieldVisual.activeSelf)
            {
                playerShieldVisual.SetActive(false);
                playerShieldVisual.transform.localScale = shieldOriginalScale;
            }

            if (showDebug) Debug.Log("[GolemFireCircle] Shield depleted by damage!");
        }

        return Mathf.Max(0f, remaining);
    }

    private void OnDestroy()
    {
        if (activeCircle == this)
        {
            activeCircle = null;

            if (playerShieldVisual && playerShieldVisual.activeSelf)
            {
                playerShieldVisual.SetActive(false);
                playerShieldVisual.transform.localScale = shieldOriginalScale;
            }

            if (showDebug) Debug.Log("[GolemFireCircle] Circle destroyed, shield removed");
        }
    }

    private void OnDrawGizmos()
    {
        Vector3 center = transform.position;

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
        Gizmos.DrawWireSphere(center, radius);

        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawWireSphere(center, shieldRadius);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = transform.position;

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawWireSphere(center, radius);

        Gizmos.color = new Color(0f, 1f, 1f, 0.6f);
        Gizmos.DrawWireSphere(center, shieldRadius);

        if (Application.isPlaying && player)
        {
            float distance = Vector2.Distance(new Vector2(center.x, center.y),
                                            new Vector2(player.position.x, player.position.y));
            bool inShieldRange = distance <= shieldRadius;

            Gizmos.color = inShieldRange ? Color.green : Color.red;
            Gizmos.DrawLine(center, player.position);

            Gizmos.color = inShieldRange ? new Color(0f, 1f, 0f, 0.8f) : new Color(1f, 0f, 0f, 0.8f);
            Gizmos.DrawSphere(player.position, 0.5f);

            UnityEditor.Handles.Label(player.position + Vector3.up * 2f,
                $"Distance: {distance:F2}\nRadius: {shieldRadius}\nInside: {inShieldRange}");
        }
    }
}
