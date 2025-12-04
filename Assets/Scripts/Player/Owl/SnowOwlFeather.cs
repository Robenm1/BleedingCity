using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SnowOwlFeather : MonoBehaviour
{
    [Header("Motion")]
    public float baseSpeed = 20f;
    public float stickDistance = 0.05f;
    public float maxLifetime = 15f;
    [Tooltip("How far the feather travels before sticking in the ground.")]
    public float maxTravelDistance = 15f;

    [Header("Damage")]
    [Tooltip("Damage multiplier when recalled (e.g., 1.5 = 150% of player's damage).")]
    public float recallDamageMultiplier = 1.5f;
    public LayerMask enemyLayers;

    [Header("Ground Detection")]
    public LayerMask groundLayers; // Assign terrain/ground layers in inspector

    // Runtime
    private int _ownerKey;
    private Transform _ownerTf;
    private PlayerStats _ownerStats; // NEW: Reference to owner's stats for damage calculation
    private Vector2 _vel;
    private Collider2D _col;
    private Rigidbody2D _rb;

    private bool _isRecalled = false;
    private bool _isStuck = false;
    private float _recallSpeedMul = 1f;
    private float _life;
    private float _distanceTraveled = 0f;
    private Vector3 _lastPosition;

    private HashSet<EnemyHealth> _hitEnemies = new HashSet<EnemyHealth>();

    private void OnEnable()
    {
        FeatherRegistry.Register(_ownerKey, this);
    }

    private void OnDisable()
    {
        FeatherRegistry.Unregister(_ownerKey, this);
    }

    public void Init(Transform ownerTf, int ownerKey, Vector2 direction, float speed, LayerMask enemies, PlayerStats ownerStats)
    {
        _ownerTf = ownerTf;
        _ownerKey = ownerKey;
        _ownerStats = ownerStats; // Store reference to owner's stats
        _vel = direction.normalized * Mathf.Max(0.01f, speed);
        enemyLayers = enemies;

        if (_col == null) _col = GetComponent<Collider2D>();
        if (_rb == null) _rb = GetComponent<Rigidbody2D>();

        _col.isTrigger = true;

        _lastPosition = transform.position;

        // Register after ownerKey is set
        FeatherRegistry.Register(_ownerKey, this);
    }

    private void Awake()
    {
        _col = GetComponent<Collider2D>();
        _rb = GetComponent<Rigidbody2D>();
        if (_col) _col.isTrigger = true;
        if (_rb) _rb.bodyType = RigidbodyType2D.Kinematic;
        _lastPosition = transform.position;
    }

    private void Update()
    {
        _life += Time.deltaTime;
        if (_life > maxLifetime) { Destroy(gameObject); return; }

        // If stuck in ground, don't move until recalled
        if (_isStuck && !_isRecalled) return;

        if (_isRecalled)
        {
            // Home to owner
            if (!_ownerTf) { Destroy(gameObject); return; }

            Vector2 toOwner = (Vector2)_ownerTf.position - (Vector2)transform.position;
            float step = (baseSpeed * _recallSpeedMul) * Time.deltaTime;

            if (toOwner.magnitude <= stickDistance)
            {
                Destroy(gameObject);
                return;
            }

            Vector2 dir = toOwner.normalized;
            transform.position += (Vector3)(dir * step);
            transform.right = dir;

            _isStuck = false; // Unstick when recalled
        }
        else
        {
            // Normal flight forward
            Vector2 step = _vel * Time.deltaTime;
            transform.position += (Vector3)step;

            // Track distance traveled
            float movedThisFrame = ((Vector3)step).magnitude;
            _distanceTraveled += movedThisFrame;

            // Stick after traveling max distance (like Xayah feathers)
            if (_distanceTraveled >= maxTravelDistance)
            {
                StickInGround();
            }

            if (_vel.sqrMagnitude > 0.0001f)
                transform.right = _vel.normalized;

            _lastPosition = transform.position;
        }
    }

    public void BeginRecall(Transform targetOwner, float speedMul)
    {
        _ownerTf = targetOwner;
        _isRecalled = true;
        _isStuck = false; // Unstick when recalled
        _recallSpeedMul = Mathf.Max(0.05f, speedMul);
        if (_col) _col.isTrigger = true;

        // Clear hit enemies so we can damage them again on recall
        _hitEnemies.Clear();
    }

    private void StickInGround()
    {
        if (_isStuck) return;
        _isStuck = true;
        _vel = Vector2.zero;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Deal damage to enemies
        if (((1 << other.gameObject.layer) & enemyLayers) != 0)
        {
            var hp = other.GetComponent<EnemyHealth>();
            if (hp != null && !_hitEnemies.Contains(hp))
            {
                // Get damage from owner's stats (supports buffs/upgrades)
                if (_ownerStats != null)
                {
                    float baseDamage = _ownerStats.GetDamage();
                    // Use recall multiplier if recalled, normal damage otherwise
                    float damageToApply = _isRecalled ? (baseDamage * recallDamageMultiplier) : baseDamage;

                    if (damageToApply > 0f)
                    {
                        hp.TakeDamage(damageToApply);
                        _hitEnemies.Add(hp);
                    }
                }
            }
            // Don't stick on enemy hit - keep flying
            return;
        }

        // If we hit ground/terrain directly, stick immediately
        if (!_isStuck && !_isRecalled && ((1 << other.gameObject.layer) & groundLayers) != 0)
        {
            StickInGround();
            return;
        }
    }

    // Optional: Use OnCollisionEnter2D if your ground uses non-trigger colliders
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if we hit ground - stick the feather
        if (!_isStuck && !_isRecalled && ((1 << collision.gameObject.layer) & groundLayers) != 0)
        {
            StickInGround();
        }
    }
}