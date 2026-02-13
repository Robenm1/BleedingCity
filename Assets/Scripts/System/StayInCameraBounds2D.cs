using UnityEngine;

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

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void LateUpdate()
    {
        var cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null) return;

        Vector3 pos = rb ? rb.position : (Vector2)transform.position;
        Vector3 v = cam.WorldToViewportPoint(pos);

        float zDist = Mathf.Abs(cam.transform.position.z - transform.position.z);
        v.z = zDist;

        float pad = Mathf.Clamp01(viewportPadding);
        Vector3 clampedV = new Vector3(
            Mathf.Clamp(v.x, pad, 1f - pad),
            Mathf.Clamp(v.y, pad, 1f - pad),
            v.z
        );

        bool isOutOfBounds = v.x < pad || v.x > 1f - pad || v.y < pad || v.y > 1f - pad;

        if (!isOutOfBounds) return;

        Vector3 targetWorld = cam.ViewportToWorldPoint(clampedV);

        if (softClamp)
        {
            float t = 1f - Mathf.Exp(-softStrength * Time.deltaTime);
            Vector2 newPos = Vector2.Lerp(pos, targetWorld, t);

            if (rb)
            {
                rb.MovePosition(newPos);

                Vector2 dirToCenter = ((Vector2)targetWorld - (Vector2)pos).normalized;
                Vector2 currentVel = rb.linearVelocity;

                if (Vector2.Dot(currentVel, dirToCenter) < 0f)
                {
                    rb.linearVelocity = Vector2.zero;
                }
            }
            else
            {
                transform.position = newPos;
            }
        }
        else
        {
            if (rb)
            {
                rb.MovePosition(targetWorld);
                rb.linearVelocity = Vector2.zero;
            }
            else
            {
                transform.position = targetWorld;
            }
        }
    }
}
