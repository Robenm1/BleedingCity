using UnityEngine;
using UnityEngine.InputSystem; // <-- IMPORTANT for Keyboard.current

[RequireComponent(typeof(PlayerXP))]
public class SpendCoinDebug : MonoBehaviour
{
    [Header("Debug Spend Settings")]
    [Tooltip("How many coins to try to spend when you press C.")]
    public int coinsToSpendOnTest = 1;

    private PlayerXP playerXP;

    private void Awake()
    {
        playerXP = GetComponent<PlayerXP>();
    }

    private void Update()
    {
        // Safety: Keyboard might be null in weird cases (no keyboard device)
        if (Keyboard.current == null)
            return;

        // New Input System version of GetKeyDown(KeyCode.C)
        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            bool success = playerXP.TrySpendCoins(coinsToSpendOnTest);

            if (success)
            {
                Debug.Log("[SpendCoinDebug] Spent " + coinsToSpendOnTest + " coins. XP and coins updated.");
            }
            else
            {
                Debug.Log("[SpendCoinDebug] Not enough coins to spend " + coinsToSpendOnTest + ".");
            }
        }
    }
}
