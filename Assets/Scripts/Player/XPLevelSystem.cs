using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(PlayerXP))]
public class XPLevelSystem : MonoBehaviour
{
    [Header("XP Requirement Scaling")]
    [Tooltip("XP needed for the FIRST level up.")]
    public int startingXPRequirement = 10;

    [Tooltip("How much extra XP is required each time you level up. Example: +10 each level.")]
    public int xpIncreasePerLevel = 10;

    [Tooltip("Current level number (1 at start, 2 after first level up, etc.).")]
    public int currentLevel = 1;

    // This is how much XP you currently need to buy the NEXT level.
    [SerializeField] private int xpRequiredForNextLevel;

    [Header("UI References")]
    [Tooltip("Slider that shows XP progress toward next level.")]
    public Slider xpSlider;

    [Tooltip("TMP that shows 'currentXP / requiredXP'.")]
    public TextMeshProUGUI xpText;

    [Tooltip("TMP that shows how many coins you currently have.")]
    public TextMeshProUGUI coinText;

    private PlayerXP playerXP;

    private void Awake()
    {
        playerXP = GetComponent<PlayerXP>();
        xpRequiredForNextLevel = startingXPRequirement;
    }

    private void OnEnable()
    {
        // Whenever XP or coins change (pickup, spend, etc.), update UI
        playerXP.OnXPChanged += HandleXPChanged;
        playerXP.OnCoinsChanged += HandleCoinsChanged;
    }

    private void OnDisable()
    {
        playerXP.OnXPChanged -= HandleXPChanged;
        playerXP.OnCoinsChanged -= HandleCoinsChanged;
    }

    private void Start()
    {
        RefreshUI();
    }

    private void HandleXPChanged(int newXP)
    {
        RefreshXPUI();
    }

    private void HandleCoinsChanged(int newCoins)
    {
        RefreshCoinUI();
    }

    /// <summary>
    /// Refresh everything (XP bar + coins text).
    /// </summary>
    private void RefreshUI()
    {
        RefreshXPUI();
        RefreshCoinUI();
    }

    /// <summary>
    /// Refresh only the XP slider and XP text.
    /// </summary>
    private void RefreshXPUI()
    {
        if (xpSlider != null)
        {
            xpSlider.maxValue = xpRequiredForNextLevel;

            int clampedXP = Mathf.Min(playerXP.currentXP, xpRequiredForNextLevel);
            xpSlider.value = clampedXP;
        }

        if (xpText != null)
        {
            int displayXP = Mathf.Min(playerXP.currentXP, xpRequiredForNextLevel);
            xpText.text = displayXP.ToString() + " / " + xpRequiredForNextLevel.ToString();
        }
    }

    /// <summary>
    /// Refresh only the coin counter TMP.
    /// </summary>
    private void RefreshCoinUI()
    {
        if (coinText != null)
        {
            coinText.text = playerXP.currentCoins.ToString();
        }
    }

    /// <summary>
    /// Called when the player presses the LevelUp button.
    /// Returns true if a level up actually happened.
    /// </summary>
    public bool TrySpendForLevel()
    {
        // Can we afford it?
        if (playerXP.currentXP < xpRequiredForNextLevel)
        {
            // Not enough XP to buy this level yet.
            return false;
        }

        // We can level. Spend coins and XP for this level.
        // This will:
        // - Remove coins based on xpRequiredForNextLevel and xpPerCoin
        // - Reset currentXP to 0
        playerXP.SpendForLevel(xpRequiredForNextLevel);

        // Increase level counter.
        currentLevel += 1;

        // Make future levels more expensive.
        xpRequiredForNextLevel += xpIncreasePerLevel;
        if (xpRequiredForNextLevel < 1)
            xpRequiredForNextLevel = 1;

        // Now update UI so it shows:
        // XP is 0 / newRequirement
        RefreshUI();

        // At this point you should open your upgrade UI (pick 1 of 3 buffs)
        return true;
    }

    /// <summary>
    /// If you ever want to force the next level's XP cost manually.
    /// </summary>
    public void SetXPRequired(int newRequirement)
    {
        xpRequiredForNextLevel = Mathf.Max(1, newRequirement);
        RefreshUI();
    }
}
