using UnityEngine;

/// Attach to the Shark prefab. Keeps it inside the visible camera with a small padding.
/// Works with moving cameras and any orthographic size.
public class StayInCameraBounds2D : MonoBehaviour
{
    [Tooltip("Camera to clamp against. If empty, uses Camera.main.")]
    public Camera targetCamera;

    [Range(0f, 0.2f)]
    [Tooltip("Padding as a fraction of screen (0.05 = 5% from edges).")]
    public float viewportPadding = 0.05f;

    [Tooltip("Use a smooth, non-teleport clamp.")]
    public bool softClamp = true;

    [Tooltip("Higher = stronger pull back to screen when outside (soft mode only).")]
    public float softStrength = 15f;

    void LateUpdate()
    {
        var cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null) return;

        // Convert position to viewport space (0..1)
        Vector3 pos = transform.position;
        Vector3 v = cam.WorldToViewportPoint(pos);

        // Distance from camera for proper ViewportToWorldPoint conversion
        // (In 2D, camera.z ~ -10 and objects at z=0 => zDist ~ 10)
        float zDist = Mathf.Abs(cam.transform.position.z - pos.z);
        v.z = zDist;

        // Clamp with padding
        float pad = Mathf.Clamp01(viewportPadding);
        Vector3 clampedV = new Vector3(
            Mathf.Clamp(v.x, pad, 1f - pad),
            Mathf.Clamp(v.y, pad, 1f - pad),
            v.z
        );

        if (softClamp)
        {
            // If inside already, do nothing
            if (Mathf.Approximately(v.x, clampedV.x) && Mathf.Approximately(v.y, clampedV.y)) return;

            Vector3 target = cam.ViewportToWorldPoint(clampedV);
            // Smooth pull back toward the clamped position
            float t = 1f - Mathf.Exp(-softStrength * Time.deltaTime);
            transform.position = Vector3.Lerp(pos, target, t);
        }
        else
        {
            // Hard clamp (teleport to edge)
            if (v.x < pad || v.x > 1f - pad || v.y < pad || v.y > 1f - pad)
            {
                transform.position = cam.ViewportToWorldPoint(clampedV);
            }
        }
    }
}
