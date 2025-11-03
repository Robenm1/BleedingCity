using UnityEngine;

[RequireComponent(typeof(PlayerControls))]
[RequireComponent(typeof(XPLevelSystem))]
public class LevelUpHandler : MonoBehaviour
{
    private PlayerControls controls;
    private XPLevelSystem xpSystem;
    private LevelUpUIManager uiManager;

    private void Awake()
    {
        controls = GetComponent<PlayerControls>();
        xpSystem = GetComponent<XPLevelSystem>();

        // We look for the UI object in the scene
        uiManager = FindObjectOfType<LevelUpUIManager>();
        if (uiManager == null)
        {
            Debug.LogWarning("[LevelUpHandler] No LevelUpUIManager found in the scene. " +
                             "Level-up panel will not open.");
        }
    }

    private void OnEnable()
    {
        controls.OnLevelUp += HandleLevelUpPressed;
    }

    private void OnDisable()
    {
        controls.OnLevelUp -= HandleLevelUpPressed;
    }

    /// <summary>
    /// Called when player presses the LevelUp button (like F).
    /// This tries to actually BUY a level using XP+coins.
    /// If successful, it then opens the buff choice UI.
    /// </summary>
    private void HandleLevelUpPressed()
    {
        // Try to spend for a level
        // xpSystem.TrySpendForLevel():
        // - checks affordability (XP + coins)
        // - subtracts coins and XP (leaves XP overflow)
        // - increases required XP for next level
        // - updates UI (XP bar, coin text)
        bool leveled = xpSystem.TrySpendForLevel();

        if (!leveled)
        {
            // Not enough XP / not enough coins / can't level right now
            return;
        }

        // If we succeeded buying a level, give player their buff choice
        if (uiManager != null)
        {
            uiManager.OpenLevelUpPanel();
        }
        else
        {
            Debug.LogWarning("[LevelUpHandler] Player leveled but no UI panel to show buff choices.");
        }
    }
}
