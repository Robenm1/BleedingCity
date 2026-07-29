using UnityEngine;

public class CameraFollowTarget : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Follow")]
    public Vector3 offset = new Vector3(0f, 0f, -10f);
    public float smoothTime = 0.12f;

    [Header("Snap On New Target")]
    public bool snapWhenTargetAssigned = true;

    private Vector3 _velocity;

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desiredPosition = target.position + offset;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref _velocity,
            smoothTime
        );
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        _velocity = Vector3.zero;

        if (target != null && snapWhenTargetAssigned)
            transform.position = target.position + offset;
    }
}