using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardPreviewManager : MonoBehaviour
{
    public static CardPreviewManager Instance { get; private set; }

    [Header("Preview Panel (scene instance)")]
    [Tooltip("Panel root (must be under a Canvas). Starts inactive.")]
    public RectTransform previewPanel;
    public Image previewIcon;
    public TMP_Text previewName;
    public TMP_Text previewDescription;

    [Header("Placement")]
    public Vector2 followOffset = new Vector2(24f, -24f);
    public bool clampInsideCanvas = true;

    private Canvas _canvas;
    private RectTransform _canvasRT;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _canvas = GetComponentInParent<Canvas>();
        if (!_canvas) _canvas = FindObjectOfType<Canvas>();
        if (_canvas) _canvasRT = _canvas.transform as RectTransform;

        if (previewPanel)
        {
            previewPanel.gameObject.SetActive(false);
            previewPanel.SetAsLastSibling();
        }
    }

    public void ShowAt(CardData card, Sprite art, Vector2 screenPos)
    {
        if (!previewPanel || !_canvas) return;

        if (previewName) previewName.text = card ? card.displayName : "";
        if (previewDescription) previewDescription.text = card ? card.description : "";
        if (previewIcon)
        {
            previewIcon.sprite = art ? art : (card ? card.icon : null);
            previewIcon.preserveAspect = true;
        }

        previewPanel.gameObject.SetActive(true);
        previewPanel.SetAsLastSibling();
        PositionAtScreen(screenPos);
    }

    public void FollowPointer(Vector2 screenPos)
    {
        if (!previewPanel || !previewPanel.gameObject.activeSelf) return;
        PositionAtScreen(screenPos);
    }

    public void Hide()
    {
        if (!previewPanel) return;
        previewPanel.gameObject.SetActive(false);
    }

    private void PositionAtScreen(Vector2 screenPos)
    {
        if (!_canvasRT || !previewPanel) return;

        Camera cam = (_canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            ? null
            : (_canvas.worldCamera ? _canvas.worldCamera : Camera.main);

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRT, screenPos, cam, out var local))
            return;

        local += followOffset;

        if (clampInsideCanvas)
        {
            Vector2 half = previewPanel.rect.size * 0.5f;
            Vector2 min = _canvasRT.rect.min + half;
            Vector2 max = _canvasRT.rect.max - half;
            local.x = Mathf.Clamp(local.x, min.x, max.x);
            local.y = Mathf.Clamp(local.y, min.y, max.y);
        }

        previewPanel.anchoredPosition = local;
    }
}
