using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Handles "marked" state on an enemy (DezertRose mark from Dezzo's 1st ability).
/// Displays a sigil sprite above the enemy HP bar, coordinating position with
/// EnemyBiteMark when both are active simultaneously.
/// </summary>
public class EnemyMark : MonoBehaviour
{
    [Tooltip("Is this enemy currently marked for priority targeting?")]
    public bool isMarked = false;

    [Tooltip("(Optional) Who applied the current mark.")]
    public Transform owner;

    [Header("Visual")]
    [Tooltip("The DezertRose mark sprite to display above the enemy.")]
    public Sprite markSprite;
    [Tooltip("Uniform scale applied to the mark sprite when shown alone (no active Bite Mark).")]
    public float sigilScale = 1f;
    [Tooltip("Sorting order for the sigil renderer. Should be above enemy sprites.")]
    public int sortingOrder = 301;
    [Tooltip("Extra world-space Y offset above the HP bar.")]
    public float markAboveBarOffset = 0.15f;
    [Tooltip("Horizontal distance each mark shifts when both DezertRose and Bite marks are active.")]
    public float dualMarkSpread = 0.3f;

    private SpriteRenderer _sigilRenderer;
    private EnemyBiteMark _biteMark;
    private Coroutine _markRoutine;
    private EnemyHealth _hp;

    // ---------- Burning Storm (static registry) ----------
    private struct BurnCfg
    {
        public float dps;
        public float tick;
        public bool requireSameOwner;
    }

    private static readonly Dictionary<Transform, BurnCfg> s_perOwner = new();
    private static bool s_hasGlobal = false;
    private static float s_globalDps = 0f;
    private static float s_globalTick = 0.25f;

    /// <summary>
    /// Enable Burning Storm DoT for this player (owner).
    /// If requireSameOwner is true, only enemies marked by this owner take DoT.
    /// </summary>
    public static void EnableBurningStormFor(Transform owner, float dps, float tickInterval, bool requireSameOwner = true)
    {
        dps = Mathf.Max(0f, dps);
        tickInterval = Mathf.Max(0.05f, tickInterval);

        if (requireSameOwner)
        {
            if (!owner) return;
            s_perOwner[owner] = new BurnCfg { dps = dps, tick = tickInterval, requireSameOwner = true };
        }
        else
        {
            s_hasGlobal = true;
            s_globalDps = dps;
            s_globalTick = tickInterval;
        }
    }

    /// <summary>
    /// Disable Burning Storm DoT for a specific owner.
    /// </summary>
    public static void DisableBurningStormFor(Transform owner)
    {
        if (!owner) return;
        s_perOwner.Remove(owner);
    }

    /// <summary>
    /// Disable the global Burning Storm DoT.
    /// </summary>
    public static void DisableGlobalBurningStorm()
    {
        s_hasGlobal = false;
        s_globalDps = 0f;
    }

    private void Awake()
    {
        _hp = GetComponent<EnemyHealth>();
        _biteMark = GetComponent<EnemyBiteMark>();

        if (_hp == null)
            Debug.LogWarning("[EnemyMark] EnemyHealth not found on this enemy.", this);

        EnsureSigil();
        HideSigil();
    }

    private void Update()
    {
        if (!isMarked || _sigilRenderer == null || !_sigilRenderer.enabled) return;

        // Sync scale to BiteMark's sigilScale when both are active; use own sigilScale otherwise.
        bool bothActive = _biteMark != null && _biteMark.isActive;
        float scale = bothActive ? Mathf.Max(0.05f, _biteMark.sigilScale) : Mathf.Max(0.05f, sigilScale);
        _sigilRenderer.transform.localScale = Vector3.one * scale;

        // Shift left when bite mark is also active so they sit side by side; centre otherwise.
        float xShift = bothActive ? -dualMarkSpread : 0f;

        // Position above the HP bar using EnemyHealth's own world offset as baseline.
        float barY = _hp != null ? _hp.worldOffset.y : 1.2f;
        _sigilRenderer.transform.position = (Vector2)transform.position
            + new Vector2(xShift, barY + markAboveBarOffset);

        // Keep the sprite upright — no spinning.
        _sigilRenderer.transform.rotation = Quaternion.identity;
    }

    // ---------- Public API ----------

    public void SetMarked(float duration)
    {
        if (_markRoutine != null) StopCoroutine(_markRoutine);
        _markRoutine = StartCoroutine(MarkFor(duration));
    }

    /// <summary>
    /// Mark and assign owner (enables same-owner DoT filtering).
    /// </summary>
    public void SetMarkedBy(Transform markerOwner, float duration)
    {
        owner = markerOwner;
        SetMarked(duration);
    }

    // ---------- Internal ----------

    private IEnumerator MarkFor(float duration)
    {
        isMarked = true;
        ShowSigil();

        float localTimer = 0f;
        float t = duration;

        while (t > 0f)
        {
            float dt = Time.deltaTime;
            t -= dt;

            float useDps = 0f;
            float useTick = 0.25f;

            if (owner != null && s_perOwner.TryGetValue(owner, out var cfg))
            {
                useDps = cfg.dps;
                useTick = cfg.tick;
            }
            else if (s_hasGlobal)
            {
                useDps = s_globalDps;
                useTick = s_globalTick;
            }

            if (useDps > 0f && _hp != null)
            {
                localTimer += dt;
                while (localTimer >= useTick)
                {
                    localTimer -= useTick;
                    _hp.TakeDamage(useDps * useTick);
                }
            }

            yield return null;
        }

        isMarked = false;
        _markRoutine = null;
        HideSigil();
    }

    private void EnsureSigil()
    {
        if (_sigilRenderer != null) return;

        var go = new GameObject("DezertRoseMarkSigil");
        go.transform.SetParent(transform, worldPositionStays: true);
        _sigilRenderer = go.AddComponent<SpriteRenderer>();
        _sigilRenderer.sortingOrder = sortingOrder;
        _sigilRenderer.enabled = false;
    }

    private void ShowSigil()
    {
        if (_sigilRenderer == null) return;

        if (markSprite != null) _sigilRenderer.sprite = markSprite;
        _sigilRenderer.sortingOrder = sortingOrder;

        // Match BiteMark's scale when both are active; use own sigilScale when alone.
        bool bothActive = _biteMark != null && _biteMark.isActive;
        float scale = bothActive ? Mathf.Max(0.05f, _biteMark.sigilScale) : Mathf.Max(0.05f, sigilScale);
        _sigilRenderer.transform.localScale = Vector3.one * scale;
        _sigilRenderer.enabled = true;
    }

    private void HideSigil()
    {
        if (_sigilRenderer != null) _sigilRenderer.enabled = false;
    }
}
