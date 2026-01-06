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

    private BuffData[] currentChoices;
    private PlayerBuffInventory buffInventory;
    private bool isPanelOpen;

    private void Awake()
    {
        buffInventory = FindObjectOfType<PlayerBuffInventory>();
        if (buffInventory == null)
        {
            Debug.LogWarning("[LevelUpUIManager] No PlayerBuffInventory found in scene.");
        }

        if (levelUpPanel != null)
            levelUpPanel.SetActive(false);

        isPanelOpen = false;
    }

    public bool IsPanelOpen()
    {
        return isPanelOpen;
    }

    public void OpenLevelUpPanel()
    {
        if (isPanelOpen)
        {
            Debug.LogWarning("[LevelUpUIManager] Panel is already open. Ignoring request.");
            return;
        }

        if (allBuffs == null || allBuffs.Count == 0)
        {
            Debug.LogWarning("[LevelUpUIManager] allBuffs is empty. No buffs to offer.");
            return;
        }

        currentChoices = GetRandomUniqueBuffs(choicesPerLevel);

        FillChoiceSlot(choice0Icon, choice0Name, choice0Description, 0);
        FillChoiceSlot(choice1Icon, choice1Name, choice1Description, 1);
        FillChoiceSlot(choice2Icon, choice2Name, choice2Description, 2);

        choice0Button.onClick.RemoveAllListeners();
        choice1Button.onClick.RemoveAllListeners();
        choice2Button.onClick.RemoveAllListeners();

        choice0Button.onClick.AddListener(() => ChooseBuff(0));
        choice1Button.onClick.AddListener(() => ChooseBuff(1));
        choice2Button.onClick.AddListener(() => ChooseBuff(2));

        if (levelUpPanel != null)
            levelUpPanel.SetActive(true);

        isPanelOpen = true;
        Time.timeScale = 0f;
    }

    private void ChooseBuff(int index)
    {
        if (currentChoices == null || index < 0 || index >= currentChoices.Length)
            return;

        BuffData chosen = currentChoices[index];
        if (chosen == null)
            return;

        Debug.Log("[LevelUpUIManager] Player chose: " + chosen.buffName + " (" + chosen.rarity + ")");

        if (buffInventory != null)
        {
            buffInventory.AddBuff(chosen);
        }

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

    private void CloseLevelUpPanel()
    {
        if (levelUpPanel != null)
            levelUpPanel.SetActive(false);

        isPanelOpen = false;
        Time.timeScale = 1f;
        currentChoices = null;
    }

    private void FillChoiceSlot(
        Image iconTarget,
        TextMeshProUGUI nameTarget,
        TextMeshProUGUI descTarget,
        int index
    )
    {
        if (currentChoices == null || index >= currentChoices.Length || currentChoices[index] == null)
        {
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

    private BuffData[] GetRandomUniqueBuffs(int amount)
    {
        if (allBuffs == null || allBuffs.Count == 0)
            return new BuffData[0];

        List<BuffData> tempList = new List<BuffData>(allBuffs);

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
