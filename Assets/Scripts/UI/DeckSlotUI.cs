using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeckSlotUI : MonoBehaviour
{
    public Image iconImage;
    public TextMeshProUGUI nameText;

    [HideInInspector] public CardData card;              // what’s in this slot now
    [HideInInspector] public CardButton sourcePoolButton; // to re-enable if removed

    public bool IsEmpty => card == null;

    public void Set(CardData data, CardButton source = null)
    {
        card = data;
        sourcePoolButton = source;

        if (iconImage) iconImage.sprite = data ? data.icon : null;
        if (nameText) nameText.text = data ? data.displayName : "";

        if (data == null)
        {
            if (iconImage) iconImage.enabled = false;
        }
        else
        {
            if (iconImage) iconImage.enabled = true;
        }
    }

    public void ClearToPool()
    {
        if (sourcePoolButton) sourcePoolButton.SetPicked(false);
        Set(null, null);
    }
}
