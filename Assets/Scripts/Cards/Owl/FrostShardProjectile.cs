using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FrostShardProjectile : MonoBehaviour
{
    [Header("Motion")]
    public float speed = 18f;
    public float lifetime = 3f;

    [Header("Damage")]
    public float damage = 12f;
    public LayerMask enemyLayers;

    [Header("Frost (on hit)")]
    public bool frostOnHit = true;
    [Range(0.05f, 1f)] public float frostSlowFactor = 0.5f;
    public float frostDuration = 3f;
    public Sprite frostIcon;
    public Vector2 iconPivot = new(0f, 0.85f);
    public Vector2 iconSize  = new(0.35f, 0.35f);

    [Header("Propagation & Meta")]
    [Range(0f, 1f)] public float propagateFrostOnKillChance = 0f;
    public float frostVulnerabilityMul = 1f;

    private float _t;

    /// <summary>Launches the projectile in a XZ-plane direction expressed as a Vector2.</summary>
    public void Launch(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        dir.Normalize();
        // Point forward on the XZ plane
        Vector3 dir3D = new Vector3(dir.x, 0f, dir.y);
        transform.rotation = Quaternion.LookRotation(dir3D, Vector3.up);
    }

    private void Awake()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    private void Update()
    {
        transform.position += transform.forward * (speed * Time.deltaTime);

        _t += Time.deltaTime;
        if (_t >= lifetime) Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & enemyLayers.value) == 0) return;

        var eh = other.GetComponent<EnemyHealth>();
        if (eh == null) return;

        eh.TakeDamage(damage);

        if (frostOnHit)
        {
            var frosted = other.GetComponent<FrostedOnEnemy>();
            if (!frosted) frosted = other.gameObject.AddComponent<FrostedOnEnemy>();

            frosted.Apply(
                Mathf.Clamp(frostSlowFactor, 0.05f, 1f),
                Mathf.Max(0f, frostDuration),
                frostIcon,
                iconPivot,
                iconSize
            );
        }

        Destroy(gameObject);
    }
}
