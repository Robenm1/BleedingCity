using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Applied to an enemy when hit by the Hell Bomb explosion.
/// While active, all summon damage dealt to this enemy is multiplied.
/// Displays the Hell's Justice icon inside the enemy's existing HP canvas,
/// tracking its world position the same way EnemyHealth tracks the HP bar.
/// </summary>
public class HellsJusticeMark : MonoBehaviour
{
    // ── Summon damage multiplier ───────────────────────────────────────────
    public float summonDamageMultiplier = 1.5f;

    // ── Icon config ────────────────────────────────────────────────────────
    [Tooltip("Sprite to display next to the HP bar while marked.")]
    public Sprite iconSprite;

    [Tooltip("Size of the icon inside the canvas (pixels).")]
    public Vector2 iconSize = new Vector2(26f, 26f);

    [Tooltip("Additional screen-space pixel offset applied on top of the HP bar's position.")]
    public Vector2 iconScreenOffset = new Vector2(0f, 22f);

    // ── State ──────────────────────────────────────────────────────────────
    private bool _isActive = false;
    private Coroutine _markRoutine;

    // ── UI references resolved once from EnemyHealth ───────────────────────
    private RectTransform _iconRT;
    private Canvas _hpCanvas;
    private RectTransform _canvasRect;
    private RectTransform _hpUIRoot;
    private Camera _uiCamera;

    public bool IsActive => _isActive;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnDestroy()
    {
        DestroyIcon();
    }

    private void LateUpdate()
    {
        if (_iconRT == null || !_iconRT.gameObject.activeSelf) return;

        // Mirror exactly what EnemyHealth does for hpUIRoot, then add the screen-space offset.
        if (_hpUIRoot != null && _hpUIRoot.gameObject.activeSelf)
        {
            // Ride on top of the HP bar's already-computed position.
            _iconRT.anchoredPosition = _hpUIRoot.anchoredPosition + iconScreenOffset;
        }
        else
        {
            // HP bar is hidden — compute position ourselves using the same world-to-canvas logic.
            var eh = GetComponent<EnemyHealth>();
            Vector3 worldOffset = eh != null ? eh.worldOffset : new Vector3(0f, 1.2f, 0f);
            Vector3 worldPos = transform.position + worldOffset;

            Camera cam = (_hpCanvas != null && _hpCanvas.renderMode == RenderMode.ScreenSpaceCamera)
                ? _uiCamera
                : null;

            Vector2 screenPoint = cam != null
                ? (Vector2)cam.WorldToScreenPoint(worldPos)
                : Camera.main != null
                    ? (Vector2)Camera.main.WorldToScreenPoint(worldPos)
                    : (Vector2)worldPos;

            if (_canvasRect != null &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPoint, cam, out var local))
            {
                _iconRT.anchoredPosition = local + iconScreenOffset;
            }
        }
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>Applies or refreshes the mark for the given duration.</summary>
    public void Apply(float duration)
    {
        ResolveReferences();

        if (_markRoutine != null)
            StopCoroutine(_markRoutine);

        _markRoutine = StartCoroutine(MarkRoutine(duration));
    }

    /// <summary>Returns the active summon damage multiplier.</summary>
    public float GetDamageMultiplier() => _isActive ? summonDamageMultiplier : 1f;

    // ── Internals ──────────────────────────────────────────────────────────

    private void ResolveReferences()
    {
        if (_hpCanvas != null) return;

        var eh = GetComponent<EnemyHealth>();
        if (eh == null) return;

        _hpUIRoot = eh.hpUIRoot;
        _uiCamera = eh.uiCamera != null ? eh.uiCamera : Camera.main;

        if (_hpUIRoot != null)
        {
            _hpCanvas   = _hpUIRoot.GetComponentInParent<Canvas>();
            _canvasRect = _hpCanvas != null ? _hpCanvas.GetComponent<RectTransform>() : null;
        }
    }

    private IEnumerator MarkRoutine(float duration)
    {
        _isActive = true;
        ShowIcon();

        yield return new WaitForSeconds(duration);

        _isActive = false;
        DestroyIcon();
        _markRoutine = null;
    }

    private void ShowIcon()
    {
        if (iconSprite == null)
        {
            Debug.LogWarning("[HellsJusticeMark] iconSprite is not assigned.");
            return;
        }

        if (_hpCanvas == null)
        {
            Debug.LogWarning("[HellsJusticeMark] Could not find HP canvas on enemy.");
            return;
        }

        if (_iconRT == null)
        {
            var go = new GameObject("HellsJusticeIcon");
            go.transform.SetParent(_hpCanvas.transform, false);

            _iconRT           = go.AddComponent<RectTransform>();
            _iconRT.anchorMin = new Vector2(0.5f, 0.5f);
            _iconRT.anchorMax = new Vector2(0.5f, 0.5f);
            _iconRT.pivot     = new Vector2(0.5f, 0.5f);
            _iconRT.sizeDelta = iconSize;

            var img = go.AddComponent<Image>();
            img.sprite         = iconSprite;
            img.preserveAspect = true;

            // Ensure it renders on top of everything else in the canvas.
            var overrideCanvas             = go.AddComponent<Canvas>();
            overrideCanvas.overrideSorting = true;
            overrideCanvas.sortingOrder    = 999;
        }

        _iconRT.gameObject.SetActive(true);
    }

    private void DestroyIcon()
    {
        if (_iconRT != null)
        {
            Destroy(_iconRT.gameObject);
            _iconRT = null;
        }
    }
}
