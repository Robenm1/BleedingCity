using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerMovement))]
public class OutOfTheOrdinaryEffect : MonoBehaviour
{
    [Header("Out of the Ordinary")]
    [Tooltip("How long after the first dash the player can dash again before cooldown begins.")]
    public float extraDashWindowDuration = 0.8f;

    [Header("Debug")]
    public bool showDebug = true;

    private PlayerMovement _movement;
    private bool _registered;

    private void Awake()
    {
        _movement = GetComponent<PlayerMovement>();
    }

    private void OnEnable()
    {
        if (_registered) return;

        if (!_movement)
            _movement = GetComponent<PlayerMovement>();

        if (!_movement)
        {
            Debug.LogWarning("[OutOfTheOrdinaryEffect] PlayerMovement was not found on player.");
            return;
        }

        _movement.EnableExtraDashWindow(Mathf.Max(0.05f, extraDashWindowDuration));

        _registered = true;

        if (showDebug)
            Debug.Log($"[OutOfTheOrdinaryEffect] Enabled. Window: {extraDashWindowDuration:F2}s");
    }

    private void OnDisable()
    {
        if (!_registered || !_movement) return;

        _movement.DisableExtraDashWindow();

        _registered = false;

        if (showDebug)
            Debug.Log("[OutOfTheOrdinaryEffect] Disabled.");
    }
}