using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ActiveCardHUD : MonoBehaviour
{
    [System.Serializable]
    public class ActiveCardSlotUI
    {
        [Header("Button Icon")]
        [Tooltip("The E/Q button icon object. This will hide when no card is assigned.")]
        public GameObject buttonIcon;

        [Header("Images")]
        public Image cardIcon;
        public Image cooldownFill;

        [Header("Text")]
        public TextMeshProUGUI cooldownText;

        [Header("Empty State")]
        public GameObject emptyState;

        [Header("Runtime")]
        [HideInInspector] public Sprite assignedIcon;
        [HideInInspector] public float cooldownDuration;
        [HideInInspector] public float cooldownRemaining;
        [HideInInspector] public bool hasCard;
        [HideInInspector] public bool isBusyNoTimer;

        [HideInInspector] public bool useCounterMode;
        [HideInInspector] public int counterValue;
    }

    [Header("Active Card 1 = E Icon Slot")]
    public ActiveCardSlotUI activeCard1_E;

    [Header("Active Card 2 = Q Icon Slot")]
    public ActiveCardSlotUI activeCard2_Q;

    [Header("Settings")]
    public bool hideCooldownTextWhenReady = true;

    private void Start()
    {
        RefreshSlot(activeCard1_E);
        RefreshSlot(activeCard2_Q);
    }

    private void Update()
    {
        UpdateCooldown(activeCard1_E);
        UpdateCooldown(activeCard2_Q);
    }

    // ── Assign Cards ─────────────────────────────────────────

    public void AssignActiveCard1(Sprite cardSprite, float cooldownDuration)
    {
        AssignSlot(activeCard1_E, cardSprite, cooldownDuration);
    }

    public void AssignActiveCard2(Sprite cardSprite, float cooldownDuration)
    {
        AssignSlot(activeCard2_Q, cardSprite, cooldownDuration);
    }

    public void ClearActiveCard1()
    {
        ClearSlot(activeCard1_E);
    }

    public void ClearActiveCard2()
    {
        ClearSlot(activeCard2_Q);
    }

    private void AssignSlot(ActiveCardSlotUI slot, Sprite cardSprite, float cooldownDuration)
    {
        if (slot == null)
            return;

        slot.assignedIcon = cardSprite;
        slot.cooldownDuration = Mathf.Max(0.01f, cooldownDuration);
        slot.cooldownRemaining = 0f;
        slot.hasCard = cardSprite != null;
        slot.isBusyNoTimer = false;
        slot.useCounterMode = false;
        slot.counterValue = 0;

        RefreshSlot(slot);
    }

    private void ClearSlot(ActiveCardSlotUI slot)
    {
        if (slot == null)
            return;

        slot.assignedIcon = null;
        slot.cooldownDuration = 0f;
        slot.cooldownRemaining = 0f;
        slot.hasCard = false;
        slot.isBusyNoTimer = false;
        slot.useCounterMode = false;
        slot.counterValue = 0;

        RefreshSlot(slot);
    }

    // ── Busy State: gray, no timer ───────────────────────────

    public void SetActiveCard1Busy(bool busy)
    {
        SetBusy(activeCard1_E, busy);
    }

    public void SetActiveCard2Busy(bool busy)
    {
        SetBusy(activeCard2_Q, busy);
    }

    private void SetBusy(ActiveCardSlotUI slot, bool busy)
    {
        if (slot == null || !slot.hasCard)
            return;

        slot.useCounterMode = false;
        slot.isBusyNoTimer = busy;

        if (busy)
            slot.cooldownRemaining = 0f;

        RefreshSlot(slot);
    }

    // ── Counter Mode: used by Will-o'-the-wisp ───────────────

    public void SetActiveCard1Counter(int value)
    {
        SetCounter(activeCard1_E, value);
    }

    public void SetActiveCard2Counter(int value)
    {
        SetCounter(activeCard2_Q, value);
    }

    public void ClearActiveCard1Counter()
    {
        ClearCounter(activeCard1_E);
    }

    public void ClearActiveCard2Counter()
    {
        ClearCounter(activeCard2_Q);
    }

    private void SetCounter(ActiveCardSlotUI slot, int value)
    {
        if (slot == null || !slot.hasCard)
            return;

        slot.useCounterMode = true;
        slot.counterValue = Mathf.Clamp(value, 0, 3);
        slot.cooldownRemaining = 0f;

        // 0 wisps = gray
        // 1+ wisps = normal icon with +number
        slot.isBusyNoTimer = slot.counterValue <= 0;

        RefreshSlot(slot);
    }

    private void ClearCounter(ActiveCardSlotUI slot)
    {
        if (slot == null)
            return;

        slot.useCounterMode = false;
        slot.counterValue = 0;
        slot.isBusyNoTimer = false;

        RefreshSlot(slot);
    }

    // ── Cooldown ─────────────────────────────────────────────

    public void StartCooldownActive1()
    {
        StartCooldown(activeCard1_E);
    }

    public void StartCooldownActive2()
    {
        StartCooldown(activeCard2_Q);
    }

    public void StartCooldownActive1(float duration)
    {
        StartCooldown(activeCard1_E, duration);
    }

    public void StartCooldownActive2(float duration)
    {
        StartCooldown(activeCard2_Q, duration);
    }

    private void StartCooldown(ActiveCardSlotUI slot)
    {
        if (slot == null || !slot.hasCard)
            return;

        slot.useCounterMode = false;
        slot.isBusyNoTimer = false;
        slot.cooldownRemaining = slot.cooldownDuration;

        RefreshSlot(slot);
    }

    private void StartCooldown(ActiveCardSlotUI slot, float duration)
    {
        if (slot == null || !slot.hasCard)
            return;

        slot.useCounterMode = false;
        slot.isBusyNoTimer = false;
        slot.cooldownDuration = Mathf.Max(0.01f, duration);
        slot.cooldownRemaining = slot.cooldownDuration;

        RefreshSlot(slot);
    }

    private void UpdateCooldown(ActiveCardSlotUI slot)
    {
        if (slot == null || !slot.hasCard)
            return;

        if (slot.useCounterMode)
        {
            RefreshSlot(slot);
            return;
        }

        if (slot.isBusyNoTimer)
        {
            RefreshSlot(slot);
            return;
        }

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

    // ── Visual Refresh ───────────────────────────────────────

    private void RefreshSlot(ActiveCardSlotUI slot)
    {
        if (slot == null)
            return;

        // Hide/show the E/Q button icon.
        if (slot.buttonIcon != null)
            slot.buttonIcon.SetActive(slot.hasCard);

        if (slot.cardIcon != null)
        {
            slot.cardIcon.enabled = slot.hasCard;
            slot.cardIcon.sprite = slot.assignedIcon;
        }

        if (slot.emptyState != null)
            slot.emptyState.SetActive(!slot.hasCard);

        bool onCooldown = slot.hasCard && slot.cooldownRemaining > 0f;
        bool showOverlay = slot.hasCard && (onCooldown || slot.isBusyNoTimer);

        if (slot.cooldownFill != null)
        {
            slot.cooldownFill.gameObject.SetActive(slot.hasCard);
            slot.cooldownFill.enabled = showOverlay;

            if (!slot.hasCard)
            {
                slot.cooldownFill.fillAmount = 0f;
            }
            else if (slot.isBusyNoTimer)
            {
                slot.cooldownFill.fillAmount = 1f;
            }
            else if (slot.cooldownDuration > 0f)
            {
                slot.cooldownFill.fillAmount = slot.cooldownRemaining / slot.cooldownDuration;
            }
            else
            {
                slot.cooldownFill.fillAmount = 0f;
            }
        }

        if (slot.cooldownText != null)
        {
            if (!slot.hasCard)
            {
                slot.cooldownText.gameObject.SetActive(false);
                slot.cooldownText.text = "";
                return;
            }

            if (slot.useCounterMode)
            {
                if (slot.counterValue > 0)
                {
                    slot.cooldownText.gameObject.SetActive(true);
                    slot.cooldownText.text = $"+{slot.counterValue}";
                }
                else
                {
                    slot.cooldownText.gameObject.SetActive(false);
                    slot.cooldownText.text = "";
                }

                return;
            }

            if (slot.isBusyNoTimer)
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

            slot.cooldownText.gameObject.SetActive(!hideCooldownTextWhenReady);
            slot.cooldownText.text = "";
        }
    }
}