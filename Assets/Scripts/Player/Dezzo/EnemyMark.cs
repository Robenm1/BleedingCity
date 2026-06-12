using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Handles the "marked" state on an enemy (DezertRose mark from Dezzo's 1st ability).
/// Displays a sigil sprite whose position and size are managed by
/// <see cref="MarkDisplayController"/> so it stays consistent with all other marks.
/// </summary>
public class EnemyMark : MonoBehaviour, IMarkDisplay
{
    [Tooltip("Is this enemy currently marked for priority targeting?")]
    public bool isMarked = false;

    [Tooltip("(Optional) Who applied the current mark.")]
    public Transform owner;

    [Header("Visual")]
    [Tooltip("The DezertRose mark sprite to display above the enemy.")]
    public Sprite markSprite;
    [Tooltip("Sorting order for the sigil renderer. Should be above enemy sprites.")]
    public int sortingOrder = 301;

    private SpriteRenderer _sigilRenderer;
    private Coroutine _markRoutine;
    private EnemyHealth _hp;

    // ---- IMarkDisplay ----
    /// <inheritdoc/>
    public bool IsMarkVisible => isMarked && _sigilRenderer != null && _sigilRenderer.enabled;

    /// <inheritdoc/>
    public SpriteRenderer MarkSpriteRenderer => _sigilRenderer;

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
        // Ensure the centralised layout controller is present on this enemy.
        MarkDisplayController.EnsureOn(gameObject);

        if (_hp == null)
            Debug.LogWarning("[EnemyMark] EnemyHealth not found on this enemy.", this);

        EnsureSigil();
        HideSigil();
    }

    // ---------- Public API ----------

    /// <summary>Marks this enemy for the specified duration.</summary>
    public void SetMarked(float duration)
    {
        if (_markRoutine != null) StopCoroutine(_markRoutine);
        _markRoutine = StartCoroutine(MarkFor(duration));
    }

    /// <summary>
    /// Marks this enemy and assigns owner (enables same-owner DoT filtering).
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
        _sigilRenderer.enabled = true;
        // Position and scale are set by MarkDisplayController each LateUpdate.
    }

    private void HideSigil()
    {
        if (_sigilRenderer != null) _sigilRenderer.enabled = false;
    }
}
