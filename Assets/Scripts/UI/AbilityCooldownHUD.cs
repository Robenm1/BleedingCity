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

        [Header("Runtime")]
        [HideInInspector] public bool hasAbility;
        [HideInInspector] public Sprite icon;
        [HideInInspector] public float cooldown;
        [HideInInspector] public float cooldownRemaining;
        [HideInInspector] public int currentVariantIndex;
    }

    [Header("Target")]
    [Tooltip("If empty, this script searches in parent first, then scene.")]
    public PlayerControls playerControls;

    [Tooltip("If empty, this script searches on the player/character.")]
    public AbilityHUDData abilityHUDData;

    [Header("Ability 1")]
    public AbilitySlotUI ability1;

    [Header("Ability 2")]
    public AbilitySlotUI ability2;

    [Header("Settings")]
    public bool hideTextWhenReady = true;

    [Tooltip("If true, changing variant does not reset the current cooldown timer.")]
    public bool keepCooldownWhenVariantChanges = true;

    private void Awake()
    {
        CacheRefs();
        LoadAbilityData();
    }

    private void OnEnable()
    {
        CacheRefs();

        if (playerControls != null)
        {
            playerControls.OnAbility1 += HandleAbility1Pressed;
            playerControls.OnAbility2 += HandleAbility2Pressed;
        }

        LoadAbilityData();
    }

    private void OnDisable()
    {
        if (playerControls != null)
        {
            playerControls.OnAbility1 -= HandleAbility1Pressed;
            playerControls.OnAbility2 -= HandleAbility2Pressed;
        }
    }

    private void Update()
    {
        UpdateSlot(ability1);
        UpdateSlot(ability2);
    }

    private void CacheRefs()
    {
        if (playerControls == null)
            playerControls = GetComponentInParent<PlayerControls>();

        if (playerControls == null)
            playerControls = FindObjectOfType<PlayerControls>();

        if (abilityHUDData == null)
            abilityHUDData = GetComponentInParent<AbilityHUDData>();

        if (abilityHUDData == null && playerControls != null)
            abilityHUDData = playerControls.GetComponent<AbilityHUDData>();

        if (abilityHUDData == null)
            abilityHUDData = FindObjectOfType<AbilityHUDData>();
    }

    private void LoadAbilityData()
    {
        CacheRefs();

        if (abilityHUDData == null)
        {
            ClearSlot(ability1);
            ClearSlot(ability2);
            return;
        }

        SetAbility1Variant(abilityHUDData.startingAbility1Variant);
        SetAbility2Variant(abilityHUDData.startingAbility2Variant);
    }

    private void HandleAbility1Pressed()
    {
        TryStartCooldown(ability1);
    }

    private void HandleAbility2Pressed()
    {
        TryStartCooldown(ability2);
    }

    // ─────────────────────────────────────────────
    // Public Variant API
    // ─────────────────────────────────────────────

    public void SetAbility1Variant(int variantIndex)
    {
        if (abilityHUDData == null)
            CacheRefs();

        if (abilityHUDData == null)
            return;

        AbilityHUDData.AbilityHUDVariant variant = abilityHUDData.GetAbility1Variant(variantIndex);
        AssignVariant(ability1, variant, variantIndex);
    }

    public void SetAbility2Variant(int variantIndex)
    {
        if (abilityHUDData == null)
            CacheRefs();

        if (abilityHUDData == null)
            return;

        AbilityHUDData.AbilityHUDVariant variant = abilityHUDData.GetAbility2Variant(variantIndex);
        AssignVariant(ability2, variant, variantIndex);
    }

    public void SetAbilityVariants(int ability1VariantIndex, int ability2VariantIndex)
    {
        SetAbility1Variant(ability1VariantIndex);
        SetAbility2Variant(ability2VariantIndex);
    }

    private void AssignVariant(AbilitySlotUI slot, AbilityHUDData.AbilityHUDVariant variant, int variantIndex)
    {
        if (slot == null)
            return;

        float oldRemaining = slot.cooldownRemaining;

        if (variant == null || variant.icon == null)
        {
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

        RefreshSlot(slot);
    }

    // ─────────────────────────────────────────────
    // Cooldown
    // ─────────────────────────────────────────────

    private void TryStartCooldown(AbilitySlotUI slot)
    {
        if (slot == null || !slot.hasAbility)
            return;

        // Pressing while on cooldown will not reset it.
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

    // ─────────────────────────────────────────────
    // Clear / Refresh
    // ─────────────────────────────────────────────

    private void ClearSlot(AbilitySlotUI slot)
    {
        if (slot == null)
            return;

        slot.hasAbility = false;
        slot.icon = null;
        slot.cooldown = 0f;
        slot.cooldownRemaining = 0f;
        slot.currentVariantIndex = 0;

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

        CacheRefs();
        LoadAbilityData();
    }
}