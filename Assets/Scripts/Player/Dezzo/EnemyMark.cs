using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Handles "marked" state on an enemy. Also contains the optional
/// Burning Storm DoT logic when enabled via EnemyMark.EnableBurningStormFor(...).
/// </summary>
public class EnemyMark : MonoBehaviour
{
    [Tooltip("Is this enemy currently marked for priority targeting?")]
    public bool isMarked = false;

    [Tooltip("(Optional) Who applied the current mark.")]
    public Transform owner; // used for same-owner-only DoT filtering

    private Coroutine markRoutine;
    private EnemyHealth _hp;

    // ---------- Burning Storm (static registry) ----------
    // Per-owner settings (same-owner-only DoT).
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
    /// If false, this call configures a GLOBAL DoT that applies to any marked enemy.
    /// </summary>
    public static void EnableBurningStormFor(Transform owner, float dps, float tickInterval, bool requireSameOwner = true)
    {
        dps = Mathf.Max(0f, dps);
        tickInterval = Mathf.Max(0.05f, tickInterval);

        if (requireSameOwner)
        {
            if (!owner) return;
            s_perOwner[owner] = new BurnCfg
            {
                dps = dps,
                tick = tickInterval,
                requireSameOwner = true
            };
        }
        else
        {
            s_hasGlobal = true;
            s_globalDps = dps;
            s_globalTick = tickInterval;
        }
    }

    /// <summary>
    /// Disable Burning Storm DoT for a specific owner (same-owner entries).
    /// </summary>
    public static void DisableBurningStormFor(Transform owner)
    {
        if (!owner) return;
        if (s_perOwner.ContainsKey(owner)) s_perOwner.Remove(owner);
    }

    /// <summary>
    /// Disable the global Burning Storm DoT (requireSameOwner=false entries).
    /// </summary>
    public static void DisableGlobalBurningStorm()
    {
        s_hasGlobal = false;
        s_globalDps = 0f;
    }

    private void Awake()
    {
        _hp = GetComponent<EnemyHealth>();
        if (_hp == null)
            Debug.LogWarning("[EnemyMark] EnemyHealth not found on this enemy.", this);
    }

    // ---------- Public API (original) ----------
    public void SetMarked(float duration)
    {
        // keep previous owner as-is
        if (markRoutine != null) StopCoroutine(markRoutine);
        markRoutine = StartCoroutine(MarkFor(duration));
    }

    /// <summary>
    /// Optional: mark and set owner (recommended if you want same-owner DoT filtering).
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

        // DoT timers for this mark instance
        float localTimer = 0f; // accumulates time between ticks

        // Choose which config applies each frame:
        // 1) Same-owner config (if exists and matches this.owner)
        // 2) Global config (if enabled)
        // If neither, no DoT.

        float t = duration;
        while (t > 0f)
        {
            float dt = Time.deltaTime;
            t -= dt;

            // Handle DoT if available
            float useDps = 0f;
            float useTick = 0.25f;

            bool hasOwnerCfg = false;
            if (owner != null && s_perOwner.TryGetValue(owner, out var cfg))
            {
                // same-owner DoT applies
                useDps = cfg.dps;
                useTick = cfg.tick;
                hasOwnerCfg = true;
            }
            else if (s_hasGlobal)
            {
                // fallback to global DoT (any mark)
                useDps = s_globalDps;
                useTick = s_globalTick;
            }

            if (useDps > 0f && _hp != null)
            {
                localTimer += dt;
                while (localTimer >= useTick)
                {
                    localTimer -= useTick;
                    float dmg = useDps * useTick; // DPS → per-tick
                    _hp.TakeDamage(dmg);
                }
            }

            yield return null;
        }

        isMarked = false;
        markRoutine = null;
    }
}
