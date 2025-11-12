using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "CardData", menuName = "Game/Cards/Card Data")]
public class CardData : ScriptableObject
{
    [Header("Meta")]
    public string displayName;
    [TextArea(2, 4)] public string description;
    public Sprite icon;

    [Header("Type")]
    public bool isGlobal = false;   // usable by any character
    public bool isActive = false;   // otherwise passive

    [Header("Effects")]
    [Tooltip("Effects to apply to the player when this card is in the deck.")]
    public List<CardEffectSO> effects = new List<CardEffectSO>();
}
