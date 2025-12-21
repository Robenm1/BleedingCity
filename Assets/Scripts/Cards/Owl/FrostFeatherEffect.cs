using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime enabler for the "Frost Feather" card:
/// - Intercepts feather spawns from OwlFeatherShooter and adds FeatherChainOnHit to instances
/// - Does NOT modify the prefab to avoid permanent changes
/// </summary>
[DisallowMultipleComponent]
public class FrostFeatherEffect : MonoBehaviour
{
    [Header("Chain Settings (from SO)")]
    [Tooltip("Maximum number of bounces after the first impact (3 = hits 4 enemies total).")]
    public int maxBounces = 3;

    [Tooltip("Damage multiplier applied to each chain hit relative to PlayerStats.GetDamage(). 1 = base damage.")]
    public float chainDamageMultiplier = 1.0f;

    private bool _isActive = false;
    private readonly List<OwlFeatherShooter> _trackedShooters = new List<OwlFeatherShooter>();

    /// <summary>Call from SO.Apply().</summary>
    public void EnableEffect()
    {
        _isActive = true;
        StartCoroutine(TrackShootersRoutine());
    }

    /// <summary>Optional if you later add SO.Remove().</summary>
    public void DisableEffect()
    {
        _isActive = false;
        UnsubscribeFromAllShooters();
    }

    private void OnDestroy()
    {
        UnsubscribeFromAllShooters();
    }

    private IEnumerator TrackShootersRoutine()
    {
        var shooters = new List<OwlFeatherShooter>();
        while (_isActive)
        {
            shooters.Clear();
            GetComponentsInChildren(true, shooters); // player + clone(s)

            // Subscribe to any new shooters
            foreach (var shooter in shooters)
            {
                if (shooter != null && !_trackedShooters.Contains(shooter))
                {
                    SubscribeToShooter(shooter);
                }
            }

            // Clean up null references
            _trackedShooters.RemoveAll(s => s == null);

            yield return new WaitForSeconds(0.5f);
        }
    }

    private void SubscribeToShooter(OwlFeatherShooter shooter)
    {
        _trackedShooters.Add(shooter);
        // Hook into the shooter's spawn event if it has one
        // If your OwlFeatherShooter doesn't have events, we'll need another approach
        // For now, we'll use a different method - see below
    }

    private void UnsubscribeFromAllShooters()
    {
        _trackedShooters.Clear();
    }

    private void Update()
    {
        if (!_isActive) return;

        // Find all feathers in the scene that don't have the chain component yet
        var allFeathers = FindObjectsOfType<SnowOwlFeather>();
        foreach (var feather in allFeathers)
        {
            if (feather == null) continue;

            // Only add to instances, not prefabs
            if (feather.gameObject.scene.name == null) continue;

            var chain = feather.GetComponent<FeatherChainOnHit>();
            if (chain == null)
            {
                // This is a newly spawned feather - add the chain component
                chain = feather.gameObject.AddComponent<FeatherChainOnHit>();
                chain.maxBounces = Mathf.Max(0, maxBounces);
                chain.damageMultiplier = Mathf.Max(0f, chainDamageMultiplier);

                // Ensure owner link exists
                var ownerLink = feather.GetComponent<FeatherOwnerLink>();
                if (!ownerLink) ownerLink = feather.gameObject.AddComponent<FeatherOwnerLink>();
            }
        }
    }
}
