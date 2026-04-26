using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Debug-only spawner. Attach to Pyro and disable when done testing.
/// Press 1 or click the on-screen button to spawn the Fire Dragon immediately.
/// </summary>
public class FireDragonDebugSpawner : MonoBehaviour
{
    [Header("References")]
    public GameObject fireDragonPrefab;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            SpawnDragon();
        }
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(10f, 10f, 180f, 40f), "Spawn Fire Dragon"))
        {
            SpawnDragon();
        }
    }

    /// <summary>
    /// Instantiates the Fire Dragon prefab. Flight path is handled by FireDragon itself.
    /// </summary>
    private void SpawnDragon()
    {
        if (fireDragonPrefab == null)
        {
            Debug.LogWarning("[FireDragonDebug] No fireDragonPrefab assigned.");
            return;
        }

        Instantiate(fireDragonPrefab, Vector3.zero, Quaternion.identity);
        Debug.Log("[FireDragonDebug] Fire Dragon spawned for testing.");
    }
}
