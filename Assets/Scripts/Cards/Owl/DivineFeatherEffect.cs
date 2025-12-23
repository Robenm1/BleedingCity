using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class DivineFeatherEffect : MonoBehaviour
{
    [Header("Trigger Thresholds (set by SO)")]
    public float triggerHpThreshold = 0.1f;
    public float shootHpThreshold = 0.5f;

    [Header("Feather Swarm (set by SO)")]
    public GameObject featherPrefab;
    public int featherCount = 8;
    public float orbitRadius = 2f;
    public float orbitSpeed = 90f;

    [Header("Protection (set by SO)")]
    public float damageReduction = 0.5f;
    public float healPerSecond = 5f;

    [Header("Recall (set by SO)")]
    public float recallSpeedMultiplier = 2f;
    public LayerMask enemyLayers;

    [Header("Visual (set by SO)")]
    public Color orbitColor = new Color(1f, 0.9f, 0.7f, 1f);

    [Header("Debug")]
    public bool enableDebugLogs = true;

    private PlayerHealth playerHealth;
    private PlayerStats playerStats;
    private PlayerControls playerControls;

    private bool hasTriggered = false;
    private bool isActive = false;
    private List<SnowOwlFeather> activeFeathers = new List<SnowOwlFeather>();
    private List<SpriteRenderer> featherRenderers = new List<SpriteRenderer>();
    private List<Vector2> featherDirections = new List<Vector2>();
    private float orbitAngle = 0f;
    private int ownerKey;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerStats = GetComponent<PlayerStats>();
        playerControls = GetComponent<PlayerControls>();

        if (!playerHealth || !playerStats)
        {
            if (enableDebugLogs) Debug.LogError("[DivineFeatherEffect] Missing PlayerHealth or PlayerStats!");
            enabled = false;
            return;
        }

        ownerKey = gameObject.GetInstanceID();
    }

    private void OnEnable()
    {
        if (playerControls != null)
        {
            playerControls.OnAbility1 += OnAbility1Pressed;
        }

        if (playerHealth != null)
        {
            playerHealth.OnDamaged += OnPlayerDamaged;
        }
    }

    private void OnDisable()
    {
        if (playerControls != null)
        {
            playerControls.OnAbility1 -= OnAbility1Pressed;
        }

        if (playerHealth != null)
        {
            playerHealth.OnDamaged -= OnPlayerDamaged;
        }

        DestroyAllFeathers();
    }

    private void Update()
    {
        if (hasTriggered) return;

        float hpPercent = playerStats.currentHealth / playerStats.maxHealth;

        if (hpPercent <= triggerHpThreshold)
        {
            TriggerDivineFeathers();
        }
    }

    private void FixedUpdate()
    {
        if (!isActive || activeFeathers.Count == 0) return;

        orbitAngle += orbitSpeed * Time.fixedDeltaTime;
        if (orbitAngle >= 360f) orbitAngle -= 360f;

        float angleStep = 360f / activeFeathers.Count;

        for (int i = activeFeathers.Count - 1; i >= 0; i--)
        {
            var feather = activeFeathers[i];
            if (feather == null)
            {
                activeFeathers.RemoveAt(i);
                if (i < featherRenderers.Count)
                    featherRenderers.RemoveAt(i);
                if (i < featherDirections.Count)
                    featherDirections.RemoveAt(i);
                continue;
            }

            float angle = (orbitAngle + (i * angleStep)) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * orbitRadius;
            feather.transform.position = transform.position + offset;
            feather.transform.right = offset.normalized;

            featherDirections[i] = offset.normalized;
        }

        float healAmount = healPerSecond * Time.fixedDeltaTime;
        playerStats.currentHealth = Mathf.Min(playerStats.currentHealth + healAmount, playerStats.maxHealth);

        float hpPercent = playerStats.currentHealth / playerStats.maxHealth;

        if (hpPercent >= shootHpThreshold)
        {
            if (enableDebugLogs) Debug.Log($"[DivineFeatherEffect] HP reached {shootHpThreshold * 100}%, auto-shooting feathers!");
            ShootAllFeathersOutward();
        }
    }

    private void OnPlayerDamaged(float damage)
    {
        if (!isActive || activeFeathers.Count == 0) return;

        float reducedDamage = damage * (1f - damageReduction);
        float damageBlocked = damage - reducedDamage;

        if (damageBlocked > 0f)
        {
            playerStats.currentHealth = Mathf.Min(playerStats.currentHealth + damageBlocked, playerStats.maxHealth);

            if (enableDebugLogs)
                Debug.Log($"[DivineFeatherEffect] Blocked {damageBlocked:F1} damage ({damageReduction * 100}% reduction)");
        }
    }

    private void TriggerDivineFeathers()
    {
        hasTriggered = true;
        isActive = true;

        if (enableDebugLogs)
            Debug.Log($"[DivineFeatherEffect] Triggering Divine Feathers at {triggerHpThreshold * 100}% HP! Current: {playerStats.currentHealth}/{playerStats.maxHealth}");

        if (!featherPrefab)
        {
            if (enableDebugLogs) Debug.LogWarning("[DivineFeatherEffect] Feather prefab not assigned!");
            return;
        }

        SpawnFeathers();
    }

    private void SpawnFeathers()
    {
        float angleStep = 360f / featherCount;

        for (int i = 0; i < featherCount; i++)
        {
            float angle = (i * angleStep) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * orbitRadius;
            Vector3 spawnPos = transform.position + offset;

            GameObject go = Instantiate(featherPrefab, spawnPos, Quaternion.identity);
            var feather = go.GetComponent<SnowOwlFeather>();

            if (feather != null)
            {
                Vector2 dir = offset.normalized;
                feather.Init(transform, ownerKey, dir, 0f, enemyLayers, playerStats);
                feather.enabled = false;

                var renderer = go.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.color = orbitColor;
                    featherRenderers.Add(renderer);
                }

                var rb = go.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.bodyType = RigidbodyType2D.Kinematic;
                    rb.linearVelocity = Vector2.zero;
                }

                activeFeathers.Add(feather);
                featherDirections.Add(dir);
            }
        }

        if (enableDebugLogs) Debug.Log($"[DivineFeatherEffect] Spawned {featherCount} orbiting feathers with {damageReduction * 100}% damage reduction (OwnerKey: {ownerKey})");
    }

    private void OnAbility1Pressed()
    {
        if (!isActive || activeFeathers.Count == 0) return;

        if (enableDebugLogs) Debug.Log("[DivineFeatherEffect] Ability1 pressed, manually shooting feathers outward!");
        ShootAllFeathersOutward();
    }

    private void ShootAllFeathersOutward()
    {
        if (!isActive) return;

        isActive = false;

        if (enableDebugLogs) Debug.Log($"[DivineFeatherEffect] Shooting {activeFeathers.Count} feathers outward! They will be recallable with Owl's recall ability.");

        for (int i = 0; i < activeFeathers.Count; i++)
        {
            var feather = activeFeathers[i];
            if (feather == null) continue;

            Vector2 shootDirection = featherDirections[i];
            float shootSpeed = feather.baseSpeed * recallSpeedMultiplier;

            feather.enabled = true;
            feather.Init(transform, ownerKey, shootDirection, shootSpeed, enemyLayers, playerStats);

            var renderer = feather.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = Color.white;
            }

            var rb = feather.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
            }
        }

        activeFeathers.Clear();
        featherRenderers.Clear();
        featherDirections.Clear();
    }

    private void DestroyAllFeathers()
    {
        foreach (var feather in activeFeathers)
        {
            if (feather != null)
            {
                Destroy(feather.gameObject);
            }
        }

        activeFeathers.Clear();
        featherRenderers.Clear();
        featherDirections.Clear();
    }
}
