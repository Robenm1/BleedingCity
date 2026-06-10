// Assets/Scripts/Cards/Dezzo/RewindRuntime.cs
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(PlayerHealth))]
public class RewindRuntime : MonoBehaviour
{
    [Header("Revive Config (set by SO)")]
    public float reviveDuration = 2.0f;
    [Range(0f, 1f)] public float healPercent = 0.5f;
    public bool freezeSharks = true;

    [Header("Invulnerability During Revive")]
    public bool invulnerableWhileReviving = true;
    public string invulnerableLayerName = "";

    // cached refs
    private PlayerStats stats;
    private PlayerHealth playerHealth;
    private DezzoSharkManager sharkMgr;

    // state
    private bool usedOnce = false;
    private bool reviving = false;
    private int originalLayer = -1;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
        playerHealth = GetComponent<PlayerHealth>();
        sharkMgr = GetComponent<DezzoSharkManager>();
    }

    private void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnDeath -= HandleDeath;
    }

    /// <summary>Subscribed to PlayerHealth.OnDeath — triggers the one-time revive sequence.</summary>
    private void HandleDeath()
    {
        if (usedOnce || reviving) return;
        StartCoroutine(ReviveSequence());
    }

    private IEnumerator ReviveSequence()
    {
        usedOnce = true;
        reviving = true;

        // Freeze sharks so they don't keep attacking while reviving
        SetSharksEnabled(false);

        // Optional: change layer to grant invulnerability
        if (invulnerableWhileReviving && !string.IsNullOrEmpty(invulnerableLayerName))
        {
            originalLayer = gameObject.layer;
            int invLayer = LayerMask.NameToLayer(invulnerableLayerName);
            if (invLayer >= 0) SetLayerRecursively(gameObject, invLayer);
        }

        // Lerp HP from 0 → targetHP over reviveDuration
        float targetHP = Mathf.Max(1f, stats.maxHealth * healPercent);

        float t = 0f;
        while (t < reviveDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / reviveDuration);
            playerHealth.SetHealth(Mathf.Lerp(0f, targetHP, k));
            yield return null;
        }

        playerHealth.SetHealth(targetHP);

        // Restore layer
        if (invulnerableWhileReviving && !string.IsNullOrEmpty(invulnerableLayerName) && originalLayer >= 0)
            SetLayerRecursively(gameObject, originalLayer);

        // Unfreeze sharks
        SetSharksEnabled(true);

        reviving = false;
    }

    private void SetSharksEnabled(bool enabled)
    {
        if (!freezeSharks || sharkMgr == null || sharkMgr.sharks == null) return;
        for (int i = 0; i < sharkMgr.sharks.Length; i++)
            if (sharkMgr.sharks[i] != null) sharkMgr.sharks[i].enabled = enabled;
    }

    private void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        for (int i = 0; i < go.transform.childCount; i++)
            SetLayerRecursively(go.transform.GetChild(i).gameObject, layer);
    }
}
