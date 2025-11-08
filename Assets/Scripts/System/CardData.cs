using UnityEngine;

[CreateAssetMenu(fileName = "Card", menuName = "Game/Card", order = 0)]
public class CardData : ScriptableObject
{
    [Header("Identity")]
    public string cardId;
    public string displayName;
    [TextArea(2, 6)] public string description;

    [Header("Visuals")]
    public Sprite icon;

    [Header("Scope")]
    [Tooltip("Checked = Global card (shown in bottom row). Unchecked = Character-specific (top row).")]
    public bool isGlobal = false;

    [Header("Type")]
    [Tooltip("Active = has a trigger/ability; Passive = always-on.")]
    public bool isActive = false;   // false = Passive, true = Active
}
