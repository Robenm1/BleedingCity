using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[RequireComponent(typeof(PlayerStats))]
public class DuneTimeAbility : MonoBehaviour
{
    [Header("Input (Optional)")]
    public InputActionReference abilityAction; // hook to Ability2, or call Activate()

    [Header("Area & Slow")]
    public float radius = 4.5f;
    [Range(0.1f, 1f)] public float enemySlowFactor = 0.4f; // 0.4 = 60% slow
    public float duration = 4f;
    public LayerMask enemyLayers;

    [Header("Shark Overdrive")]
    public float sharkSpeedMultiplier = 1.35f;
    public float sharkDamageMultiplier = 1.25f;

    [Header("Cooldown")]
    public float baseCooldown = 18f;

    [Header("FX (optional)")]
    public GameObject castVfxPrefab;

    [Header("UI / Debug")]
    [Tooltip("If ON, the slow zone shows a semi-transparent area while active.")]
    public bool showCircle = true;

    [Header("Zone Prefab (optional)")]
    [Tooltip("Optional prefab that already has a SlowZone + visuals configured. If empty, we create one at runtime.")]
    public SlowZone slowZonePrefab;

    private PlayerStats stats;
    private DezzoSharkManager sharkMgr;
    private float cdTimer = 0f;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
        sharkMgr = GetComponent<DezzoSharkManager>();
        if (sharkMgr == null) Debug.LogWarning("[DuneTimeAbility] DezzoSharkManager not found on Dezzo.");
    }

    private void OnEnable()
    {
        if (abilityAction != null && abilityAction.action != null)
            abilityAction.action.performed += OnPerformed;
    }

    private void OnDisable()
    {
        if (abilityAction != null && abilityAction.action != null)
            abilityAction.action.performed -= OnPerformed;
    }

    private void Update()
    {
        if (cdTimer > 0f)
        {
            cdTimer -= Time.deltaTime;
            if (cdTimer < 0f) cdTimer = 0f;
        }
    }

    private void OnPerformed(InputAction.CallbackContext ctx) => Activate();

    /// Call from your PlayerControls when Ability2 is pressed if you’re not using InputActionReference.
    public void Activate()
    {
        if (cdTimer > 0f) return;

        // cooldown respects PlayerStats cooldownMultiplier (1=normal, 0.8=faster)
        float cd = baseCooldown * Mathf.Max(0.05f, stats.GetCooldownMultiplier());
        cdTimer = cd;

        if (castVfxPrefab) Instantiate(castVfxPrefab, transform.position, Quaternion.identity);

        // --- Spawn a stationary slow zone at cast position ---
        SpawnSlowZone();

        // Overdrive sharks for the same duration
        if (sharkMgr != null)
            sharkMgr.StartOverdrive(duration, sharkSpeedMultiplier, sharkDamageMultiplier);
    }

    private void SpawnSlowZone()
    {
        Vector3 pos = transform.position;

        SlowZone zoneInstance = null;

        if (slowZonePrefab != null)
        {
            zoneInstance = Instantiate(slowZonePrefab, pos, Quaternion.identity);
            // Ensure parameters match current ability settings
            zoneInstance.Init(radius, enemySlowFactor, duration, enemyLayers, showCircle);
        }
        else
        {
            // Create a simple zone at runtime if no prefab given
            var go = new GameObject("DuneTime_SlowZone");
            go.transform.position = pos;
            zoneInstance = go.AddComponent<SlowZone>();
            zoneInstance.Init(radius, enemySlowFactor, duration, enemyLayers, showCircle);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.85f, 0.75f, 0.2f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
