using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FireEagleFireball : MonoBehaviour
{
    [HideInInspector]
    public float damage = 15f;

    [HideInInspector]
    public LayerMask enemyLayers;

    [Header("Lifetime")]
    public float lifetime = 5f;

    [Header("Visual")]
    public bool rotateSprite = true;
    public float rotationSpeed = 360f;

    private bool hasHit = false;
    private Rigidbody2D rb;
    private Vector2 direction;
    private float speed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    /// <summary>
    /// Called by FireEagle after spawning. Sets travel direction and speed.
    /// </summary>
    public void Launch(Vector2 dir, float projectileSpeed)
    {
        direction = dir.normalized;
        speed = projectileSpeed;
    }

    private void Update()
    {
        if (rotateSprite)
        {
            transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
        }
    }

    private void FixedUpdate()
    {
        if (hasHit) return;

        Vector2 currentPos = rb.position;
        Vector2 step = direction * speed * Time.fixedDeltaTime;
        Vector2 nextPos = currentPos + step;

        if (step.sqrMagnitude > 0f)
        {
            RaycastHit2D hit = Physics2D.CircleCast(currentPos, 0.1f, step.normalized, step.magnitude, enemyLayers);
            if (hit.collider != null)
            {
                ApplyDamage(hit.collider.gameObject);
                hasHit = true;
                Destroy(gameObject);
                return;
            }
        }

        rb.MovePosition(nextPos);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHit) return;
        if (((1 << collision.gameObject.layer) & enemyLayers) == 0) return;

        var enemyHealth = collision.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damage);
            hasHit = true;
            Destroy(gameObject);
        }
    }

    private void ApplyDamage(GameObject obj)
    {
        var enemyHealth = obj.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damage);
        }
    }
}
