// Assets/Scripts/Cards/Dezzo/RewindRuntime.cs
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerStats))]
public class RewindRuntime : MonoBehaviour
{
    [Header("Set by SO")]
    public float reviveDuration = 2.0f;
    public float healPercent = 0.5f;
    public bool freezeSharks = true;
    public bool lockControls = true;
    public bool invulnerableWhileReviving = true;
    public string invulnerableLayerName = "";

    // cached
    private PlayerStats stats;
    private DezzoSharkManager sharkMgr;

    // state
    private bool usedOnce = false;
    private bool reviving = false;

    // optional: things to disable during revive if present
    private MonoBehaviour moveController;   // your PlayerMovement script (if any)
    private MonoBehaviour inputController;  // your PlayerControls/Input relay (if any)

    // layer restore
    private int originalLayer = -1;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
        sharkMgr = GetComponent<DezzoSharkManager>();

        // Try to auto-find common movement/control scripts (optional)
        moveController = GetComponent<MonoBehaviour>(); // placeholder; leave null unless you assign explicitly
        // If you want, expose public fields to assign specific scripts via inspector:
        // public MonoBehaviour movementToDisable;
        // public MonoBehaviour controlsToDisable;
    }

    private void Update()
    {
        if (usedOnce || reviving) return;
        if (stats == null) return;

        // Intercept "death" without changing existing code:
        // If health hits 0 or below, we consume the revive and run the sequence.
        if (stats.currentHealth <= 0f)
        {
            StartCoroutine(ReviveSequence());
        }
    }

    private IEnumerator ReviveSequence()
    {
        usedOnce = true;
        reviving = true;

        // 1) Freeze sharks (safest way without editing shark scripts: disable their components)
        if (freezeSharks && sharkMgr != null && sharkMgr.sharks != null)
        {
            for (int i = 0; i < sharkMgr.sharks.Length; i++)
            {
                if (sharkMgr.sharks[i])
                    sharkMgr.sharks[i].enabled = false;
            }
        }

        // 2) Lock player controls/movement (only if present)
        if (lockControls)
        {
            TrySetEnabled(moveController, false);
            TrySetEnabled(inputController, false);
        }

        // 3) Make invulnerable (optional) — simplest: change layer temporarily
        if (invulnerableWhileReviving && !string.IsNullOrEmpty(invulnerableLayerName))
        {
            originalLayer = gameObject.layer;
            int invLayer = LayerMask.NameToLayer(invulnerableLayerName);
            if (invLayer >= 0) SetLayerRecursively(gameObject, invLayer);
        }

        // 4) Animate HP bar from 0 → targetHP over reviveDuration
        float targetHP = Mathf.Max(1f, stats.maxHealth * healPercent);
        float startHP = Mathf.Clamp(stats.currentHealth, 0f, stats.maxHealth);
        // Ensure we show 0 at start for the fill animation
        stats.currentHealth = 0f;

        float t = 0f;
        while (t < reviveDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / reviveDuration);
            stats.currentHealth = Mathf.Lerp(0f, targetHP, k);
            yield return null;
        }
        stats.currentHealth = targetHP;

        // 5) Restore everything
        if (invulnerableWhileReviving && !string.IsNullOrEmpty(invulnerableLayerName) && originalLayer >= 0)
        {
            SetLayerRecursively(gameObject, originalLayer);
        }

        if (lockControls)
        {
            TrySetEnabled(moveController, true);
            TrySetEnabled(inputController, true);
        }

        if (freezeSharks && sharkMgr != null && sharkMgr.sharks != null)
        {
            for (int i = 0; i < sharkMgr.sharks.Length; i++)
            {
                if (sharkMgr.sharks[i])
                    sharkMgr.sharks[i].enabled = true;
            }
        }

        reviving = false;
    }

    private void TrySetEnabled(MonoBehaviour mb, bool enabled)
    {
        if (mb != null) mb.enabled = enabled;
    }

    private void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        for (int i = 0; i < go.transform.childCount; i++)
            SetLayerRecursively(go.transform.GetChild(i).gameObject, layer);
    }
}
