using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem; // New Input System
using System.Collections;

[RequireComponent(typeof(Image))] // simple raycast target for hotspot
public class CardButtonPreviewHotspot : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Source")]
    [Tooltip("CardButton on the small card root. If empty, will auto-find on start.")]
    public CardButton sourceCardButton;

    [Tooltip("The SMALL card's art Image (not the preview). If empty, auto-find.")]
    public Image sourceIcon;

    [Header("Behavior")]
    [Tooltip("Hold time (unscaled) before preview shows.")]
    public float holdTime = 0.35f;

    [Tooltip("If true, preview follows the pointer while held.")]
    public bool followPointer = false;

    private bool _pointerDown;
    private bool _previewOpen;
    private Coroutine _holdRoutine;

    void Start()
    {
        if (!sourceCardButton)
            sourceCardButton = GetComponentInParent<CardButton>();
        if (!sourceIcon)
            sourceIcon = AutoFindIconOnSource(sourceCardButton);

        // Ensure the hotspot has a RaycastTarget image
        var img = GetComponent<Image>();
        if (img) img.raycastTarget = true;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _pointerDown = true;
        _holdRoutine = StartCoroutine(HoldToPreview());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _pointerDown = false;

        if (_holdRoutine != null)
        {
            StopCoroutine(_holdRoutine);
            _holdRoutine = null;
        }

        // If preview is open, close it and CANCEL the click so the card isn’t selected
        if (_previewOpen)
        {
            _previewOpen = false;
            if (CardPreviewManager.Instance) CardPreviewManager.Instance.Hide();

            // Cancel the click on release (prevents selection)
            if (eventData != null)
            {
                eventData.pointerPress = null;
                eventData.rawPointerPress = null;
                eventData.eligibleForClick = false;
                eventData.clickCount = 0;
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // If you want leaving to cancel before it opens
        if (!_previewOpen)
        {
            _pointerDown = false;
            if (_holdRoutine != null)
            {
                StopCoroutine(_holdRoutine);
                _holdRoutine = null;
            }
        }
    }

    private IEnumerator HoldToPreview()
    {
        float t = 0f;
        while (_pointerDown && t < holdTime)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!_pointerDown) yield break;

        // OPEN preview
        var card = sourceCardButton ? sourceCardButton.card : null;
        var sprite = (sourceIcon && sourceIcon.sprite) ? sourceIcon.sprite : (card ? card.icon : null);

        Vector2 screenPos = GetPointerScreenPosition();
        if (CardPreviewManager.Instance)
        {
            CardPreviewManager.Instance.ShowAt(card, sprite, screenPos);
            _previewOpen = true;
        }

        // While holding, optionally follow pointer
        while (_pointerDown && _previewOpen && followPointer)
        {
            if (CardPreviewManager.Instance)
                CardPreviewManager.Instance.FollowPointer(GetPointerScreenPosition());
            yield return null;
        }
    }

    private Vector2 GetPointerScreenPosition()
    {
        if (Mouse.current != null) return Mouse.current.position.ReadValue();
        // Fallback, in case old input still present
        return Input.mousePosition;
    }

    private Image AutoFindIconOnSource(CardButton cb)
    {
        if (!cb) return null;
        var images = cb.GetComponentsInChildren<Image>(true);
        Image best = null;

        // Prefer sensibly-named art images
        foreach (var img in images)
        {
            if (!img) continue;
            var n = img.name.ToLower();
            if (n.Contains("icon") || n.Contains("art") || n.Contains("thumb") || n.Contains("portrait"))
            { best = img; break; }
        }
        if (!best && images.Length > 0)
        {
            foreach (var img in images)
            {
                if (!img) continue;
                var n = img.name.ToLower();
                if (!(n.Contains("bg") || n.Contains("background") || n.Contains("frame") || n.Contains("border")))
                { best = img; break; }
            }
        }
        return best;
    }
}
