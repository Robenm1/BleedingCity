using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyFollow : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The player to chase. If empty, we'll search for tag 'Player'.")]
    public Transform playerTarget;

    private Rigidbody playerRB;
    private PlayerHealth playerHealth;

    [Header("Chase Settings")]
    public float moveSpeed = 3f;
    [Tooltip("Stop moving if within this distance (prevents face-hugging).")]
    public float stopDistance = 1.2f;
    [Range(0f, 1f)] public float steeringSmooth = 0.15f;

    [Header("Slow Effect")]
    private float slowMultiplier = 1f;

    [Header("Anti-Block / Sidestep")]
    public bool avoidBlockingPlayer = true;
    public float sidestepTriggerDistance = 1.5f;
    public float playerApproachSpeedThreshold = 1f;
    [Range(0f, 1f)] public float approachDotThreshold = 0.7f;
    public float sidestepDuration = 0.3f;
    public float sidestepSpeedMultiplier = 1.2f;
    public float sidestepMaxSpeed = 5f;
    [Range(0f, 1f)] public float sidestepSmooth = 0.25f;

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

    [Header("Stop Area Damage (Trigger)")]
    [Tooltip("If true, the player takes damage when this enemy touches the player's 'stop area' trigger.")]
    public bool damageOnStopArea = true;

    [Tooltip("Use tag match for the stop area. If false, use the layer mask.")]
    public bool useTagForStopArea = true;

    [Tooltip("Tag to identify the player's stop-area trigger.")]
    public string stopAreaTag = "PlayerStopArea";

    [Tooltip("Layer(s) for the player's stop-area trigger if not using tag.")]
    public LayerMask stopAreaLayers;

    [Header("Stuck Detection")]
    [Tooltip("Distance threshold to detect if enemy is stuck")]
    public float stuckDistanceThreshold = 0.05f;
    [Tooltip("Time before considering enemy stuck")]
    public float stuckTimeThreshold = 0.5f;

    private float _nextDamageTime = 0f;
    private float _contactTimer = 0f;

    private bool isSidestepping = false;
    private float sidestepTimer = 0f;
    private Vector3 sidestepDirection;
    private Vector3 sidestepCurrentVelocity;
    private Vector3 sidestepVelSmoothRef;
    private Vector3 sidestepTargetVelocity;

    private Rigidbody rb;
    private Vector3 chaseCurrentVelocity;
    private Vector3 chaseVelSmoothRef;

    private float _retryFindTimer = 0f;

    private Vector3 lastPosition;
    private float stuckTimer = 0f;

    private void OnEnable()
    {
        PlayerLocator.OnPlayerChanged += HandlePlayerChanged;
    }

    private void OnDisable()
    {
        PlayerLocator.OnPlayerChanged -= HandlePlayerChanged;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        lastPosition = rb.position;
    }

    private void Start()
    {
        if (playerTarget == null && PlayerLocator.Current != null)
            BindTarget(PlayerLocator.Current);

        if (playerTarget == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) BindTarget(p.transform);
        }
    }

    private void Update()
    {
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

        CheckIfStuck();

        // Use XZ plane only — ignore Y difference
        Vector3 toPlayer3D = playerTarget.position - transform.position;
        Vector3 toPlayerFlat = new Vector3(toPlayer3D.x, 0f, toPlayer3D.z);
        float dist = toPlayerFlat.magnitude;

        if (isSidestepping)
        {
            SidestepMove();
            return;
        }

        if (avoidBlockingPlayer && playerRB != null && ShouldStartSidestep(toPlayerFlat, dist))
        {
            BeginSidestep(toPlayerFlat);
            SidestepMove();
            return;
        }

        NormalChaseMove(toPlayerFlat, dist);
    }

    private void CheckIfStuck()
    {
        float distanceMoved = Vector3.Distance(rb.position, lastPosition);

        if (distanceMoved < stuckDistanceThreshold && (chaseCurrentVelocity.sqrMagnitude > 0.01f || sidestepCurrentVelocity.sqrMagnitude > 0.01f))
        {
            stuckTimer += Time.fixedDeltaTime;

            if (stuckTimer >= stuckTimeThreshold)
            {
                chaseCurrentVelocity   = Vector3.zero;
                sidestepCurrentVelocity = Vector3.zero;
                chaseVelSmoothRef      = Vector3.zero;
                sidestepVelSmoothRef   = Vector3.zero;
                isSidestepping         = false;
                stuckTimer             = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
        }

        lastPosition = rb.position;
    }

    private void NormalChaseMove(Vector3 toPlayerFlat, float dist)
    {
        float effectiveSpeed = moveSpeed * slowMultiplier;
        Vector3 desiredVelocity = (dist > stopDistance) ? toPlayerFlat.normalized * effectiveSpeed : Vector3.zero;

        chaseCurrentVelocity = Vector3.SmoothDamp(
            chaseCurrentVelocity,
            desiredVelocity,
            ref chaseVelSmoothRef,
            steeringSmooth
        );

        Vector3 newPos = rb.position + chaseCurrentVelocity * Time.fixedDeltaTime;
        newPos.y = rb.position.y;
        rb.MovePosition(newPos);

        // Face movement direction (rotate on Y axis only)
        if (chaseCurrentVelocity.sqrMagnitude > 0.001f)
        {
            Vector3 flatDir = new Vector3(chaseCurrentVelocity.x, 0f, chaseCurrentVelocity.z);
            if (flatDir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(flatDir, Vector3.up);
        }
    }

    private bool ShouldStartSidestep(Vector3 toPlayerFlat, float dist)
    {
        if (dist > sidestepTriggerDistance) return false;

        Vector3 playerVel3D = playerRB != null ? playerRB.linearVelocity : Vector3.zero;
        Vector3 playerVelFlat = new Vector3(playerVel3D.x, 0f, playerVel3D.z);
        if (playerVelFlat.magnitude < playerApproachSpeedThreshold) return false;

        Vector3 fromPlayerToEnemy = (transform.position - playerTarget.position);
        fromPlayerToEnemy.y = 0f;
        fromPlayerToEnemy.Normalize();
        float dot = Vector3.Dot(playerVelFlat.normalized, fromPlayerToEnemy);
        return dot >= approachDotThreshold;
    }

    private void BeginSidestep(Vector3 toPlayerFlat)
    {
        isSidestepping = true;
        sidestepTimer  = sidestepDuration;

        Vector3 dir = toPlayerFlat.normalized;
        // Perpendicular on the XZ plane
        Vector3 perpLeft = new Vector3(-dir.z, 0f, dir.x).normalized;
        sidestepDirection = perpLeft;

        Vector3 playerVel3D   = playerRB ? playerRB.linearVelocity : Vector3.zero;
        Vector3 playerVelFlat = new Vector3(playerVel3D.x, 0f, playerVel3D.z);
        float targetSpeed = playerVelFlat.magnitude * sidestepSpeedMultiplier;
        targetSpeed = Mathf.Clamp(targetSpeed, moveSpeed, sidestepMaxSpeed);
        targetSpeed *= slowMultiplier;

        sidestepTargetVelocity  = sidestepDirection * targetSpeed;
        sidestepCurrentVelocity = Vector3.zero;
        sidestepVelSmoothRef    = Vector3.zero;
    }

    private void SidestepMove()
    {
        sidestepTimer -= Time.fixedDeltaTime;
        if (sidestepTimer <= 0f) isSidestepping = false;

        sidestepCurrentVelocity = Vector3.SmoothDamp(
            sidestepCurrentVelocity,
            sidestepTargetVelocity,
            ref sidestepVelSmoothRef,
            sidestepSmooth
        );

        Vector3 newPos = rb.position + sidestepCurrentVelocity * Time.fixedDeltaTime;
        newPos.y = rb.position.y;
        rb.MovePosition(newPos);

        if (sidestepCurrentVelocity.sqrMagnitude > 0.001f)
        {
            Vector3 flatDir = new Vector3(sidestepCurrentVelocity.x, 0f, sidestepCurrentVelocity.z);
            if (flatDir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(flatDir, Vector3.up);
        }
    }

    public void ApplySlow(float slowPercent)
    {
        slowMultiplier = 1f - slowPercent;
    }

    public void RemoveSlow(float slowPercent)
    {
        slowMultiplier = 1f;
    }

    private void HandlePlayerChanged(Transform t)
    {
        BindTarget(t);
    }

    private void BindTarget(Transform t)
    {
        playerTarget = t;
        playerRB     = playerTarget ? playerTarget.GetComponent<Rigidbody>() : null;
        playerHealth = playerTarget ? playerTarget.GetComponent<PlayerHealth>() : null;

        if (playerTarget == null)
            Debug.LogWarning("[EnemyFollow] Player target cleared.");
        else
        {
            if (playerRB == null)
                Debug.LogWarning("[EnemyFollow] Player has no Rigidbody. Sidestep logic limited.");
            if (playerHealth == null)
                Debug.LogWarning("[EnemyFollow] Player has no PlayerHealth. Cannot apply damage.");
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (playerHealth == null) return;

        if (!collision.collider.CompareTag("Player") &&
            !(collision.rigidbody && collision.rigidbody.gameObject.CompareTag("Player")))
            return;

        _contactTimer += Time.deltaTime;
        if (_contactTimer < Mathf.Max(0f, minimumContactTime)) return;

        if (requireLowRelativeSpeed)
        {
            float relSpeed = 0f;
            Vector3 selfVel3D   = chaseCurrentVelocity;
            Vector3 selfVelFlat = new Vector3(selfVel3D.x, 0f, selfVel3D.z);
            if (playerRB != null)
            {
                Vector3 pv = playerRB.linearVelocity;
                relSpeed = (new Vector3(pv.x, 0f, pv.z) - selfVelFlat).magnitude;
            }
            else
            {
                relSpeed = selfVelFlat.magnitude;
            }

            if (relSpeed > relativeSpeedThreshold) return;
        }

        TryTickDamage();
    }

    private void OnCollisionExit(Collision collision)
    {
        if (!collision.collider.CompareTag("Player") &&
            !(collision.rigidbody && collision.rigidbody.gameObject.CompareTag("Player")))
            return;

        _contactTimer = 0f;
    }

    private void OnTriggerStay(Collider other)
    {
        if (!damageOnStopArea || playerHealth == null) return;

        if (useTagForStopArea)
        {
            if (!other.CompareTag(stopAreaTag)) return;
        }
        else
        {
            if (((1 << other.gameObject.layer) & stopAreaLayers.value) == 0) return;
        }

        Transform root = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform.root;
        if (playerTarget == null || (root != playerTarget && root != playerTarget.root)) return;

        TryTickDamage();
    }

    private void OnTriggerExit(Collider other)
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

        _contactTimer = 0f;
    }

    private void TryTickDamage()
    {
        if (Time.time < _nextDamageTime) return;

        float dealt = playerHealth.ApplyDamage(contactDamage);
        if (dealt > 0f)
            _nextDamageTime = Time.time + Mathf.Max(0f, tickInterval);
    }
}
