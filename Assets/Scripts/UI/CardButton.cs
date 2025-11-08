using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class CardButton : MonoBehaviour
{
    public enum Context { Pool, Deck }

    [Header("Data")]
    public CardData card;

    [Header("Wiring")]
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI typeTag;

    [Header("Runtime")]
    public CharacterSelectUIManager manager;
    public Context context = Context.Pool;
    [HideInInspector] public CardButton sourcePoolButton; // set on deck items to re-enable pool button

    private Button _btn;

    private void Awake()
    {
        _btn = GetComponent<Button>();
        _btn.onClick.AddListener(OnClicked);
        ApplyTMPOneLine(nameText);
        Refresh();
    }

    // -------- Bind for POOL ----------
    public void Bind(CardData data, CharacterSelectUIManager ui)
    {
        card = data;
        manager = ui;
        context = Context.Pool;
        sourcePoolButton = null;
        Refresh();
        SetPicked(false);
    }

    // -------- Bind for DECK ----------
    public void BindAsDeck(CardData data, CharacterSelectUIManager ui, CardButton poolButton)
    {
        card = data;
        manager = ui;
        context = Context.Deck;
        sourcePoolButton = poolButton;
        Refresh();
        // deck items are always interactable (click to remove)
        SetPicked(false);
    }

    public void Refresh()
    {
        bool has = card != null;

        if (nameText) nameText.text = has ? card.displayName : string.Empty;

        if (typeTag)
        {
            if (has) { typeTag.text = card.isActive ? "ACTIVE" : "PASSIVE"; typeTag.gameObject.SetActive(true); }
            else typeTag.gameObject.SetActive(false);
        }

        if (iconImage)
        {
            iconImage.sprite = has ? card.icon : null;
            iconImage.preserveAspect = true;

            var rt = iconImage.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            rt.sizeDelta = Vector2.zero;

            var fitter = iconImage.GetComponent<AspectRatioFitter>();
            if (!fitter) fitter = iconImage.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent; // no stretch
        }

        if (_btn) _btn.interactable = has;
        gameObject.SetActive(has);
    }

    private void OnClicked()
    {
        if (!manager || !card) return;

        if (context == Context.Pool)
        {
            manager.TryAddCardToDeck(this);
        }
        else // Deck
        {
            manager.RemoveDeckCard(this);
        }
    }

    // Grey out a pool card after picked
    public void SetPicked(bool picked)
    {
        if (context == Context.Deck)
        { // deck items stay clickable (for remove)
            var cgDeck = GetComponent<CanvasGroup>();
            if (!cgDeck) cgDeck = gameObject.AddComponent<CanvasGroup>();
            cgDeck.alpha = 1f;
            _btn.interactable = true;
            return;
        }

        var cg = GetComponent<CanvasGroup>();
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();
        cg.alpha = picked ? 0.45f : 1f;
        _btn.interactable = !picked;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (iconImage && card) iconImage.sprite = card.icon;
        if (nameText) ApplyTMPOneLine(nameText);
        if (typeTag)
        {
            if (card) { typeTag.text = card.isActive ? "ACTIVE" : "PASSIVE"; typeTag.gameObject.SetActive(true); }
            else typeTag.gameObject.SetActive(false);
        }
        Refresh();
    }
#endif

    private static void ApplyTMPOneLine(TextMeshProUGUI t)
    {
        if (!t) return;
        t.enableWordWrapping = false;
        t.overflowMode = TextOverflowModes.Ellipsis;
        t.richText = false;
        t.alignment = TextAlignmentOptions.Midline;
    }
}
