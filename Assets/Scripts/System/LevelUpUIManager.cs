using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class LevelUpUIManager : MonoBehaviour
{
    [Header("Panel Root")]
    [Tooltip("The full Level Up panel UI object. We'll SetActive(true/false) on this.")]
    public GameObject levelUpPanel;

    [Header("Buff Pool (runtime for this run)")]
    [Tooltip("All possible buff ScriptableObjects. We'll randomly roll from this list.")]
    public List<BuffData> allBuffs = new List<BuffData>();

    [Tooltip("How many choices to offer each time you level (usually 3).")]
    public int choicesPerLevel = 3;

    [Header("Choice Slot 0")]
    public Image choice0Icon;
    public TextMeshProUGUI choice0Name;
    public TextMeshProUGUI choice0Description;
    public Button choice0Button;

    [Header("Choice Slot 1")]
    public Image choice1Icon;
    public TextMeshProUGUI choice1Name;
    public TextMeshProUGUI choice1Description;
    public Button choice1Button;

    [Header("Choice Slot 2")]
    public Image choice2Icon;
    public TextMeshProUGUI choice2Name;
    public TextMeshProUGUI choice2Description;
    public Button choice2Button;

    // The rolled options being shown right now
    private BuffData[] currentChoices;

    // We need to give the chosen buff to the player
    private PlayerBuffInventory buffInventory;

    private void Awake()
    {
        // Grab the player's buff inventory
        buffInventory = FindObjectOfType<PlayerBuffInventory>();
        if (buffInventory == null)
        {
            Debug.LogWarning("[LevelUpUIManager] No PlayerBuffInventory found in scene.");
        }

        // Make sure the panel starts hidden in normal gameplay
        if (levelUpPanel != null)
            levelUpPanel.SetActive(false);
    }

    /// <summary>
    /// Called (by LevelUpHandler) when the player has successfully leveled.
    /// - Picks random buffs
    /// - Shows the panel with those buffs
    /// - Pauses the game (Time.timeScale = 0)
    /// </summary>
    public void OpenLevelUpPanel()
    {
        if (allBuffs == null || allBuffs.Count == 0)
        {
            Debug.LogWarning("[LevelUpUIManager] allBuffs is empty. No buffs to offer.");
            return;
        }

        // Roll unique random buffs for this choice
        currentChoices = GetRandomUniqueBuffs(choicesPerLevel);

        // Fill the UI slots with the chosen buffs
        FillChoiceSlot(choice0Icon, choice0Name, choice0Description, 0);
        FillChoiceSlot(choice1Icon, choice1Name, choice1Description, 1);
        FillChoiceSlot(choice2Icon, choice2Name, choice2Description, 2);

        // Assign button listeners
        choice0Button.onClick.RemoveAllListeners();
        choice1Button.onClick.RemoveAllListeners();
        choice2Button.onClick.RemoveAllListeners();

        choice0Button.onClick.AddListener(() => ChooseBuff(0));
        choice1Button.onClick.AddListener(() => ChooseBuff(1));
        choice2Button.onClick.AddListener(() => ChooseBuff(2));

        // Show panel
        if (levelUpPanel != null)
            levelUpPanel.SetActive(true);

        // Pause time while the panel is open
        Time.timeScale = 0f;
    }

    /// <summary>
    /// Called when the player clicks one of the buff buttons.
    /// Gives the buff, handles rarity rules, closes the panel, and unpauses.
    /// </summary>
    private void ChooseBuff(int index)
    {
        // Safety checks
        if (currentChoices == null || index < 0 || index >= currentChoices.Length)
            return;

        BuffData chosen = currentChoices[index];
        if (chosen == null)
            return;

        Debug.Log("[LevelUpUIManager] Player chose: " + chosen.buffName + " (" + chosen.rarity + ")");

        // Add the buff to the player's inventory
        if (buffInventory != null)
        {
            buffInventory.AddBuff(chosen);
        }

        // If Epic or Legendary, remove it from the pool so it won't show again this run
        if (chosen.rarity == Rarity.Epic || chosen.rarity == Rarity.Legendary)
        {
            for (int i = allBuffs.Count - 1; i >= 0; i--)
            {
                if (allBuffs[i] == chosen)
                {
                    allBuffs.RemoveAt(i);
                }
            }
        }

        CloseLevelUpPanel();
    }

    /// <summary>
    /// Hides the panel and unpauses the game.
    /// </summary>
    private void CloseLevelUpPanel()
    {
        if (levelUpPanel != null)
            levelUpPanel.SetActive(false);

        // Unpause
        Time.timeScale = 1f;

        // Clear memory of the last roll
        currentChoices = null;
    }

    /// <summary>
    /// Fill icon/name/description for one of the choice slots.
    /// If no buff available for that index (like pool runs out), we blank that slot.
    /// </summary>
    private void FillChoiceSlot(
        Image iconTarget,
        TextMeshProUGUI nameTarget,
        TextMeshProUGUI descTarget,
        int index
    )
    {
        if (currentChoices == null || index >= currentChoices.Length || currentChoices[index] == null)
        {
            // slot is empty, clear visuals
            if (iconTarget != null) iconTarget.sprite = null;
            if (nameTarget != null) nameTarget.text = "";
            if (descTarget != null) descTarget.text = "";
            return;
        }

        BuffData data = currentChoices[index];

        if (iconTarget != null)
            iconTarget.sprite = data.icon;

        if (nameTarget != null)
            nameTarget.text = data.buffName;

        if (descTarget != null)
            descTarget.text = data.description;
    }

    /// <summary>
    /// Take a shuffled copy of the pool and return up to N unique buffs.
    /// If the pool is smaller than requested, we just return as many as we have.
    /// </summary>
    private BuffData[] GetRandomUniqueBuffs(int amount)
    {
        if (allBuffs == null || allBuffs.Count == 0)
            return new BuffData[0];

        // Copy the pool so we don't mutate it while picking
        List<BuffData> tempList = new List<BuffData>(allBuffs);

        // Fisher-Yates shuffle
        for (int i = 0; i < tempList.Count; i++)
        {
            int j = Random.Range(i, tempList.Count);
            BuffData swap = tempList[i];
            tempList[i] = tempList[j];
            tempList[j] = swap;
        }

        int finalCount = Mathf.Min(amount, tempList.Count);
        BuffData[] result = new BuffData[finalCount];

        for (int k = 0; k < finalCount; k++)
        {
            result[k] = tempList[k];
        }

        return result;
    }
}
