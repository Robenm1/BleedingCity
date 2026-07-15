using UnityEngine;

[DisallowMultipleComponent]
public class BulletFromThePastProjectile : MonoBehaviour
{
    [Header("Runtime")]
    public PlayerStats ownerStats;
    public Vector2 direction = Vector2.right;

    [Header("Movement")]
    public float speed = 14f;
    public float lifetime = 3f;

    [Header("Impact")]
    public LayerMask enemyLayers;
    public float hitRadius = 0.15f;

    [Header("Explosion")]
    public float explosionRadius = 3f;

    [Tooltip("1 = 100% of player attack damage.")]
    public float damageScaling = 1f;

    [Header("Slow")]
    public float slowDuration = 2f;

    [Tooltip("0.5 = enemy moves at 50% speed.")]
    public float slowMultiplier = 0.5f;

    [Header("Visuals")]
    [Tooltip("Animator on the same bullet prefab object.")]
    public Animator explosionAnimator;

    [Tooltip("Exact animation state name for the explosion.")]
    public string explosionStateName = "Explosion";

    [Tooltip("How long before the projectile is destroyed after exploding.")]
    public float explosionDuration = 0.45f;

    [Tooltip("Trail Renderer on the same bullet prefab.")]
    public TrailRenderer trail;

    [Tooltip("Collider on the bullet prefab.")]
    public Collider2D projectileCollider;

    [Tooltip("Original diameter of the explosion animation in Unity world units before scaling. Start with 1.")]
    public float explosionVisualBaseDiameter = 1f;

    [Tooltip("Extra correction if the explosion visual has transparent padding.")]
    public float explosionVisualScaleMultiplier = 1f;

    [Header("Debug")]
    public bool showDebug = true;

    private float _timer;
    private bool _exploded;
    private SpriteRenderer _spriteRenderer;
    private Vector3 _originalScale;

    private void Awake()
    {
        if (trail == null)
            trail = GetComponent<TrailRenderer>();

        if (projectileCollider == null)
            projectileCollider = GetComponent<Collider2D>();

        if (explosionAnimator == null)
            explosionAnimator = GetComponent<Animator>();

        _spriteRenderer = GetComponent<SpriteRenderer>();
        _originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        _timer = 0f;
        _exploded = false;

        transform.localScale = _originalScale;

        if (direction.sqrMagnitude <= 0.01f)
            direction = Vector2.right;

        direction.Normalize();

        if (trail != null)
        {
            trail.Clear();
            trail.emitting = true;
        }

        if (projectileCollider != null)
            projectileCollider.enabled = true;

        if (_spriteRenderer != null)
            _spriteRenderer.enabled = true;

        // Prevent explosion animation from playing on spawn.
        if (explosionAnimator != null)
            explosionAnimator.enabled = false;
    }

    private void Update()
    {
        if (_exploded) return;

        _timer += Time.deltaTime;

        if (_timer >= Mathf.Max(0.1f, lifetime))
        {
            Explode();
            return;
        }

        transform.position += (Vector3)(direction * Mathf.Max(0.1f, speed) * Time.deltaTime);

        CheckImpact();
    }

    private void CheckImpact()
    {
        Collider2D hit = Physics2D.OverlapCircle(
            transform.position,
            Mathf.Max(0.01f, hitRadius),
            enemyLayers
        );

        if (hit == null) return;

        var enemy = hit.GetComponent<EnemyHealth>();
        if (enemy == null) enemy = hit.GetComponentInParent<EnemyHealth>();
        if (enemy == null) return;

        Explode();
    }

    private void Explode()
    {
        if (_exploded) return;
        _exploded = true;

        if (trail != null)
            trail.emitting = false;

        if (projectileCollider != null)
            projectileCollider.enabled = false;

        DealExplosionDamage();

        ScaleExplosionVisualToRadius();

        if (explosionAnimator != null)
        {
            explosionAnimator.enabled = true;
            explosionAnimator.Play(explosionStateName, 0, 0f);
            explosionAnimator.Update(0f);
        }

        if (showDebug)
            Debug.Log("[BulletFromThePastProjectile] Bullet exploded.");

        Destroy(gameObject, Mathf.Max(0.05f, explosionDuration));
    }

    private void DealExplosionDamage()
    {
        float ownerDamage = ownerStats != null ? ownerStats.GetDamage() : 10f;
        float damage = ownerDamage * Mathf.Max(0f, damageScaling);

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            Mathf.Max(0.1f, explosionRadius),
            enemyLayers
        );

        BulletFromThePastEffect.BeginBulletDamage();

        try
        {
            foreach (var hit in hits)
            {
                if (hit == null) continue;

                var enemy = hit.GetComponent<EnemyHealth>();
                if (enemy == null) enemy = hit.GetComponentInParent<EnemyHealth>();
                if (enemy == null) continue;

                // Do not damage Abyssal Doll with this explosion.
                if (enemy.GetComponent<AbyssalDollObject>() != null) continue;
                if (enemy.GetComponentInParent<AbyssalDollObject>() != null) continue;

                enemy.TakeDamage(damage);

                var slow = enemy.GetComponent<DemonicSlowEffect>();
                if (!slow) slow = enemy.gameObject.AddComponent<DemonicSlowEffect>();

                slow.ApplySlow(slowMultiplier, slowDuration);
            }
        }
        finally
        {
            BulletFromThePastEffect.EndBulletDamage();
        }

        if (showDebug)
            Debug.Log($"[BulletFromThePastProjectile] Explosion damage: {damage:F1}");
    }

    private void ScaleExplosionVisualToRadius()
    {
        float targetDiameter = Mathf.Max(0.1f, explosionRadius) * 2f;
        float baseDiameter = Mathf.Max(0.01f, explosionVisualBaseDiameter);

        float scale = targetDiameter / baseDiameter;
        scale *= Mathf.Max(0.01f, explosionVisualScaleMultiplier);

        transform.localScale = _originalScale * scale;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.1f, explosionRadius));

        Gizmos.color = new Color(1f, 1f, 0.2f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.01f, hitRadius));
    }
#endif
}