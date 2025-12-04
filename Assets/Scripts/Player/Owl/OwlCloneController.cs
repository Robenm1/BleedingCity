// Assets/Scripts/Player/SnowOwl/OwlCloneController.cs
using UnityEngine;

public class OwlCloneController : MonoBehaviour
{
    public Transform owner;
    public float lifetime = 8f;
    public float followDistance = 1.8f;
    public float followSmooth = 6f;

    private float _t;

    private void Update()
    {
        _t += Time.deltaTime;
        if (_t >= lifetime) { Destroy(gameObject); return; }

        if (owner)
        {
            Vector3 target = owner.position + (Vector3)(Vector2.right * followDistance);
            transform.position = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-followSmooth * Time.deltaTime));
        }
    }
}
