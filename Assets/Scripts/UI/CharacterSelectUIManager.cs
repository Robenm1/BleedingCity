using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class CharacterSelectUIManager : MonoBehaviour
{
    [Header("Large Display (Left page)")]
    public Image selectedIconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;

    [Header("Pool Panels in #6 (fixed panels)")]
    public RectTransform[] topRowSlots;
    public RectTransform[] bottomRowSlots;

    [Header("Deck Panels (#4)")]
    public RectTransform[] deckSlots;

    [Header("Prefabs")]
    public CardButton cardButtonPrefab;

    [Header("Global Cards (bottom row)")]
    public List<CardData> globalCards = new List<CardData>();

    [Header("Character Buttons (#5)")]
    public CharacterButton[] characterButtons;

    [Header("Lock UX")]
    [Tooltip("If ON, clicking another character while locked shakes the non-global deck cards.")]
    public bool enableLockShake = false;
    public float shakeDuration = 0.6f;
    public float shakeAmplitude = 6f;
    public float shakeSpeed = 20f;
    public float shakeRotDegrees = 5f;

    [Header("State (read-only)")]
    public CharacterData currentSelection;

    // runtime
    private readonly List<CardButton> _spawnedPoolButtons = new();
    private readonly List<CardButton> _deckButtons = new();
    private readonly Dictionary<CardButton, Coroutine> _shakeRoutines = new();

    private CharacterData _lockedCharacter = null;

    // portrait cache
    private RectTransform _iconRT;
    private Vector2 _originalSize;

    private void Awake()
    {
        if (selectedIconImage)
        {
            _iconRT = selectedIconImage.rectTransform;
            _originalSize = _iconRT.sizeDelta;
            selectedIconImage.preserveAspect = true;
            selectedIconImage.sprite = null;
            selectedIconImage.enabled = false;
        }
        if (nameText) nameText.text = string.Empty;
        if (descriptionText) descriptionText.text = string.Empty;

        ShowAllSlots(topRowSlots, true);
        ShowAllSlots(bottomRowSlots, true);
        ShowAllSlots(deckSlots, true);

        ClearPoolVisualsOnly();
        ClearDeckCompletely();

        UnlockAllCharacters();
    }

    // ===== Character select (from CharacterButton) =====
    public void SelectCharacter(CharacterData data)
    {
        // If locked to another character and user clicked a different one: do NOTHING (optional shake).
        if (_lockedCharacter != null && data != _lockedCharacter)
        {
            if (enableLockShake) ShakeNonGlobalDeckCards();
            return; // <-- critical: no rebuild, no text/icon changes => no twitch
        }

        // If the selection didn't actually change, do nothing.
        if (data == currentSelection)
            return;

        currentSelection = data;

        // Update big portrait + texts
        if (selectedIconImage)
        {
            if (data && data.selectedIcon)
            {
                selectedIconImage.enabled = true;
                selectedIconImage.sprite = data.selectedIcon;
                if (_iconRT) _iconRT.sizeDelta = _originalSize;
                selectedIconImage.preserveAspect = true;
            }
            else
            {
                selectedIconImage.sprite = null;
                selectedIconImage.enabled = false;
            }
        }
        if (nameText) nameText.text = data ? data.displayName : string.Empty;
        if (descriptionText) descriptionText.text = data ? data.description : string.Empty;

        // Rebuild pool only when we truly changed character
        RebuildPoolIntoFixedSlots();
        RefreshPoolPickedStates();
    }

    // ===== Pool → Deck (panel children) =====
    public bool TryAddCardToDeck(CardButton poolButton)
    {
        if (!poolButton || poolButton.card == null) return false;

        // If locked to another character, block non-global picks
        if (_lockedCharacter != null && !poolButton.card.isGlobal && currentSelection != _lockedCharacter)
        {
            if (enableLockShake) ShakeNonGlobalDeckCards();
            return false;
        }

        var slot = FirstEmptyDeckPanel();
        if (!slot)
        {
            Debug.Log("[CharacterSelectUIManager] Deck is full.");
            return false;
        }

        // Lock others after first non-global pick
        if (_lockedCharacter == null && currentSelection != null && !poolButton.card.isGlobal)
        {
            _lockedCharacter = currentSelection;
            LockOtherCharacters(_lockedCharacter);
        }

        var deckBtn = Instantiate(cardButtonPrefab, slot);
        StretchToSlot(deckBtn.GetComponent<RectTransform>());
        deckBtn.BindAsDeck(poolButton.card, this, poolButton);
        _deckButtons.Add(deckBtn);

        poolButton.SetPicked(true);
        RefreshPoolPickedStates();
        return true;
    }

    public void RemoveDeckCard(CardButton deckButton)
    {
        if (!deckButton) return;

        StopShake(deckButton);

        if (deckButton.sourcePoolButton)
            deckButton.sourcePoolButton.SetPicked(false);

        _deckButtons.Remove(deckButton);
        Destroy(deckButton.gameObject);

        RefreshPoolPickedStates();

        if (NoNonGlobalCardsInDeck())
        {
            _lockedCharacter = null;
            UnlockAllCharacters();
        }
    }

    // ===== Build pool into fixed slots =====
    private void RebuildPoolIntoFixedSlots()
    {
        ClearPoolVisualsOnly();

        var specials = (currentSelection && currentSelection.specialCards != null)
            ? currentSelection.specialCards
            : new List<CardData>();
        FillRow(topRowSlots, specials);

        FillRow(bottomRowSlots, globalCards);
    }

    private void FillRow(RectTransform[] slots, List<CardData> cards)
    {
        if (slots == null || slots.Length == 0) return;
        int count = Mathf.Min(slots.Length, cards != null ? cards.Count : 0);

        for (int i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            if (!slot) continue;

            DestroyOnlyChildrenWith<CardButton>(slot);

            if (i < count)
            {
                var data = cards[i];
                if (!data) continue;

                var btn = Instantiate(cardButtonPrefab, slot);
                StretchToSlot(btn.GetComponent<RectTransform>());
                btn.Bind(data, this); // POOL context
                _spawnedPoolButtons.Add(btn);
            }
        }
    }

    // ===== Helpers =====
    private RectTransform FirstEmptyDeckPanel()
    {
        if (deckSlots == null) return null;
        foreach (var slot in deckSlots)
        {
            if (!slot) continue;
            bool hasCard = false;
            for (int i = 0; i < slot.childCount; i++)
            {
                if (slot.GetChild(i).GetComponent<CardButton>())
                {
                    hasCard = true;
                    break;
                }
            }
            if (!hasCard) return slot;
        }
        return null;
    }

    private bool NoNonGlobalCardsInDeck()
    {
        foreach (var b in _deckButtons)
        {
            if (b && b.card && !b.card.isGlobal) return false;
        }
        return true;
    }

    private bool IsCardAlreadyInDeck(CardData data)
    {
        foreach (var b in _deckButtons)
            if (b && b.card == data) return true;
        return false;
    }

    private void StretchToSlot(RectTransform rt)
    {
        if (!rt) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
    }

    private void ClearPoolVisualsOnly()
    {
        foreach (var b in _spawnedPoolButtons)
            if (b) Destroy(b.gameObject);
        _spawnedPoolButtons.Clear();

        CleanupRow(topRowSlots);
        CleanupRow(bottomRowSlots);
    }

    private void CleanupRow(RectTransform[] slots)
    {
        if (slots == null) return;
        foreach (var s in slots) DestroyOnlyChildrenWith<CardButton>(s);
    }

    private void DestroyOnlyChildrenWith<T>(RectTransform parent) where T : Component
    {
        if (!parent) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            var c = parent.GetChild(i);
            if (c.GetComponent<T>()) Destroy(c.gameObject);
        }
    }

    private void ShowAllSlots(RectTransform[] slots, bool show)
    {
        if (slots == null) return;
        foreach (var s in slots) if (s) s.gameObject.SetActive(show);
    }

    private void ClearDeckCompletely()
    {
        foreach (var kv in _shakeRoutines)
            if (kv.Key) ResetCardTransform(kv.Key);
        _shakeRoutines.Clear();

        for (int i = _deckButtons.Count - 1; i >= 0; i--)
            if (_deckButtons[i]) Destroy(_deckButtons[i].gameObject);
        _deckButtons.Clear();

        CleanupRow(deckSlots);
    }

    // ===== Character locking visuals =====
    private void LockOtherCharacters(CharacterData keepActive)
    {
        if (characterButtons == null) return;
        foreach (var cb in characterButtons)
        {
            if (!cb) continue;
            bool isOwner = (cb.character == keepActive);
            cb.SetLocked(!isOwner); // owner normal; others dark (still clickable if your CharacterButton allows)
        }
    }

    private void UnlockAllCharacters()
    {
        if (characterButtons == null) return;
        foreach (var cb in characterButtons)
            if (cb) cb.SetLocked(false);
    }

    // ===== Keep pool greying in sync with deck =====
    private void RefreshPoolPickedStates()
    {
        for (int i = 0; i < _spawnedPoolButtons.Count; i++)
        {
            var btn = _spawnedPoolButtons[i];
            if (!btn || btn.card == null) continue;
            bool alreadyInDeck = IsCardAlreadyInDeck(btn.card);
            btn.SetPicked(alreadyInDeck);
        }
    }

    // ===== Shake system (only used if enableLockShake = true) =====
    private void ShakeNonGlobalDeckCards()
    {
        if (!enableLockShake) return;
        foreach (var b in _deckButtons)
        {
            if (!b || b.card == null) continue;
            if (b.card.isGlobal) continue;
            StartShake(b);
        }
    }

    private void StartShake(CardButton deckBtn)
    {
        if (!deckBtn) return;
        StopShake(deckBtn);
        var co = StartCoroutine(ShakeCardRoutine(deckBtn));
        _shakeRoutines[deckBtn] = co;
    }

    private void StopShake(CardButton deckBtn)
    {
        if (!deckBtn) return;

        if (_shakeRoutines.TryGetValue(deckBtn, out var running) && running != null)
        {
            StopCoroutine(running);
            _shakeRoutines.Remove(deckBtn);
        }
        ResetCardTransform(deckBtn);
    }

    private void ResetCardTransform(CardButton deckBtn)
    {
        var rt = deckBtn ? deckBtn.GetComponent<RectTransform>() : null;
        if (!rt) return;
        rt.anchoredPosition = Vector2.zero;
        rt.localRotation = Quaternion.identity;
    }

    private IEnumerator ShakeCardRoutine(CardButton deckBtn)
    {
        var rt = deckBtn.GetComponent<RectTransform>();
        if (!rt) yield break;

        rt.anchoredPosition = Vector2.zero;
        rt.localRotation = Quaternion.identity;

        float t = 0f;
        while (t < shakeDuration)
        {
            t += Time.unscaledDeltaTime;
            float s = Mathf.Sin(t * shakeSpeed);
            rt.anchoredPosition = new Vector2(s * shakeAmplitude, 0f);
            rt.localRotation = Quaternion.Euler(0f, 0f, s * shakeRotDegrees);
            yield return null;
        }

        rt.anchoredPosition = Vector2.zero;
        rt.localRotation = Quaternion.identity;
        _shakeRoutines.Remove(deckBtn);
    }
}
