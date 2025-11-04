using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

[RequireComponent(typeof(PlayerStats))]
public class StatsPanelController : MonoBehaviour
{
    [Header("Wiring")]
    public GameObject panelRoot;
    public InputActionReference statsMenuAction;
    public PlayerStats stats;
    public PlayerBuffInventory buffInventory;

    [Header("HP Slider UI")]
    public Slider hpSlider;
    public TextMeshProUGUI hpSliderCenterText;   // shows CURRENT HP on the bar

    [Header("Stat Lines (rendered as 'LABEL : value')")]
    public TextMeshProUGUI hpStatText;   // "HP: 100"
    public TextMeshProUGUI atkStatText;  // "ATK: 25"
    public TextMeshProUGUI defStatText;  // "DEF: 6"

    [Header("Buff Pager")]
    public BuffSlotUI[] buffSlots = new BuffSlotUI[3];
    public Button leftArrowButton;
    public Button rightArrowButton;

    [Header("Slider Text Color Swap")]
    public RectTransform sliderFillRect;   // Fill image RectTransform
    public RectTransform sliderTextRect;   // Center text RectTransform
    public Color onFillColor = Color.black;
    public Color offFillColor = Color.white;

    private bool isOpen = false;
    private int pageIndex = 0;
    private const int pageSize = 3;
    private float cachedPrevTimeScale = 1f;

    private void Reset()
    {
        stats = GetComponent<PlayerStats>();
        if (!buffInventory) buffInventory = GetComponent<PlayerBuffInventory>();
    }

    private void Awake()
    {
        if (!stats) stats = GetComponent<PlayerStats>();
        if (!buffInventory) buffInventory = GetComponent<PlayerBuffInventory>();

        if (panelRoot) panelRoot.SetActive(false);

        if (leftArrowButton) leftArrowButton.onClick.AddListener(PrevPage);
        if (rightArrowButton) rightArrowButton.onClick.AddListener(NextPage);

        // keep stat lines on one row and left aligned
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
        {
            statsMenuAction.action.performed -= OnStatsMenuPerformed;
            statsMenuAction.action.Disable();
        }
    }

    private void Update()
    {
        if (!isOpen) return;

        UpdateStatsUI();
        UpdateBuffPagerUI();
        UpdateSliderTextColor();
    }

    // -------- Toggle --------
    private void OnStatsMenuPerformed(InputAction.CallbackContext ctx) => TogglePanel();

    public void TogglePanel()
    {
        if (isOpen) ClosePanel();
        else OpenPanel();
    }

    public void OpenPanel()
    {
        if (isOpen) return;
        isOpen = true;

        cachedPrevTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        if (panelRoot) panelRoot.SetActive(true);
        pageIndex = 0;

        UpdateStatsUI();
        UpdateBuffPagerUI();
        UpdateSliderTextColor();
    }

    public void ClosePanel()
    {
        if (!isOpen) return;
        isOpen = false;

        if (panelRoot) panelRoot.SetActive(false);
        Time.timeScale = cachedPrevTimeScale <= 0f ? 1f : cachedPrevTimeScale;
    }

    // -------- Stats / HP Binding --------
    private void UpdateStatsUI()
    {
        if (!stats) return;

        // Slider binds to current/max HP
        if (hpSlider)
        {
            if (!Mathf.Approximately(hpSlider.maxValue, stats.maxHealth))
                hpSlider.maxValue = stats.maxHealth;

            hpSlider.value = Mathf.Clamp(stats.currentHealth, 0f, stats.maxHealth);
        }

        // Slider center text = current HP number
        if (hpSliderCenterText)
            hpSliderCenterText.text = Mathf.RoundToInt(stats.currentHealth).ToString();

        // Stat lines rendered as "LABEL: value" on a single line (non-breaking space after colon)
        string nbsp = "\u00A0";
        if (hpStatText) hpStatText.text = $"HP:{nbsp}{Mathf.RoundToInt(stats.maxHealth)}";
        if (atkStatText) atkStatText.text = $"ATK:{nbsp}{Mathf.RoundToInt(stats.baseDamage)}";
        if (defStatText) defStatText.text = $"DEF:{nbsp}{Mathf.RoundToInt(stats.armor)}";
    }

    // -------- Buff Pager --------
    private void UpdateBuffPagerUI()
    {
        if (buffSlots == null || buffSlots.Length == 0) return;

        int total = (buffInventory && buffInventory.ownedBuffs != null)
            ? buffInventory.ownedBuffs.Count
            : 0;

        for (int i = 0; i < buffSlots.Length; i++)
        {
            int idx = pageIndex * pageSize + i;
            BuffData data = (idx >= 0 && idx < total) ? buffInventory.ownedBuffs[idx] : null;
            if (buffSlots[i] != null) buffSlots[i].Set(data); // slot hides itself when null
        }

        int totalPages = Mathf.CeilToInt(total / (float)pageSize);
        if (leftArrowButton) leftArrowButton.interactable = pageIndex > 0;
        if (rightArrowButton) rightArrowButton.interactable = pageIndex < Mathf.Max(0, totalPages - 1);
    }

    public void NextPage()
    {
        int total = (buffInventory && buffInventory.ownedBuffs != null)
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

    // -------- Slider text color based on fill overlap --------
    private void UpdateSliderTextColor()
    {
        if (!hpSliderCenterText || !sliderFillRect || !sliderTextRect) return;

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
        if (!t) return;
        t.enableWordWrapping = false;
        t.overflowMode = TextOverflowModes.Overflow;   // use Truncate if you prefer
        t.richText = false;
        t.alignment = TextAlignmentOptions.Left;       // or MidlineLeft for vertical center
    }
}
