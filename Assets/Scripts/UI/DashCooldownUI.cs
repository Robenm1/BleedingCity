using UnityEngine;
using UnityEngine.UI;

public class DashCooldownUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Slider slider;             // Assign your shaped slider here
    [SerializeField] private Image fillImage;           // Assign the slider Fill image here
    [SerializeField] private PlayerMovement movement;   // Assign PlayerMovement from the player
    [SerializeField] private PlayerStats stats;         // Assign PlayerStats from the player

    [Header("Colors")]
    [SerializeField] private Color cooldownColor = Color.white;
    [SerializeField] private Color extraDashWindowColor = new Color(0.1f, 0.55f, 1f, 1f);

    [Header("Behavior")]
    [SerializeField] private bool hideWhenReady = true;      // Hide bar when cooldown = 0
    [SerializeField] private bool fillFromFull = true;       // Full -> empty during cooldown/window
    [SerializeField] private bool forceShowOnStart = false;  // DEBUG: show bar at Start

    private void Reset()
    {
        slider = GetComponentInChildren<Slider>();

        if (slider != null && slider.fillRect != null)
            fillImage = slider.fillRect.GetComponent<Image>();
    }

    private void Awake()
    {
        if (movement == null) movement = FindObjectOfType<PlayerMovement>();
        if (stats == null && movement != null) stats = movement.GetComponent<PlayerStats>();

        if (slider == null) slider = GetComponentInChildren<Slider>();
        if (fillImage == null && slider != null && slider.fillRect != null)
            fillImage = slider.fillRect.GetComponent<Image>();

        if (movement == null) Debug.LogWarning("[DashCooldownUI] PlayerMovement reference is missing.");
        if (stats == null) Debug.LogWarning("[DashCooldownUI] PlayerStats reference is missing.");
        if (slider == null) Debug.LogWarning("[DashCooldownUI] Slider reference is missing.");
        if (fillImage == null) Debug.LogWarning("[DashCooldownUI] Fill Image reference is missing. Color change will not work.");
    }

    private void OnEnable()
    {
        SetupInitial();
    }

    private void SetupInitial()
    {
        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
            SetVisual(0f);
            SetColor(cooldownColor);
        }

        if (forceShowOnStart)
            SetVisible(true);
        else
            SetVisible(!hideWhenReady);
    }

    private void Update()
    {
        if (movement == null || slider == null) return;

        // Blue Out of the Ordinary window has priority over normal cooldown.
        if (movement.IsExtraDashWindowActive())
        {
            float t = movement.GetExtraDashWindowNormalized();

            SetVisible(true);
            SetColor(extraDashWindowColor);
            SetVisual(fillFromFull ? t : (1f - t));

            return;
        }

        float cdRemaining = movement.GetDashCooldownRemaining();
        float cdNormalized = movement.GetDashCooldownNormalized();

        if (cdRemaining > 0f)
        {
            SetVisible(true);
            SetColor(cooldownColor);
            SetVisual(fillFromFull ? cdNormalized : (1f - cdNormalized));

            return;
        }

        SetVisual(0f);
        SetColor(cooldownColor);

        if (hideWhenReady)
            SetVisible(false);
        else
            SetVisible(true);
    }

    private void SetVisual(float normalized)
    {
        if (slider != null)
            slider.value = Mathf.Clamp01(normalized);
    }

    private void SetColor(Color color)
    {
        if (fillImage != null)
            fillImage.color = color;
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
        if (slider == null) return;

        SetVisible(true);
        SetColor(cooldownColor);
        SetVisual(1f);

        Debug.Log("[DashCooldownUI] Test cooldown kicked visually.");
    }
}