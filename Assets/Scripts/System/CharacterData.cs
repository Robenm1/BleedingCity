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
    public Sprite buttonIcon;
    public Sprite selectedIcon;

    [Header("Element")]
    [Tooltip("Element icon shown in the character select scene.")]
    public Sprite element;

    [Header("Special Cards (Top Row in #6)")]
    public List<CardData> specialCards = new List<CardData>();
}