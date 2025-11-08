using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "CharacterData", menuName = "Game/Character", order = 0)]
public class CharacterData : ScriptableObject
{
    [Header("Identity")]
    public string characterId;
    public string displayName;
    [TextArea(2, 6)] public string description;

    [Header("Prefabs")]
    public GameObject characterPrefab;

    [Header("Icons")]
    public Sprite buttonIcon;   // small icon for grid button (#5)
    public Sprite selectedIcon; // big portrait (#1)

    [Header("Special Cards (Top Row in #6)")]
    public List<CardData> specialCards = new List<CardData>();
}
