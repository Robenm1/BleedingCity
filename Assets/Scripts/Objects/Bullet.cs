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

    [Tooltip("Radius used for the hit check. Should roughly match the bullet collider radius.")]
    public float hitRadius = 0.1f;

    private Vector2 direction;
    private float lifeTimer;
    private Rigidbody2D rb;

    // We keep track of last position so we can sweep between frames
    private Vector2 lastPos;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // We want this to behave like a kinematic projectile
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        lifeTimer = lifetime;
    }

    private void OnEnable()
    {
        lastPos = rb.position;
    }

    private void Update()
    {
        // Kill after lifetime so bullets don't live forever
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void FixedUpdate()
    {
        // Calculate where we want to move this frame
        Vector2 currentPos = rb.position;
        Vector2 step = direction * speed * Time.fixedDeltaTime;
        Vector2 nextPos = currentPos + step;

        // BEFORE we move there, check if we would hit someone along that path
        // We'll use CircleCast so fast bullets don't skip
        Vector2 castDir = (nextPos - currentPos);
        float castDist = castDir.magnitude;

        if (castDist > 0f)
        {
            RaycastHit2D hit = Physics2D.CircleCast(
                currentPos,
                hitRadius,
                castDir.normalized,
                castDist,
                hitLayers
            );

            if (hit.collider != null)
            {
                // We hit something on the allowed layer(s)
                ApplyDamage(hit.collider.gameObject);
                Destroy(gameObject);
                return;
            }
        }

        // If we didn't hit anything, actually move the bullet
        rb.MovePosition(nextPos);

        // Store for next frame (not strictly required here, but good practice)
        lastPos = rb.position;
    }

    /// <summary>
    /// Call this once right after you spawn the bullet.
    /// dir should be normalized.
    /// </summary>
    public void Init(Vector2 dir, float dmgOverride = -1f, float speedOverride = -1f)
    {
        direction = dir.normalized;

        if (dmgOverride >= 0f)
            damage = dmgOverride;

        if (speedOverride >= 0f)
            speed = speedOverride;
    }

    private void ApplyDamage(GameObject obj)
    {
        EnemyHealth eh = obj.GetComponent<EnemyHealth>();
        if (eh != null)
        {
            eh.TakeDamage(damage);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Just so you can see the hit radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }
}
