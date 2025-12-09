using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime enabler for the "Frost Feather" card:
/// - Adds/updates a FeatherChainOnHit on the feather prefab used by any OwlFeatherShooter
///   on this player (and children like the clone).
/// - Keeps re-syncing in case a clone spawns later.
/// </summary>
[DisallowMultipleComponent]
public class FrostFeatherEffect : MonoBehaviour
{
    [Header("Chain Settings (from SO)")]
    [Tooltip("Maximum number of bounces after the first impact (3 = hits 4 enemies total).")]
    public int maxBounces = 3;

    [Tooltip("Damage multiplier applied to each chain hit relative to PlayerStats.GetDamage(). 1 = base damage.")]
    public float chainDamageMultiplier = 1.0f;

    [Header("Maintenance")]
    [Tooltip("How often we rescan for OwlFeatherShooter components to decorate their feather prefab.")]
    public float resyncInterval = 0.5f;

    private Coroutine _runner;

    /// <summary>Call from SO.Apply().</summary>
    public void EnableEffect()
    {
        if (_runner != null) StopCoroutine(_runner);
        _runner = StartCoroutine(ResyncRoutine());
    }

    /// <summary>Optional if you later add SO.Remove().</summary>
    public void DisableEffect()
    {
        if (_runner != null)
        {
            StopCoroutine(_runner);
            _runner = null;
        }
        // We don't rip components off the prefab at runtime to avoid breaking active instances.
    }

    private IEnumerator ResyncRoutine()
    {
        var shooters = new List<OwlFeatherShooter>();
        while (true)
        {
            shooters.Clear();
            GetComponentsInChildren(true, shooters); // player + clone(s)
            for (int i = 0; i < shooters.Count; i++)
            {
                TryDecorateShooterFeather(shooters[i]);
            }
            yield return new WaitForSeconds(resyncInterval);
        }
    }

    private void TryDecorateShooterFeather(OwlFeatherShooter shooter)
    {
        if (shooter == null) return;

        // NOTE: In your project, featherPrefab is a SnowOwlFeather (component), not a GameObject.
        var featherComp = shooter.featherPrefab; // SnowOwlFeather
        if (!featherComp) return;

        GameObject prefabGO = featherComp.gameObject;

        // Ensure the chain component exists on the prefab itself.
        var chain = prefabGO.GetComponent<FeatherChainOnHit>();
        if (!chain) chain = prefabGO.AddComponent<FeatherChainOnHit>();

        // Configure chain behavior with new property names
        chain.maxBounces = Mathf.Max(0, maxBounces);
        chain.damageMultiplier = Mathf.Max(0f, chainDamageMultiplier);

        // Ensure owner link exists so spawned feathers inherit the shooter/owner correctly.
        var ownerLink = prefabGO.GetComponent<FeatherOwnerLink>();
        if (!ownerLink) ownerLink = prefabGO.AddComponent<FeatherOwnerLink>();
        // No need to set anything here; your shooter should stamp owner on spawn.
    }
}