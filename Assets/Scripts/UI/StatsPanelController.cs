using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

[RequireComponent(typeof(PlayerStats))]
public class StatsPanelController : MonoBehaviour
{
    /// <summary>Fired whenever the stats panel opens or closes. True = open, false = closed.</summary>
    public static event Action<bool> OnPanelToggled;

    [Header("Wiring")]
    public GameObject panelRoot;
    public InputActionReference statsMenuAction;
    public PlayerStats stats;
    public PlayerBuffInventory buffInventory;

    [Header("Cards Panel")]
    [Tooltip("Optional. Used to reset Cards UI back to Stats view when opening/closing the panel.")]
    public StatsCardsPanelSwitcher cardsPanelSwitcher;

    [Header("Input Blocking")]
    [Tooltip("If true, gameplay inputs are disabled while the stats panel is open.")]
    public bool disableGameplayInputsWhileOpen = true;

    [Tooltip("Optional. If empty, the script searches on the same player.")]
    public PlayerControls playerControls;

    [Tooltip("Optional manual list. If empty, the script auto-finds InputActionReference fields on PlayerControls.")]
    public InputActionReference[] gameplayActionsToDisable;

    [Tooltip("Delay before re-enabling gameplay inputs after closing the panel. This prevents the last held button from firing.")]
    public float reEnableInputDelay = 0.08f;

    [Header("HP Slider UI")]
    public Slider hpSlider;
    public TextMeshProUGUI hpSliderCenterText;

    [Header("Stat Lines (rendered as 'LABEL : value')")]
    public TextMeshProUGUI hpStatText;
    public TextMeshProUGUI atkStatText;
    public TextMeshProUGUI defStatText;

    [Header("Buff Pager")]
    public BuffSlotUI[] buffSlots = new BuffSlotUI[3];
    public Button leftArrowButton;
    public Button rightArrowButton;

    [Header("Slider Text Color Swap")]
    public RectTransform sliderFillRect;
    public RectTransform sliderTextRect;
    public Color onFillColor = Color.black;
    public Color offFillColor = Color.white;

    private bool isOpen = false;
    private int pageIndex = 0;
    private const int pageSize = 3;
    private float cachedPrevTimeScale = 1f;

    private readonly List<InputAction> _disabledGameplayActions = new List<InputAction>();
    private Coroutine _reenableInputRoutine;

    private void Reset()
    {
        stats = GetComponent<PlayerStats>();

        if (!buffInventory)
            buffInventory = GetComponent<PlayerBuffInventory>();

        if (!playerControls)
            playerControls = GetComponent<PlayerControls>();
    }

    private void Awake()
    {
        if (!stats)
            stats = GetComponent<PlayerStats>();

        if (!buffInventory)
            buffInventory = GetComponent<PlayerBuffInventory>();

        if (!playerControls)
            playerControls = GetComponent<PlayerControls>();

        if (!cardsPanelSwitcher && panelRoot)
            cardsPanelSwitcher = panelRoot.GetComponentInChildren<StatsCardsPanelSwitcher>(true);

        if (cardsPanelSwitcher)
            cardsPanelSwitcher.SetVisibleWithStatsPanel(false);

        if (panelRoot)
            panelRoot.SetActive(false);

        if (leftArrowButton)
            leftArrowButton.onClick.AddListener(PrevPage);

        if (rightArrowButton)
            rightArrowButton.onClick.AddListener(NextPage);

        ConfigureStatLine(hpStatText);
        ConfigureStatLine(atkStatText);
        ConfigureStatLine(defStatText);
    }

