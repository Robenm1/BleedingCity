using UnityEngine;
using System;

public class PlayerXP : MonoBehaviour
{
    [Header("Wallet / Progress")]
    [Tooltip("How much XP progress the player currently has toward next level.")]
    public int currentXP = 0;

    [Tooltip("How many coins the player is holding right now.")]
    public int currentCoins = 0;

    [Header("Coin-to-XP Relationship")]
    [Tooltip("How much XP we consider ONE coin to be worth when paying costs.\nExample: if this is 5, then 1 coin = 5 XP when leveling or buying.")]
    public int xpPerCoin = 5;

    // UI listeners can hook these
    public event Action<int> OnXPChanged;
    public event Action<int> OnCoinsChanged;

    // =========================
    // GAIN (pickup)
    // =========================

    /// <summary>
    /// Called by XPCoin when the player walks over a coin.
    /// Adds 1 coin AND adds xpFromThisCoin XP toward level.
    /// </summary>
    public void AddCoinPickup(int xpFromThisCoin)
    {
        currentCoins += 1;
        currentXP += xpFromThisCoin;

        OnCoinsChanged?.Invoke(currentCoins);
        OnXPChanged?.Invoke(currentXP);
    }

    // =========================
    // SPEND (shop)
    // =========================

    /// <summary>
    /// Spend coins in a shop.
    /// Also subtracts XP progress, because spending coins should slow leveling.
    /// Returns true if spend worked.
    /// </summary>
    public bool TrySpendCoins(int coinsToSpend)
    {
        if (coinsToSpend <= 0) return false;
        if (currentCoins < coinsToSpend) return false;

        currentCoins -= coinsToSpend;

        // Spending coins also reduces XP bar progress
        int xpToRemove = coinsToSpend * Mathf.Max(1, xpPerCoin);
        currentXP -= xpToRemove;
        if (currentXP < 0) currentXP = 0;

        OnCoinsChanged?.Invoke(currentCoins);
        OnXPChanged?.Invoke(currentXP);
        return true;
    }

    // =========================
    // LEVEL-UP COST HELPERS
    // =========================

    /// <summary>
    /// How many coins are needed to pay an XP cost?
    /// We round UP so you can't underpay.
    /// </summary>
    public int GetCoinsCostForXP(int xpCost)
    {
        int value = Mathf.Max(1, xpPerCoin); // safety
        return Mathf.CeilToInt((float)xpCost / value);
    }

    /// <summary>
    /// Can we afford to buy a level that costs xpCost?
    /// You must have BOTH enough XP progress AND enough coins.
    /// </summary>
    public bool CanAffordLevel(int xpCost)
    {
        int coinsNeeded = GetCoinsCostForXP(xpCost);

        if (currentXP < xpCost) return false;
        if (currentCoins < coinsNeeded) return false;

        return true;
    }

    // =========================
    // LEVEL-UP SPEND
    // =========================

    /// <summary>
    /// Spend the XP cost to level up.
    /// This:
    ///  - Removes the correct number of coins
    ///  - Subtracts ONLY the XP cost (leftover XP carries over)
    ///  - Notifies UI
    /// </summary>
    public void SpendForLevel(int xpCostForThisLevel)
    {
        int coinsToRemove = GetCoinsCostForXP(xpCostForThisLevel);

        // Remove coins
        currentCoins -= coinsToRemove;
        if (currentCoins < 0) currentCoins = 0;

        // Subtract XP cost, but keep leftover XP
        currentXP -= xpCostForThisLevel;
        if (currentXP < 0) currentXP = 0;

        OnCoinsChanged?.Invoke(currentCoins);
        OnXPChanged?.Invoke(currentXP);
    }

    // =========================
    // Getters
    // =========================

    public int GetXP()
    {
        return currentXP;
    }

    public int GetCoins()
    {
        return currentCoins;
    }
}
