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
    public Image nameBG;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI typeTag;

    [Header("Pin")]
    [Tooltip("Sprite displayed on top of the card when it is added to the deck.")]
    public Sprite pinSprite;

    [Header("Runtime")]
    public CharacterSelectUIManager manager;
    public Context context = Context.Pool;
    [HideInInspector] public CardButton sourcePoolButton; // set on deck items to re-enable pool button

    private Button _btn;
    private Image _pinImage;

    private void Awake()
    {
        _btn = GetComponent<Button>();
        _btn.onClick.AddListener(OnClicked);
        ApplyTMPNameStyle(nameText);
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
        ShowPin(false);
        SetNameBG(true);
    }

    // -------- Bind for DECK ----------
    public void BindAsDeck(CardData data, CharacterSelectUIManager ui, CardButton poolButton)
    {
        card = data;
        manager = ui;
        context = Context.Deck;
        sourcePoolButton = poolButton;
        Refresh();
        SetPicked(false);
        ShowPin(true);
        SetNameBG(false);
    }

    /// <summary>Shows or hides the dark name background strip.</summary>
    private void SetNameBG(bool visible)
    {
        if (!nameBG) return;
        var c = nameBG.color;
        c.a = visible ? 0.65f : 0f;
        nameBG.color = c;
    }

    // -------- Pin overlay ----------
    private void ShowPin(bool show)
    {
        if (!pinSprite) return;

        if (show)
        {
            if (!_pinImage)
            {
                var pinGO = new GameObject("Pin", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                pinGO.transform.SetParent(transform, false);

                var rt = pinGO.GetComponent<RectTransform>();
                // Anchor to top-center, pivot at top-center
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot     = new Vector2(0.5f, 1f);

                // Size roughly 70% of card width; nudge upward so it overlaps the top edge
                float pinSize = 160f;
                rt.sizeDelta        = new Vector2(pinSize, pinSize);
                rt.anchoredPosition = new Vector2(20f, pinSize * 0.4f);

                _pinImage        = pinGO.GetComponent<Image>();
                _pinImage.sprite = pinSprite;
                _pinImage.preserveAspect = true;
                _pinImage.raycastTarget  = false;

                // Render above everything else in this card
                pinGO.transform.SetAsLastSibling();
            }
            else
            {
                _pinImage.gameObject.SetActive(true);
            }
        }
        else
        {
            if (_pinImage) _pinImage.gameObject.SetActive(false);
        }
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
        if (nameText) ApplyTMPNameStyle(nameText);
        if (typeTag)
        {
            if (card) { typeTag.text = card.isActive ? "ACTIVE" : "PASSIVE"; typeTag.gameObject.SetActive(true); }
            else typeTag.gameObject.SetActive(false);
        }
        Refresh();
    }
#endif

    private static void ApplyTMPNameStyle(TextMeshProUGUI t)
    {
        if (!t) return;
        t.enableWordWrapping = true;
        t.overflowMode = TextOverflowModes.Truncate;
        t.enableAutoSizing = true;
        t.fontSizeMin = 8;
        t.fontSizeMax = 18;
        t.richText = false;
        t.alignment = TextAlignmentOptions.Center;
    }
}
