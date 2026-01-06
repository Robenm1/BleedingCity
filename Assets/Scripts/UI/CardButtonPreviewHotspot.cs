using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Image))]
public class CardButtonPreviewHotspot : MonoBehaviour,
    IPointerEnterHandler, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
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

    private bool _pointerOver;
    private bool _pointerDown;
    private bool _previewOpen;
    private Coroutine _holdRoutine;

    void Start()
    {
        if (!sourceCardButton)
            sourceCardButton = GetComponentInParent<CardButton>();
        if (!sourceIcon)
            sourceIcon = AutoFindIconOnSource(sourceCardButton);

        var img = GetComponent<Image>();
        if (img) img.raycastTarget = true;
    }

    void OnDisable()
    {
        ClosePreview();
    }

    void OnDestroy()
    {
        ClosePreview();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _pointerOver = true;

        if (_holdRoutine == null && !_previewOpen)
        {
            _holdRoutine = StartCoroutine(HoldToPreview());
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _pointerDown = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _pointerDown = false;

        if (_previewOpen)
        {
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
        _pointerOver = false;
        _pointerDown = false;
        ClosePreview();
    }

    private void ClosePreview()
    {
        if (_holdRoutine != null)
        {
            StopCoroutine(_holdRoutine);
            _holdRoutine = null;
        }

        if (_previewOpen)
        {
            _previewOpen = false;
            if (CardPreviewManager.Instance) CardPreviewManager.Instance.Hide();
        }
    }

    private IEnumerator HoldToPreview()
    {
        float t = 0f;
        while (_pointerOver && t < holdTime)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!_pointerOver)
        {
            _holdRoutine = null;
            yield break;
        }

        var card = sourceCardButton ? sourceCardButton.card : null;
        var sprite = (sourceIcon && sourceIcon.sprite) ? sourceIcon.sprite : (card ? card.icon : null);

        Vector2 screenPos = GetPointerScreenPosition();
        if (CardPreviewManager.Instance)
        {
            CardPreviewManager.Instance.ShowAt(card, sprite, screenPos);
            _previewOpen = true;
        }

        while (_pointerOver)
        {
            if (followPointer && CardPreviewManager.Instance)
            {
                CardPreviewManager.Instance.FollowPointer(GetPointerScreenPosition());
            }
            yield return null;
        }

        ClosePreview();
        _holdRoutine = null;
    }

    private Vector2 GetPointerScreenPosition()
    {
        if (Mouse.current != null) return Mouse.current.position.ReadValue();
        return Input.mousePosition;
    }

    private Image AutoFindIconOnSource(CardButton cb)
    {
        if (!cb) return null;
        var images = cb.GetComponentsInChildren<Image>(true);
        Image best = null;

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
