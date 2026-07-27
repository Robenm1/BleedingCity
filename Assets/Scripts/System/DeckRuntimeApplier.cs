using System.Collections.Generic;
using UnityEngine;

public class DeckRuntimeApplier : MonoBehaviour
{
    [Header("Runtime Deck")]
    [Tooltip("The cards selected before the run. Used by UI and reapplied effects.")]
    public List<CardData> selectedCards = new List<CardData>();

    /// <summary>
    /// Called by PlayerSpawnAnchor with the deck (List<CardData>).
    /// Stores the deck and applies all CardEffectSO in each card.
    /// </summary>
    public void ReapplyFrom(List<CardData> cards)
    {
        selectedCards.Clear();

        if (cards == null)
            return;

        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] != null)
                selectedCards.Add(cards[i]);
        }

        ApplySelectedCards();

        RefreshCardsPanelUI();
    }

    private void ApplySelectedCards()
    {
        for (int i = 0; i < selectedCards.Count; i++)
        {
            CardData card = selectedCards[i];

            if (card == null || card.effects == null)
                continue;

            for (int j = 0; j < card.effects.Count; j++)
            {
                CardEffectSO effect = card.effects[j];

                if (effect == null)
                    continue;

                effect.Apply(gameObject);
            }
        }
    }

    private void RefreshCardsPanelUI()
    {
        SelectedCardsPanelUI panel = GetComponentInChildren<SelectedCardsPanelUI>(true);

        if (panel == null)
            panel = FindObjectOfType<SelectedCardsPanelUI>(true);

        if (panel != null)
            panel.Refresh();
    }

    public List<CardData> GetSelectedCards()
    {
        return selectedCards;
    }
}