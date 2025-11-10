using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyFollow : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The player to chase. If empty, we'll search for tag 'Player'.")]
    public Transform playerTarget;

    private Rigidbody2D playerRB;   // to read player's velocity
    private PlayerHealth playerHealth;

    [Header("Chase Settings")]
    public float moveSpeed = 3f;
    [Tooltip("Stop moving if within this distance (prevents face-hugging).")]
    public float stopDistance = 1.2f;
    [Range(0f, 1f)] public float steeringSmooth = 0.15f;

    [Header("Anti-Block / Sidestep")]
    public bool avoidBlockingPlayer = true;
    public float sidestepTriggerDistance = 1.5f;
    public float playerApproachSpeedThreshold = 1f;
    [Range(0f, 1f)] public float approachDotThreshold = 0.7f;
    public float sidestepDuration = 0.3f;
    public float sidestepSpeedMultiplier = 1.2f;
    public float sidestepMaxSpeed = 5f;
    [Range(0f, 1f)] public float sidestepSmooth = 0.25f;

    // ================= CONTACT DAMAGE (collision-based) =================
    [Header("Contact Damage (Collision)")]
    [Tooltip("Raw damage per tick while the player is colliding with this enemy.")]
    public float contactDamage = 10f;

    [Tooltip("Time between damage ticks from THIS enemy (seconds).")]
    public float tickInterval = 0.5f;

    [Tooltip("Require the player to be 'stuck' (low relative speed) to take collision damage.")]
    public bool requireLowRelativeSpeed = true;

    [Tooltip("Relative speed threshold under which we consider them 'stuck'.")]
    public float relativeSpeedThreshold = 0.5f;

    [Tooltip("Require collision to persist at least this long before the first tick.")]
    public float minimumContactTime = 0.1f;

    // ================= STOP-AREA DAMAGE (trigger-based) =================
    [Header("Stop Area Damage (Trigger)")]
    [Tooltip("If true, the player takes damage when this enemy touches the player's 'stop area' trigger.")]
    public bool damageOnStopArea = true;

    [Tooltip("Use tag match for the stop area. If false, use the layer mask.")]
    public bool useTagForStopArea = true;

    [Tooltip("Tag to identify the player's stop-area trigger.")]
    public string stopAreaTag = "PlayerStopArea";

    [Tooltip("Layer(s) for the player's stop-area trigger if not using tag.")]
    public LayerMask stopAreaLayers;

    // timers
    private float _nextDamageTime = 0f;
    private float _contactTimer = 0f;

    // sidestep
    private bool isSidestepping = false;
    private float sidestepTimer = 0f;
    private Vector2 sidestepDirection;
    private Vector2 sidestepCurrentVelocity;
    private Vector2 sidestepVelSmoothRef;
    private Vector2 sidestepTargetVelocity;

    private Rigidbody2D rb;
    private Vector2 chaseCurrentVelocity;
    private Vector2 chaseVelSmoothRef;

    // reacquire timing if player missing
    private float _retryFindTimer = 0f;

    private void OnEnable()
    {
        // Subscribe to player spawn/changes (works with anchor spawn)
        PlayerLocator.OnPlayerChanged += HandlePlayerChanged;
    }

    private void OnDisable()
    {
        PlayerLocator.OnPlayerChanged -= HandlePlayerChanged;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // IMPORTANT: We no longer change bodyType / gravity / constraints here.
        // Set those in the Inspector as you prefer.
    }

    private void Start()
    {
        // Prefer the global locator (works even if player spawned after enemies)
        if (playerTarget == null && PlayerLocator.Current != null)
            BindTarget(PlayerLocator.Current);

        // Fallback: find by tag once
        if (playerTarget == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) BindTarget(p.transform);
        }
    }

    private void Update()
    {
        // If target lost (e.g., respawned), try to reacquire periodically
        if (playerTarget == null)
        {
            _retryFindTimer -= Time.deltaTime;
            if (_retryFindTimer <= 0f)
            {
                _retryFindTimer = 0.5f;
                if (PlayerLocator.Current != null) BindTarget(PlayerLocator.Current);
                else
                {
                    var p = GameObject.FindGameObjectWithTag("Player");
                    if (p != null) BindTarget(p.transform);
                }
            }
        }
    }

    private void FixedUpdate()
    {
        if (playerTarget == null) return;

        Vector2 toPlayer = (Vector2)(playerTarget.position - transform.position);
        float dist = toPlayer.magnitude;

        if (isSidestepping)
        {
            SidestepMove();
            return;
        }

        if (avoidBlockingPlayer && playerRB != null && ShouldStartSidestep(toPlayer, dist))
        {
            BeginSidestep(toPlayer);
            SidestepMove();
            return;
        }

        NormalChaseMove(toPlayer, dist);
    }

    // ================= NORMAL CHASE =================
    private void NormalChaseMove(Vector2 toPlayer, float dist)
    {
        Vector2 desiredVelocity = (dist > stopDistance) ? toPlayer.normalized * moveSpeed : Vector2.zero;

        chaseCurrentVelocity = Vector2.SmoothDamp(
            chaseCurrentVelocity,
            desiredVelocity,
            ref chaseVelSmoothRef,
            steeringSmooth
        );

        Vector2 newPos = rb.position + chaseCurrentVelocity * Time.fixedDeltaTime;
        rb.MovePosition(newPos);

        if (chaseCurrentVelocity.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(chaseCurrentVelocity.y, chaseCurrentVelocity.x) * Mathf.Rad2Deg;
            rb.rotation = angle; // sprite faces right by default
        }
    }

    // ================= SIDESTEP =================
    private bool ShouldStartSidestep(Vector2 toPlayer, float dist)
    {
        if (dist > sidestepTriggerDistance) return false;

        Vector2 playerVel = playerRB != null ? playerRB.linearVelocity : Vector2.zero;
        if (playerVel.magnitude < playerApproachSpeedThreshold) return false;

        Vector2 fromPlayerToEnemy = ((Vector2)transform.position - (Vector2)playerTarget.position).normalized;
        float dot = Vector2.Dot(playerVel.normalized, fromPlayerToEnemy);
        return dot >= approachDotThreshold;
    }

    private void BeginSidestep(Vector2 toPlayer)
    {
        isSidestepping = true;
        sidestepTimer = sidestepDuration;

        Vector2 dir = toPlayer.normalized;
        Vector2 perpLeft = new Vector2(-dir.y, dir.x).normalized;
        sidestepDirection = perpLeft;

        float targetSpeed = playerRB ? playerRB.linearVelocity.magnitude * sidestepSpeedMultiplier : moveSpeed;
        targetSpeed = Mathf.Clamp(targetSpeed, moveSpeed, sidestepMaxSpeed);

        sidestepTargetVelocity = sidestepDirection * targetSpeed;
        sidestepCurrentVelocity = Vector2.zero;
        sidestepVelSmoothRef = Vector2.zero;
    }

    private void SidestepMove()
    {
        sidestepTimer -= Time.fixedDeltaTime;
        if (sidestepTimer <= 0f) isSidestepping = false;

        sidestepCurrentVelocity = Vector2.SmoothDamp(
            sidestepCurrentVelocity,
            sidestepTargetVelocity,
            ref sidestepVelSmoothRef,
            sidestepSmooth
        );

        Vector2 newPos = rb.position + sidestepCurrentVelocity * Time.fixedDeltaTime;
        rb.MovePosition(newPos);

        if (sidestepCurrentVelocity.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(sidestepCurrentVelocity.y, sidestepCurrentVelocity.x) * Mathf.Rad2Deg;
            rb.rotation = angle;
        }
    }

    // ================= TARGET BINDING =================
    private void HandlePlayerChanged(Transform t)
    {
        BindTarget(t);
    }

    private void BindTarget(Transform t)
    {
        playerTarget = t;
        playerRB = playerTarget ? playerTarget.GetComponent<Rigidbody2D>() : null;
        playerHealth = playerTarget ? playerTarget.GetComponent<PlayerHealth>() : null;

        if (playerTarget == null)
            Debug.LogWarning("[EnemyFollow] Player target cleared.");
        else
        {
            if (playerRB == null)
                Debug.LogWarning("[EnemyFollow] Player has no Rigidbody2D. Sidestep logic limited.");
            if (playerHealth == null)
                Debug.LogWarning("[EnemyFollow] Player has no PlayerHealth. Cannot apply damage.");
        }
    }

    // ================= COLLISION DAMAGE (when physically touching player) =================
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (playerHealth == null) return;

        // Only react to the player root object
        if (!collision.collider.CompareTag("Player") &&
            !(collision.rigidbody && collision.rigidbody.gameObject.CompareTag("Player")))
            return;

        // measure sustained contact time
        _contactTimer += Time.deltaTime;
        if (_contactTimer < Mathf.Max(0f, minimumContactTime)) return;

        // optional "stuck" feeling: need low relative speed
        if (requireLowRelativeSpeed)
        {
            float relSpeed = 0f;
            Vector2 selfVel = chaseCurrentVelocity; // our kinematic intent
            if (playerRB != null)
                relSpeed = (playerRB.linearVelocity - selfVel).magnitude;
            else
                relSpeed = selfVel.magnitude;

            if (relSpeed > relativeSpeedThreshold) return;
        }

        TryTickDamage();
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (!collision.collider.CompareTag("Player") &&
            !(collision.rigidbody && collision.rigidbody.gameObject.CompareTag("Player")))
            return;

        _contactTimer = 0f;
    }

    // ================= STOP-AREA DAMAGE (when touching player's stop ring trigger) =================
    private void OnTriggerStay2D(Collider2D other)
    {
        if (!damageOnStopArea || playerHealth == null) return;

        // Must belong to the current player's stop frame
        if (useTagForStopArea)
        {
            if (!other.CompareTag(stopAreaTag)) return;
        }
        else
        {
            if (((1 << other.gameObject.layer) & stopAreaLayers.value) == 0) return;
        }

        // Also ensure this stop area belongs to our current player (avoid edge cases)
        // Check the root of the trigger we hit
        Transform root = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform.root;
        if (playerTarget == null || (root != playerTarget && root != playerTarget.root)) return;

        // For stop-area triggers we do not require "stuck" speed or min time;
        // but we still respect tickInterval to rate-limit.
        TryTickDamage();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!damageOnStopArea) return;

        if (useTagForStopArea)
        {
            if (!other.CompareTag(stopAreaTag)) return;
        }
        else
        {
            if (((1 << other.gameObject.layer) & stopAreaLayers.value) == 0) return;
        }

        _contactTimer = 0f; // reset just in case (shared timer)
    }

    // ================= DAMAGE TICK HELPER =================
    private void TryTickDamage()
    {
        if (Time.time < _nextDamageTime) return;

        float dealt = playerHealth.ApplyDamage(contactDamage);
        if (dealt > 0f)
            _nextDamageTime = Time.time + Mathf.Max(0f, tickInterval);
    }
}
