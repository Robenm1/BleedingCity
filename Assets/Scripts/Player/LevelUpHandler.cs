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

    private void HandleLevelUpPressed()
    {
        if (uiManager != null && uiManager.IsPanelOpen())
        {
            return;
        }

        bool leveled = xpSystem.TrySpendForLevel();

        if (!leveled)
        {
            return;
        }

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
