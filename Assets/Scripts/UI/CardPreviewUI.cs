using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardPreviewUI : MonoBehaviour
{
    public Canvas rootCanvas;            // Overlay canvas (assign)
    public RectTransform panel;          // This preview panel's RectTransform
    public Image icon;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public float padding = 12f;

    private CanvasGroup cg;

    void Awake()
    {
        if (!rootCanvas) rootCanvas = GetComponentInParent<Canvas>();
        cg = GetComponent<CanvasGroup>();
        HideImmediate();
    }

    public void Show(CardData card, Vector2 screenPos)
    {
        if (!card) { HideImmediate(); return; }

        if (icon) icon.sprite = card.icon;
        if (nameText) nameText.text = card.displayName;
        if (descriptionText) descriptionText.text = card.description;

        PositionNear(screenPos);
        gameObject.SetActive(true);
        if (cg) { cg.alpha = 1f; cg.interactable = false; cg.blocksRaycasts = false; }
    }

    public void Hide()
    {
        if (cg) cg.alpha = 0f;
        gameObject.SetActive(false);
    }

    public void HideImmediate()
    {
        if (cg) cg.alpha = 0f;
        gameObject.SetActive(false);
    }

    private void PositionNear(Vector2 screenPos)
    {
        if (!rootCanvas || !panel) return;

        var cam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : rootCanvas.worldCamera;

        // Offset a bit from the cursor/finger
        Vector2 desired = screenPos + new Vector2(16f, -16f);

        // Convert to local canvas space
        RectTransform canvasRT = rootCanvas.transform as RectTransform;
        UnityEngine.RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, desired, cam, out var localPos);

        // Clamp inside canvas
        var canvasRect = canvasRT.rect;
        var size = panel.rect.size;
        localPos.x = Mathf.Clamp(localPos.x, canvasRect.xMin + size.x * 0.5f + padding, canvasRect.xMax - size.x * 0.5f - padding);
        localPos.y = Mathf.Clamp(localPos.y, canvasRect.yMin + size.y * 0.5f + padding, canvasRect.yMax - size.y * 0.5f - padding);

        panel.anchoredPosition = localPos;
    }
}
