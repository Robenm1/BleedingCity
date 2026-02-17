using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Bullet Stats")]
    [Tooltip("How fast the bullet moves (units/sec).")]
    public float speed = 15f;

    [Tooltip("How much damage this bullet deals on hit.")]
    public float damage = 10f;

    [Tooltip("How long this bullet lives before auto-destroy (seconds).")]
    public float lifetime = 3f;

    [Header("Collision")]
    [Tooltip("Which layers count as valid targets (e.g. Enemy layer).")]
    public LayerMask hitLayers;

    [Tooltip("Radius used for the hit check.")]
    public float hitRadius = 0.1f;

    // Direction on the XZ plane
    private Vector3 direction;
    private float lifeTimer;

    private void Awake()
    {
        lifeTimer = lifetime;
    }

    private void Update()
    {
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
            Destroy(gameObject);
    }

    private void FixedUpdate()
    {
        Vector3 currentPos = transform.position;
        Vector3 step       = direction * speed * Time.fixedDeltaTime;
        Vector3 nextPos    = currentPos + step;

        float castDist = step.magnitude;
        if (castDist > 0f)
        {
            // SphereCast on the XZ plane to catch enemies
            if (Physics.SphereCast(currentPos, hitRadius, direction, out RaycastHit hit, castDist, hitLayers))
            {
                ApplyDamage(hit.collider.gameObject);
                Destroy(gameObject);
                return;
            }
        }

        transform.position = nextPos;
    }

    /// <summary>Call once after spawning. dir should point on the XZ plane.</summary>
    public void Init(Vector2 dir, float dmgOverride = -1f, float speedOverride = -1f)
    {
        // Map 2D direction to XZ world space
        direction = new Vector3(dir.x, 0f, dir.y).normalized;

        if (dmgOverride >= 0f)  damage = dmgOverride;
        if (speedOverride >= 0f) speed = speedOverride;
    }

    private void ApplyDamage(GameObject obj)
    {
        EnemyHealth eh = obj.GetComponent<EnemyHealth>();
        if (eh != null)
            eh.TakeDamage(damage);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }
}
