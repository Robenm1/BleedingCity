using System.Collections.Generic;
using UnityEngine;

public class DeckRuntimeApplier : MonoBehaviour
{
    /// <summary>
    /// Called by PlayerSpawnAnchor with the deck (List<CardData>).
    /// Applies all CardEffectSO in each card.
    /// </summary>
    public void ReapplyFrom(List<CardData> selectedCards)
    {
        if (selectedCards == null) return;

        foreach (var card in selectedCards)
        {
            if (card == null || card.effects == null) continue;

            foreach (var effect in card.effects)
            {
                if (effect == null) continue;
                effect.Apply(gameObject);
            }
        }
    }
}
