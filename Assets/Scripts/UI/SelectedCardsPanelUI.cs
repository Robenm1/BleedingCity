using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SelectedCardsPanelUI : MonoBehaviour
{
    [Header("Player")]
    [Tooltip("Optional. If empty, the script finds the object tagged Player.")]
    public GameObject player;

    [Tooltip("If your player has a specific component that stores the cards, drag it here. Optional.")]
    public MonoBehaviour cardHolderComponent;

    [Header("Card Image Slots")]
    [Tooltip("Drag your card image UI components here.")]
    public Image[] cardImages;

    [Header("Optional Card Names")]
    public TextMeshProUGUI[] cardNameTexts;

    [Header("Optional Card Descriptions")]
    [Tooltip("Drag your TMP description texts here. Same order as card images.")]
    public TextMeshProUGUI[] cardDescriptionTexts;

    [Header("Empty Slots")]
    public bool hideEmptySlots = true;
    public Sprite emptySlotSprite;

    [Header("Auto Refresh")]
    public bool refreshOnEnable = true;

    [Tooltip("Small delay helps if cards are assigned after player spawn.")]
    public float refreshDelay = 0.05f;

    [Header("Debug")]
    public bool showDebug = false;

    private readonly List<CardData> _cards = new List<CardData>();

    private void OnEnable()
    {
        if (refreshOnEnable)
            StartCoroutine(RefreshDelayed());
    }

    private IEnumerator RefreshDelayed()
    {
        yield return new WaitForSecondsRealtime(refreshDelay);
        Refresh();
    }

    public void Refresh()
    {
        FindPlayerIfNeeded();
        ReadCardsFromPlayer();
        DrawCards();
    }

    private void FindPlayerIfNeeded()
    {
        if (player != null)
            return;

        GameObject foundPlayer = GameObject.FindGameObjectWithTag("Player");

        if (foundPlayer != null)
            player = foundPlayer;
    }

    private void ReadCardsFromPlayer()
    {
        _cards.Clear();

        if (cardHolderComponent != null)
        {
            TryReadCardsFromComponent(cardHolderComponent, _cards);
            return;
        }

        if (player == null)
        {
            if (showDebug)
                Debug.LogWarning("[SelectedCardsPanelUI] No player found.");

            return;
        }

        MonoBehaviour[] components = player.GetComponents<MonoBehaviour>();

        for (int i = 0; i < components.Length; i++)
        {
            MonoBehaviour component = components[i];

            if (component == null)
                continue;

            if (TryReadCardsFromComponent(component, _cards))
            {
                cardHolderComponent = component;

                if (showDebug)
                    Debug.Log($"[SelectedCardsPanelUI] Found cards on component: {component.GetType().Name}");

                return;
            }
        }

        if (showDebug)
            Debug.LogWarning("[SelectedCardsPanelUI] Could not find selected cards on player.");
    }

    private bool TryReadCardsFromComponent(MonoBehaviour component, List<CardData> result)
    {
        if (component == null)
            return false;

        System.Type type = component.GetType();

        string[] possibleFieldNames =
        {
            "selectedCards",
            "currentDeck",
            "deckCards",
            "equippedCards",
            "chosenCards",
            "playerCards",
            "cards",
            "selectedDeck",
            "activeDeck"
        };

        for (int i = 0; i < possibleFieldNames.Length; i++)
        {
            string name = possibleFieldNames[i];

            FieldInfo field = type.GetField(
                name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
            );

            if (field != null)
            {
                object value = field.GetValue(component);

                if (TryConvertToCardList(value, result))
                    return result.Count > 0;
            }

            PropertyInfo property = type.GetProperty(
                name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
            );

            if (property != null && property.CanRead)
            {
                object value = property.GetValue(component, null);

                if (TryConvertToCardList(value, result))
                    return result.Count > 0;
            }
        }

        return false;
    }

    private bool TryConvertToCardList(object value, List<CardData> result)
    {
        if (value == null)
            return false;

        if (value is IList<CardData> cardList)
        {
            for (int i = 0; i < cardList.Count; i++)
            {
                if (cardList[i] != null)
                    result.Add(cardList[i]);
            }

            return true;
        }

        if (value is CardData[] cardArray)
        {
            for (int i = 0; i < cardArray.Length; i++)
            {
                if (cardArray[i] != null)
                    result.Add(cardArray[i]);
            }

            return true;
        }

        return false;
    }

    private void DrawCards()
    {
        if (cardImages == null)
            return;

        for (int i = 0; i < cardImages.Length; i++)
        {
            CardData card = i < _cards.Count ? _cards[i] : null;
            DrawCardSlot(i, card);
        }

        if (showDebug)
            Debug.Log($"[SelectedCardsPanelUI] Showing {_cards.Count} selected cards.");
    }

    private void DrawCardSlot(int index, CardData card)
    {
        Image image = cardImages[index];

        if (image == null)
            return;

        if (card == null)
        {
            if (emptySlotSprite != null)
            {
                image.sprite = emptySlotSprite;
                image.enabled = true;
                image.gameObject.SetActive(true);
            }
            else
            {
                image.sprite = null;
                image.enabled = false;
                image.gameObject.SetActive(!hideEmptySlots);
            }

            SetCardName(index, "");
            SetCardDescription(index, "");
            return;
        }

        Sprite sprite = GetCardSprite(card);

        if (sprite != null)
        {
            image.sprite = sprite;
            image.enabled = true;
            image.gameObject.SetActive(true);
        }
        else
        {
            image.sprite = emptySlotSprite;
            image.enabled = emptySlotSprite != null;
            image.gameObject.SetActive(!hideEmptySlots || emptySlotSprite != null);
        }

        SetCardName(index, GetCardName(card));
        SetCardDescription(index, GetCardDescription(card));
    }

    private Sprite GetCardSprite(CardData card)
    {
        if (card == null)
            return null;

        string[] possibleSpriteNames =
        {
            "icon",
            "cardIcon",
            "uiIcon",
            "sprite",
            "artwork",
            "cardSprite",
            "selectedIcon",
            "buttonIcon"
        };

        System.Type type = card.GetType();

        for (int i = 0; i < possibleSpriteNames.Length; i++)
        {
            string name = possibleSpriteNames[i];

            FieldInfo field = type.GetField(
                name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
            );

            if (field != null && typeof(Sprite).IsAssignableFrom(field.FieldType))
            {
                Sprite sprite = field.GetValue(card) as Sprite;

                if (sprite != null)
                    return sprite;
            }

            PropertyInfo property = type.GetProperty(
                name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
            );

            if (property != null && typeof(Sprite).IsAssignableFrom(property.PropertyType))
            {
                Sprite sprite = property.GetValue(card, null) as Sprite;

                if (sprite != null)
                    return sprite;
            }
        }

        return null;
    }

    private string GetCardName(CardData card)
    {
        if (card == null)
            return "";

        string[] possibleNameFields =
        {
            "cardName",
            "displayName",
            "title",
            "cardTitle",
            "cardDisplayName"
        };

        string value = TryGetStringFromCard(card, possibleNameFields);

        if (!string.IsNullOrEmpty(value))
            return value;

        return card.name;
    }

    private string GetCardDescription(CardData card)
    {
        if (card == null)
            return "";

        string[] possibleDescriptionFields =
        {
            "description",
            "cardDescription",
            "desc",
            "tooltip",
            "details",
            "effectDescription",
            "longDescription",
            "shortDescription"
        };

        string value = TryGetStringFromCard(card, possibleDescriptionFields);

        if (!string.IsNullOrEmpty(value))
            return value;

        return "";
    }

    private string TryGetStringFromCard(CardData card, string[] possibleNames)
    {
        if (card == null || possibleNames == null)
            return "";

        System.Type type = card.GetType();

        for (int i = 0; i < possibleNames.Length; i++)
        {
            string name = possibleNames[i];

            FieldInfo field = type.GetField(
                name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
            );

            if (field != null && field.FieldType == typeof(string))
            {
                string value = field.GetValue(card) as string;

                if (!string.IsNullOrEmpty(value))
                    return value;
            }

            PropertyInfo property = type.GetProperty(
                name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
            );

            if (property != null && property.PropertyType == typeof(string))
            {
                string value = property.GetValue(card, null) as string;

                if (!string.IsNullOrEmpty(value))
                    return value;
            }
        }

        return "";
    }

    private void SetCardName(int index, string value)
    {
        if (cardNameTexts == null)
            return;

        if (index < 0 || index >= cardNameTexts.Length)
            return;

        if (cardNameTexts[index] == null)
            return;

        cardNameTexts[index].text = value;
        cardNameTexts[index].gameObject.SetActive(!string.IsNullOrEmpty(value));
    }

    private void SetCardDescription(int index, string value)
    {
        if (cardDescriptionTexts == null)
            return;

        if (index < 0 || index >= cardDescriptionTexts.Length)
            return;

        if (cardDescriptionTexts[index] == null)
            return;

        cardDescriptionTexts[index].text = value;
        cardDescriptionTexts[index].gameObject.SetActive(!string.IsNullOrEmpty(value));
    }
}