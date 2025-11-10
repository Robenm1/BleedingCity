using System.Collections.Generic;
using UnityEngine;

public class SelectionCarrier : MonoBehaviour
{
    public static SelectionCarrier Instance { get; private set; }

    public CharacterData selectedCharacter;
    public List<CardData> selectedCards = new();

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetSelection(CharacterData character, IReadOnlyList<CardData> deck)
    {
        selectedCharacter = character;
        selectedCards = deck != null ? new List<CardData>(deck) : new List<CardData>();
    }

    public void Clear()
    {
        selectedCharacter = null;
        selectedCards.Clear();
    }
}
