using UnityEngine;

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

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (rotateSprite)
        {
            transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHit) return;

        if (((1 << collision.gameObject.layer) & enemyLayers) != 0)
        {
            var enemyHealth = collision.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
                hasHit = true;
                Destroy(gameObject);
            }
        }
    }
}
