using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AbilityCooldownHUD : MonoBehaviour
{
    [System.Serializable]
    public class AbilitySlotUI
    {
        [Header("Button/Icon Objects")]
        public GameObject buttonIcon;
        public Image abilityIcon;
        public Image cooldownFill;
        public TextMeshProUGUI cooldownText;

        [Header("Optional Counter Text")]
        [Tooltip("Used for special abilities like Punishing Ground traps. Example: +2, +1.")]
        public TextMeshProUGUI counterText;

        [Header("Runtime")]
        [HideInInspector] public bool hasAbility;
        [HideInInspector] public Sprite icon;
        [HideInInspector] public float cooldown;
        [HideInInspector] public float cooldownRemaining;
        [HideInInspector] public int currentVariantIndex;
    }

    [Header("Target")]
    public PlayerControls playerControls;
    public AbilityHUDData abilityHUDData;
    public SummonEvolutionTracker summonTracker;

    [Header("Special Sync")]
    [Tooltip("Optional. If found, Ability 1 HUD uses real Sand Repulse cooldown.")]
    public SandRepulseAbility sandRepulseAbility;

    [Tooltip("Optional. If found, Ability 2 HUD uses real Hell Bomb cooldown/trap count.")]
    public PyroAbility2 pyroAbility2;

    [Header("Ability 1")]
    public AbilitySlotUI ability1;

    [Header("Ability 2")]
    public AbilitySlotUI ability2;

    [Header("Settings")]
    public bool hideTextWhenReady = true;
    public bool keepCooldownWhenVariantChanges = true;

    [Header("Debug")]
    public bool showDebug = true;

    private int _lastSummonLevel = -999;

    private void Awake()
    {
        CacheRefs();
    }

    private void Start()
    {
        CacheRefs();
        LoadAbilityData();
        RefreshPyroTrapCounter();
    }

    private void OnEnable()
    {
        CacheRefs();

        if (playerControls != null)
        {
            playerControls.OnAbility1 += HandleAbility1Pressed;
            playerControls.OnAbility2 += HandleAbility2Pressed;
        }

        if (pyroAbility2 != null)
        {
            pyroAbility2.OnTrapChargesChanged += HandlePyroTrapChargesChanged;
            pyroAbility2.OnCooldownChanged += HandlePyroAbility2CooldownChanged;
            pyroAbility2.BroadcastUI();
        }

        LoadAbilityData();
        RefreshPyroTrapCounter();
    }

    private void OnDisable()
    {
        if (playerControls != null)
        {
            playerControls.OnAbility1 -= HandleAbility1Pressed;
            playerControls.OnAbility2 -= HandleAbility2Pressed;
        }

        if (pyroAbility2 != null)
        {
            pyroAbility2.OnTrapChargesChanged -= HandlePyroTrapChargesChanged;
            pyroAbility2.OnCooldownChanged -= HandlePyroAbility2CooldownChanged;
        }
    }

    private void Update()
    {
        AutoUpdateVariants();

        SyncSandRepulseCooldown();

        if (sandRepulseAbility == null)
            UpdateSlot(ability1);
        else
            RefreshSlot(ability1);

        if (pyroAbility2 == null)
            UpdateSlot(ability2);
        else
            RefreshSlot(ability2);
    }

    private void CacheRefs()
    {
        if (playerControls == null)
            playerControls = GetComponentInParent<PlayerControls>();

        if (playerControls == null)
            playerControls = FindObjectOfType<PlayerControls>();

        if (abilityHUDData == null && playerControls != null)
            abilityHUDData = playerControls.GetComponent<AbilityHUDData>();

        if (abilityHUDData == null)
            abilityHUDData = GetComponentInParent<AbilityHUDData>();

        if (abilityHUDData == null)
            abilityHUDData = FindObjectOfType<AbilityHUDData>();

        if (summonTracker == null && playerControls != null)
            summonTracker = playerControls.GetComponent<SummonEvolutionTracker>();

        if (summonTracker == null)
            summonTracker = GetComponentInParent<SummonEvolutionTracker>();

        if (summonTracker == null)
            summonTracker = FindObjectOfType<SummonEvolutionTracker>();

        if (sandRepulseAbility == null && playerControls != null)
            sandRepulseAbility = playerControls.GetComponent<SandRepulseAbility>();

        if (sandRepulseAbility == null)
            sandRepulseAbility = GetComponentInParent<SandRepulseAbility>();

        if (pyroAbility2 == null && playerControls != null)
            pyroAbility2 = playerControls.GetComponent<PyroAbility2>();

        if (pyroAbility2 == null)
            pyroAbility2 = GetComponentInParent<PyroAbility2>();
    }

    private void LoadAbilityData()
    {
        CacheRefs();

        if (abilityHUDData == null)
        {
            if (showDebug)
                Debug.LogWarning("[AbilityCooldownHUD] No AbilityHUDData found.");

            ClearSlot(ability1);
            ClearSlot(ability2);
            return;
        }

        if (showDebug)
            Debug.Log($"[AbilityCooldownHUD] Loaded AbilityHUDData from: {abilityHUDData.name}");

        SetAbility1Variant(abilityHUDData.startingAbility1Variant);
        SetAbility2Variant(abilityHUDData.startingAbility2Variant);

        _lastSummonLevel = -999;
        AutoUpdateVariants();
    }

    private void AutoUpdateVariants()
    {
        if (abilityHUDData == null)
            return;

        if (summonTracker == null)
            CacheRefs();

        if (summonTracker == null)
            return;

        int level = summonTracker.currentLevel;

        if (level == _lastSummonLevel)
            return;

        _lastSummonLevel = level;

        int variantIndex = Mathf.Max(0, level - 1);

        if (abilityHUDData.ability1FollowsSummonLevel && abilityHUDData.Ability1HasMultipleVariants())
            SetAbility1Variant(variantIndex);

        if (abilityHUDData.ability2FollowsSummonLevel && abilityHUDData.Ability2HasMultipleVariants())
            SetAbility2Variant(variantIndex);
    }

    private void HandleAbility1Pressed()
    {
        // Sand Repulse has its own real cooldown.
        // HUD should not guess from input.
        if (sandRepulseAbility != null)
            return;

        TryStartCooldown(ability1);
    }

    private void HandleAbility2Pressed()
    {
        // Pyro Hell Bomb has its own real cooldown/trap charge logic.
        // HUD should not guess from input.
        if (pyroAbility2 != null)
            return;

        TryStartCooldown(ability2);
    }

    private void SyncSandRepulseCooldown()
    {
        if (sandRepulseAbility == null || ability1 == null || !ability1.hasAbility)
            return;

        ability1.cooldown = Mathf.Max(0.01f, sandRepulseAbility.GetCurrentCooldownDuration());
        ability1.cooldownRemaining = Mathf.Max(0f, sandRepulseAbility.GetCooldownRemaining());

        RefreshSlot(ability1);
    }

    private void HandlePyroTrapChargesChanged(int remaining, int max)
    {
        RefreshPyroTrapCounter();
    }

    private void HandlePyroAbility2CooldownChanged(float remaining, float duration)
    {
        if (ability2 == null)
            return;

        ability2.cooldown = Mathf.Max(0.01f, duration);
        ability2.cooldownRemaining = Mathf.Max(0f, remaining);

        RefreshSlot(ability2);
        RefreshPyroTrapCounter();
    }

    private void RefreshPyroTrapCounter()
    {
        if (ability2 == null || ability2.counterText == null)
            return;

        if (pyroAbility2 == null)
        {
            ability2.counterText.gameObject.SetActive(false);
            ability2.counterText.text = "";
            return;
        }

        bool showCounter =
            pyroAbility2.HasTrapCounter() &&
            !pyroAbility2.IsOnCooldown() &&
            ability2.hasAbility;

        if (!showCounter)
        {
            ability2.counterText.gameObject.SetActive(false);
            ability2.counterText.text = "";
            return;
        }

        int remaining = pyroAbility2.GetBombsRemainingBeforeCooldown();

        ability2.counterText.gameObject.SetActive(remaining > 0);
        ability2.counterText.text = remaining > 0 ? $"+{remaining}" : "";
    }

    public void SetAbility1Variant(int variantIndex)
    {
        if (abilityHUDData == null)
            CacheRefs();

        if (abilityHUDData == null)
            return;

        AbilityHUDData.AbilityHUDVariant variant = abilityHUDData.GetAbility1Variant(variantIndex);
        AssignVariant(ability1, variant, variantIndex, "Ability 1");
    }

    public void SetAbility2Variant(int variantIndex)
    {
        if (abilityHUDData == null)
            CacheRefs();

        if (abilityHUDData == null)
            return;

        AbilityHUDData.AbilityHUDVariant variant = abilityHUDData.GetAbility2Variant(variantIndex);
        AssignVariant(ability2, variant, variantIndex, "Ability 2");

        RefreshPyroTrapCounter();
    }

    private void AssignVariant(AbilitySlotUI slot, AbilityHUDData.AbilityHUDVariant variant, int variantIndex, string abilityName)
    {
        if (slot == null)
            return;

        float oldRemaining = slot.cooldownRemaining;

        if (variant == null)
        {
            if (showDebug)
                Debug.LogWarning($"[AbilityCooldownHUD] {abilityName} variant {variantIndex} is NULL.");

            ClearSlot(slot);
            return;
        }

        if (variant.icon == null)
        {
            if (showDebug)
                Debug.LogWarning($"[AbilityCooldownHUD] {abilityName} variant '{variant.variantName}' has NO ICON assigned.");

            ClearSlot(slot);
            return;
        }

        slot.currentVariantIndex = variantIndex;
        slot.icon = variant.icon;
        slot.cooldown = Mathf.Max(0.01f, variant.cooldown);
        slot.hasAbility = true;

        if (keepCooldownWhenVariantChanges)
            slot.cooldownRemaining = Mathf.Min(oldRemaining, slot.cooldown);
        else
            slot.cooldownRemaining = 0f;

        if (showDebug)
        {
            Debug.Log(
                $"[AbilityCooldownHUD] Loaded {abilityName}: " +
                $"Variant '{variant.variantName}', Icon '{variant.icon.name}', Cooldown {slot.cooldown}"
            );
        }

        RefreshSlot(slot);
    }

    private void TryStartCooldown(AbilitySlotUI slot)
    {
        if (slot == null || !slot.hasAbility)
            return;

        if (slot.cooldownRemaining > 0f)
            return;

        slot.cooldownRemaining = Mathf.Max(0.01f, slot.cooldown);
        RefreshSlot(slot);
    }

    private void UpdateSlot(AbilitySlotUI slot)
    {
        if (slot == null || !slot.hasAbility)
            return;

        if (slot.cooldownRemaining <= 0f)
        {
            slot.cooldownRemaining = 0f;
            RefreshSlot(slot);
            return;
        }

        slot.cooldownRemaining -= Time.deltaTime;

        if (slot.cooldownRemaining < 0f)
            slot.cooldownRemaining = 0f;

        RefreshSlot(slot);
    }

    private void ClearSlot(AbilitySlotUI slot)
    {
        if (slot == null)
            return;

        slot.hasAbility = false;
        slot.icon = null;
        slot.cooldown = 0f;
        slot.cooldownRemaining = 0f;
        slot.currentVariantIndex = 0;

        if (slot.counterText != null)
        {
            slot.counterText.gameObject.SetActive(false);
            slot.counterText.text = "";
        }

        RefreshSlot(slot);
    }

    private void RefreshSlot(AbilitySlotUI slot)
    {
        if (slot == null)
            return;

        if (slot.buttonIcon != null)
            slot.buttonIcon.SetActive(slot.hasAbility);

        if (slot.abilityIcon != null)
        {
            slot.abilityIcon.enabled = slot.hasAbility;
            slot.abilityIcon.sprite = slot.icon;
        }

        bool onCooldown = slot.hasAbility && slot.cooldownRemaining > 0f;

        if (slot.cooldownFill != null)
        {
            slot.cooldownFill.gameObject.SetActive(slot.hasAbility);
            slot.cooldownFill.enabled = onCooldown;

            if (slot.cooldown > 0f)
                slot.cooldownFill.fillAmount = slot.cooldownRemaining / slot.cooldown;
            else
                slot.cooldownFill.fillAmount = 0f;
        }

        if (slot.cooldownText != null)
        {
            if (!slot.hasAbility)
            {
                slot.cooldownText.gameObject.SetActive(false);
                slot.cooldownText.text = "";
                return;
            }

            if (onCooldown)
            {
                slot.cooldownText.gameObject.SetActive(true);
                slot.cooldownText.text = Mathf.CeilToInt(slot.cooldownRemaining).ToString();
                return;
            }

            slot.cooldownText.gameObject.SetActive(!hideTextWhenReady);
            slot.cooldownText.text = "";
        }
    }

    public void ForceReload()
    {
        abilityHUDData = null;
        playerControls = null;
        summonTracker = null;
        sandRepulseAbility = null;
        pyroAbility2 = null;
        _lastSummonLevel = -999;

        CacheRefs();
        LoadAbilityData();
        RefreshPyroTrapCounter();
    }
}