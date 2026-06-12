using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Centralises the visual layout of all mark sigils on one enemy.
/// Collects every <see cref="IMarkDisplay"/> component on the same GameObject,
/// sizes them uniformly (matching Owl's frost mark), and arranges them in a
/// centered row above the HP bar every LateUpdate.
///
/// Add this to any enemy prefab, or let mark components call
/// <see cref="EnsureOn"/> from their Awake so it is created automatically.
/// </summary>
[DisallowMultipleComponent]
public class MarkDisplayController : MonoBehaviour
{
    [Header("Layout")]
    [Tooltip("Uniform world-space scale applied to every mark sigil.")]
    public float markSize = 0.2f;

    [Tooltip("Horizontal gap (world units) between sigils when more than one mark is active.")]
    public float markSpacing = 0.22f;

    [Tooltip("Extra world-space Y above the HP bar's worldOffset, enough to clear the bar's visual height.")]
    public float markAboveBarOffset = 0.45f;

    private IMarkDisplay[] _marks;
    private EnemyHealth _hp;
    private readonly List<SpriteRenderer> _activeRenderers = new List<SpriteRenderer>(4);

    private void Start()
    {
        _hp = GetComponent<EnemyHealth>();
        RefreshMarkList();
    }

    private void LateUpdate()
    {
        if (_marks == null) RefreshMarkList();

        _activeRenderers.Clear();
        foreach (var mark in _marks)
        {
            if (mark.IsMarkVisible && mark.MarkSpriteRenderer != null)
                _activeRenderers.Add(mark.MarkSpriteRenderer);
        }

        if (_activeRenderers.Count == 0) return;

        float barY = _hp != null ? _hp.worldOffset.y : 1.2f;
        float yPos = transform.position.y + barY + markAboveBarOffset;
        float totalWidth = (_activeRenderers.Count - 1) * markSpacing;
        float startX = transform.position.x - totalWidth * 0.5f;

        for (int i = 0; i < _activeRenderers.Count; i++)
        {
            var sr = _activeRenderers[i];
            sr.transform.position = new Vector3(startX + i * markSpacing, yPos, transform.position.z);
            sr.transform.localScale = new Vector3(markSize, markSize, 1f);
            sr.transform.rotation = Quaternion.identity;
        }
    }

    /// <summary>Returns the number of currently active and visible marks.</summary>
    public int ActiveMarkCount
    {
        get
        {
            if (_marks == null) return 0;
            int count = 0;
            foreach (var mark in _marks)
                if (mark.IsMarkVisible) count++;
            return count;
        }
    }

    /// <summary>
    /// Rebuilds the internal mark list. Call this if IMarkDisplay components are
    /// added to this GameObject at runtime after Start has already run.
    /// </summary>
    public void RefreshMarkList() => _marks = GetComponents<IMarkDisplay>();

    /// <summary>
    /// Ensures a <see cref="MarkDisplayController"/> exists on <paramref name="go"/>
    /// and refreshes its mark list so dynamically added marks are included.
    /// Safe to call from Awake.
    /// </summary>
    public static MarkDisplayController EnsureOn(GameObject go)
    {
        var ctrl = go.GetComponent<MarkDisplayController>();
        if (ctrl == null) ctrl = go.AddComponent<MarkDisplayController>();
        // Always refresh so a newly added mark component is immediately registered,
        // even if the controller's Start() has already run.
        ctrl.RefreshMarkList();
        return ctrl;
    }
}