    private void OnEnable()
    {
        if (statsMenuAction != null && statsMenuAction.action != null)
        {
            statsMenuAction.action.performed += OnStatsMenuPerformed;
            statsMenuAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (statsMenuAction != null && statsMenuAction.action != null)
            statsMenuAction.action.performed -= OnStatsMenuPerformed;

        EnableGameplayInputsImmediate();
    }

    private void OnDestroy()
    {
        if (leftArrowButton)
            leftArrowButton.onClick.RemoveListener(PrevPage);

        if (rightArrowButton)
            rightArrowButton.onClick.RemoveListener(NextPage);
    }

    private void Update()
    {
        if (!isOpen)
            return;

        UpdateStatsUI();
        UpdateBuffPagerUI();
        UpdateSliderTextColor();
    }

    private void OnStatsMenuPerformed(InputAction.CallbackContext ctx)
    {
        TogglePanel();
    }

    public void TogglePanel()
    {
        if (isOpen)
            ClosePanel();
        else
            OpenPanel();
    }

    public void OpenPanel()
    {
        if (isOpen)
            return;

        isOpen = true;

        cachedPrevTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        DisableGameplayInputs();

        if (panelRoot)
            panelRoot.SetActive(true);

        if (cardsPanelSwitcher)
            cardsPanelSwitcher.SetVisibleWithStatsPanel(true);

        pageIndex = 0;

        UpdateStatsUI();
        UpdateBuffPagerUI();
        UpdateSliderTextColor();

        OnPanelToggled?.Invoke(true);
    }

    public void ClosePanel()
    {
        if (!isOpen)
            return;

        isOpen = false;

        if (cardsPanelSwitcher)
            cardsPanelSwitcher.SetVisibleWithStatsPanel(false);

        if (panelRoot)
            panelRoot.SetActive(false);

        Time.timeScale = cachedPrevTimeScale <= 0f ? 1f : cachedPrevTimeScale;

        EnableGameplayInputsDelayed();

        OnPanelToggled?.Invoke(false);
    }

    // ─────────────────────────────────────────────
    // Input Blocking
    // ─────────────────────────────────────────────

    private void DisableGameplayInputs()
    {
        if (!disableGameplayInputsWhileOpen)
            return;

        if (_reenableInputRoutine != null)
        {
            StopCoroutine(_reenableInputRoutine);
            _reenableInputRoutine = null;
        }

        _disabledGameplayActions.Clear();

        List<InputActionReference> refs = new List<InputActionReference>();

        if (gameplayActionsToDisable != null && gameplayActionsToDisable.Length > 0)
        {
            refs.AddRange(gameplayActionsToDisable);
        }
        else
        {
            AutoCollectPlayerControlActions(refs);
        }

        for (int i = 0; i < refs.Count; i++)
        {
            InputActionReference actionRef = refs[i];

            if (actionRef == null || actionRef.action == null)
                continue;

            if (statsMenuAction != null && actionRef.action == statsMenuAction.action)
                continue;

            if (!actionRef.action.enabled)
                continue;

            actionRef.action.Disable();
            _disabledGameplayActions.Add(actionRef.action);
        }

        // Keep TAB / stats menu active so it can close the panel.
        if (statsMenuAction != null && statsMenuAction.action != null)
            statsMenuAction.action.Enable();
    }

    private void EnableGameplayInputsDelayed()
    {
        if (!disableGameplayInputsWhileOpen)
            return;

        if (_reenableInputRoutine != null)
            StopCoroutine(_reenableInputRoutine);

        _reenableInputRoutine = StartCoroutine(EnableGameplayInputsAfterDelay());
    }

    private IEnumerator EnableGameplayInputsAfterDelay()
    {
        float delay = Mathf.Max(0f, reEnableInputDelay);

        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);
        else
            yield return null;

        EnableGameplayInputsImmediate();
        _reenableInputRoutine = null;
    }

    private void EnableGameplayInputsImmediate()
    {
        for (int i = 0; i < _disabledGameplayActions.Count; i++)
        {
            if (_disabledGameplayActions[i] != null)
                _disabledGameplayActions[i].Enable();
        }

        _disabledGameplayActions.Clear();

        if (statsMenuAction != null && statsMenuAction.action != null)
            statsMenuAction.action.Enable();
    }

