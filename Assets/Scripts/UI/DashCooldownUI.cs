using UnityEngine;
using UnityEngine.UI;

public class DashCooldownUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Slider slider;             // Assign your shaped slider here
    [SerializeField] private PlayerMovement movement;   // Assign PlayerMovement from the player
    [SerializeField] private PlayerStats stats;         // Assign PlayerStats from the player

    [Header("Behavior")]
    [SerializeField] private bool hideWhenReady = true;   // Hide bar when cooldown = 0
    [SerializeField] private bool fillFromFull = true;    // Full -> empty during cooldown
    [SerializeField] private bool forceShowOnStart = false; // DEBUG: show bar at Start

    private float cdTotal;
    private float cdRemaining;
    private bool active;

    private void Reset()
    {
        slider = GetComponentInChildren<Slider>();
    }

    private void Awake()
    {
        if (movement == null) movement = FindObjectOfType<PlayerMovement>();
        if (stats == null && movement != null) stats = movement.GetComponent<PlayerStats>();

        if (movement == null) Debug.LogWarning("[DashCooldownUI] PlayerMovement reference is missing.");
        if (stats == null) Debug.LogWarning("[DashCooldownUI] PlayerStats reference is missing.");
        if (slider == null) Debug.LogWarning("[DashCooldownUI] Slider reference is missing.");
    }

    private void OnEnable()
    {
        if (movement != null)
            movement.OnDashStarted += HandleDashStarted;

        SetupInitial();
    }

    private void OnDisable()
    {
        if (movement != null)
            movement.OnDashStarted -= HandleDashStarted;
    }

    private void SetupInitial()
    {
        cdTotal = stats != null ? stats.GetDashCooldown() : 2f;

        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
            SetVisual(0f); // ready
        }

        if (forceShowOnStart)
            SetVisible(true);
        else
            SetVisible(!hideWhenReady);
    }

    private void HandleDashStarted()
    {
        cdTotal = stats != null ? stats.GetDashCooldown() : 2f;
        cdRemaining = cdTotal;
        active = true;

        Debug.Log("[DashCooldownUI] Dash started: total cooldown = " + cdTotal);

        SetVisible(true);
        SetVisual(fillFromFull ? 1f : 0f);
    }

    private void Update()
    {
        if (!active) return;

        cdRemaining -= Time.deltaTime;
        if (cdRemaining <= 0f)
        {
            cdRemaining = 0f;
            active = false;
            SetVisual(0f);
            if (hideWhenReady) SetVisible(false);
            return;
        }

        float t = Mathf.Clamp01(cdRemaining / Mathf.Max(0.0001f, cdTotal));
        SetVisual(fillFromFull ? t : (1f - t));
    }

    private void SetVisual(float normalized)
    {
        if (slider != null)
            slider.value = Mathf.Clamp01(normalized);
    }

    private void SetVisible(bool v)
    {
        if (slider != null && slider.gameObject.activeSelf != v)
            slider.gameObject.SetActive(v);
    }

    // === DEBUG: Simulate a dash to test UI without input ===
    [ContextMenu("Kick Test Cooldown")]
    public void KickTestCooldown()
    {
        cdTotal = stats != null ? stats.GetDashCooldown() : 2f;
        cdRemaining = cdTotal;
        active = true;
        SetVisible(true);
        SetVisual(1f);
        Debug.Log("[DashCooldownUI] Test cooldown kicked for " + cdTotal + "s");
    }
}
