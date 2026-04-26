using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// The Fire Dragon shadow that appears when the Golem dies.
/// Flies from the bottom-right corner of the camera to the top-left corner,
/// dealing fire damage to all enemies it passes through, then destroys itself.
/// </summary>
public class FireDragon : MonoBehaviour
{
    [Header("Flight")]
    [Tooltip("Extra world-units beyond the camera edge to start/end the flight path.")]
    public float cameraEdgePadding = 5f;

    [Tooltip("Total time in seconds for the dragon to cross the scene.")]
    public float flightDuration = 3.5f;

    [Tooltip("Multiplies flight speed. 1 = default, 2 = twice as fast, 0.5 = half speed.")]
    public float flightSpeedMultiplier = 1f;

    [Header("Area Burn on Entry")]
    [Tooltip("If true, instantly deals a burst of damage to every enemy on screen when the dragon appears.")]
    public bool dealBurstDamageOnEntry = true;

    [Tooltip("Burst damage dealt once to every on-screen enemy when the dragon enters.")]
    public float burstDamage = 150f;

    [Tooltip("Enemy layers to damage.")]
    public LayerMask enemyLayers;

    [Header("Debug")]
    public bool showDebug = true;

    private Vector3 startPosition;
    private Vector3 endPosition;

    private void Start()
    {
        SetupFlightPath();

        if (dealBurstDamageOnEntry)
        {
            BurnAllOnScreenEnemies();
        }

        StartCoroutine(FlyAcrossScene());
    }

    /// <summary>
    /// Calculates start (bottom-right) and end (top-left) world positions
    /// based on the current camera frustum.
    /// </summary>
    private void SetupFlightPath()
    {
        Camera cam = Camera.main;

        if (cam == null)
        {
            // Fallback: fixed world coordinates if no camera is found.
            startPosition = new Vector3(20f, -15f, 0f);
            endPosition = new Vector3(-20f, 15f, 0f);
            transform.position = startPosition;
            return;
        }

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        Vector3 camPos = cam.transform.position;

        startPosition = new Vector3(
            camPos.x + halfWidth + cameraEdgePadding,
            camPos.y - halfHeight - cameraEdgePadding,
            0f
        );

        endPosition = new Vector3(
            camPos.x - halfWidth - cameraEdgePadding,
            camPos.y + halfHeight + cameraEdgePadding,
            0f
        );

        transform.position = startPosition;

        // Rotate to face the direction of travel, head-first.
        // Atan2 gives the angle for the +X axis; the sprite's head points up (+Y),
        // so subtract 90° to align +Y with the flight direction.
        Vector2 direction = (endPosition - startPosition).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        if (showDebug)
        {
            Debug.Log($"[FireDragon] Flight path: {startPosition} → {endPosition} over {flightDuration}s");
        }
    }

    /// <summary>
    /// Instantly deals burst damage to every enemy currently visible on screen.
    /// </summary>
    private void BurnAllOnScreenEnemies()
    {
        Camera cam = Camera.main;
        float scanRadius = cam != null
            ? Mathf.Sqrt(Mathf.Pow(cam.orthographicSize * cam.aspect, 2f) + Mathf.Pow(cam.orthographicSize, 2f)) + 2f
            : 30f;

        Vector3 origin = cam != null ? cam.transform.position : transform.position;
        origin.z = 0f;

        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, scanRadius, enemyLayers);

        var damaged = new HashSet<EnemyHealth>();
        foreach (var hit in hits)
        {
            if (!hit) continue;
            var health = hit.GetComponent<EnemyHealth>();
            if (health != null && !damaged.Contains(health))
            {
                health.TakeSummonDamage(burstDamage);
                damaged.Add(health);
            }
        }

        if (showDebug)
        {
            Debug.Log($"[FireDragon] Entry burst hit {damaged.Count} enemies for {burstDamage} damage each.");
        }
    }

    private IEnumerator FlyAcrossScene()
    {
        float elapsed = 0f;
        float effectiveDuration = flightDuration / Mathf.Max(flightSpeedMultiplier, 0.01f);

        while (elapsed < effectiveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / effectiveDuration);
            transform.position = Vector3.Lerp(startPosition, endPosition, t);
            yield return null;
        }

        transform.position = endPosition;

        if (showDebug) Debug.Log("[FireDragon] Dragon has left the scene.");
        Destroy(gameObject);
    }
}