    private void AutoCollectPlayerControlActions(List<InputActionReference> result)
    {
        if (result == null)
            return;

        if (!playerControls)
            playerControls = GetComponent<PlayerControls>();

        if (!playerControls)
            return;

        FieldInfo[] fields = typeof(PlayerControls).GetFields(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
        );

        for (int i = 0; i < fields.Length; i++)
        {
            FieldInfo field = fields[i];

            if (!typeof(InputActionReference).IsAssignableFrom(field.FieldType))
                continue;

            InputActionReference actionRef = field.GetValue(playerControls) as InputActionReference;

            if (actionRef == null)
                continue;

            if (!result.Contains(actionRef))
                result.Add(actionRef);
        }
    }

    // ─────────────────────────────────────────────
    // Stats / HP Binding
    // ─────────────────────────────────────────────

    private void UpdateStatsUI()
    {
        if (!stats)
            return;

        if (hpSlider)
        {
            if (!Mathf.Approximately(hpSlider.maxValue, stats.maxHealth))
                hpSlider.maxValue = stats.maxHealth;

            hpSlider.value = Mathf.Clamp(stats.currentHealth, 0f, stats.maxHealth);
        }

        if (hpSliderCenterText)
            hpSliderCenterText.text = Mathf.RoundToInt(stats.currentHealth).ToString();

        string nbsp = "\u00A0";

        if (hpStatText)
            hpStatText.text = $"HP:{nbsp}{Mathf.RoundToInt(stats.maxHealth)}";

        if (atkStatText)
            atkStatText.text = $"ATK:{nbsp}{Mathf.RoundToInt(stats.baseDamage)}";

        if (defStatText)
            defStatText.text = $"DEF:{nbsp}{Mathf.RoundToInt(stats.armor)}";
    }

    // ─────────────────────────────────────────────
    // Buff Pager
    // ─────────────────────────────────────────────

    private void UpdateBuffPagerUI()
    {
        if (buffSlots == null || buffSlots.Length == 0)
            return;

        int total = buffInventory && buffInventory.ownedBuffs != null
            ? buffInventory.ownedBuffs.Count
            : 0;

        for (int i = 0; i < buffSlots.Length; i++)
        {
            int idx = pageIndex * pageSize + i;

            BuffData data = idx >= 0 && idx < total
                ? buffInventory.ownedBuffs[idx]
                : null;

            if (buffSlots[i] != null)
                buffSlots[i].Set(data);
        }

        int totalPages = Mathf.CeilToInt(total / (float)pageSize);

        if (leftArrowButton)
            leftArrowButton.interactable = pageIndex > 0;

        if (rightArrowButton)
            rightArrowButton.interactable = pageIndex < Mathf.Max(0, totalPages - 1);
    }

    public void NextPage()
    {
        int total = buffInventory && buffInventory.ownedBuffs != null
            ? buffInventory.ownedBuffs.Count
            : 0;

        int totalPages = Mathf.CeilToInt(total / (float)pageSize);

        if (pageIndex < Mathf.Max(0, totalPages - 1))
        {
            pageIndex++;
            UpdateBuffPagerUI();
        }
    }

    public void PrevPage()
    {
        if (pageIndex > 0)
        {
            pageIndex--;
            UpdateBuffPagerUI();
        }
    }

    // ─────────────────────────────────────────────
    // Slider text color based on fill overlap
    // ─────────────────────────────────────────────

    private void UpdateSliderTextColor()
    {
        if (!hpSliderCenterText || !sliderFillRect || !sliderTextRect)
            return;

        Rect fillWorldRect = GetWorldRect(sliderFillRect);
        Rect textWorldRect = GetWorldRect(sliderTextRect);

        bool overlaps = fillWorldRect.Overlaps(textWorldRect, true);
        hpSliderCenterText.color = overlaps ? onFillColor : offFillColor;
    }

    private Rect GetWorldRect(RectTransform rt)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);

        float xMin = corners[0].x;
        float yMin = corners[0].y;
        float width = corners[2].x - corners[0].x;
        float height = corners[2].y - corners[0].y;

        return new Rect(xMin, yMin, width, height);
    }

    private void ConfigureStatLine(TextMeshProUGUI t)
    {
        if (!t)
            return;

        t.enableWordWrapping = false;
        t.overflowMode = TextOverflowModes.Overflow;
        t.richText = false;
        t.alignment = TextAlignmentOptions.Left;
    }
}