using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DezzoShark : MonoBehaviour
{
    public enum State { Idle, Seek, BiteWindup, Retreat, Cooldown }

    [Header("Owner wiring")]
    public DezzoSharkManager owner;
    [HideInInspector] public int index = 0;                // set by manager
    [HideInInspector] public Collider2D playerCollider;    // set by manager

    [Header("Bite")]
    public float biteRange = 0.75f;
    public float retreatDuration = 0.6f;
    public float retreatDistance = 2.5f;
    public float damageMultiplier = 1f;

    [Header("Motion")]
    [Range(0f, 1f)] public float turnSmooth = 0.15f;

    [Header("Anti-Stack / Separation")]
    public float separationRadius = 1.0f;
    public float separationStrength = 3.0f;
    public float avoidPlayerRadius = 0.9f;
    public float avoidPlayerStrength = 3.0f;

    [Header("Leash")]
    [Tooltip("How hard to pull sharks back inside PlayerStats.attackRange when they get outside.")]
    public float leashStrength = 6f;
    [Tooltip("Start pulling back slightly outside range to avoid edge jitter.")]
    public float leashBuffer = 0.1f; // 10% beyond attackRange before strong pull

    [Header("Collision")]
    [Tooltip("Force the shark collider to be a Trigger to avoid pushing.")]
    public bool forceTriggerCollider = true;

    private Transform target;
    private State state = State.Idle;
    private float retreatTimer = 0f;
    private float attackCooldown = 0f;
    private Vector2 velocity;

    private Collider2D myCol;

    // Opposite orbit per index: 0 = CW (+1), 1 = CCW (-1)
    private float orbitDir => (index % 2 == 0) ? +1f : -1f;

    public bool IsBusy => state == State.BiteWindup || state == State.Retreat || state == State.Cooldown;

    private void Awake()
    {
        myCol = GetComponent<Collider2D>();
        if (owner == null)
            owner = GetComponentInParent<DezzoSharkManager>();
    }

    private void Start()
    {
        // Stop pushing the player
        if (forceTriggerCollider && myCol != null) myCol.isTrigger = true;
        if (playerCollider != null && myCol != null)
            Physics2D.IgnoreCollision(myCol, playerCollider, true);
    }

    private void Update()
    {
        if (attackCooldown > 0f)
        {
            attackCooldown -= Time.deltaTime;
            if (attackCooldown < 0f) attackCooldown = 0f;
        }

        switch (state)
        {
            case State.Idle: TickIdle(); break;
            case State.Seek: TickSeek(); break;
            case State.BiteWindup: TickBite(); break;
            case State.Retreat: TickRetreat(); break;
            case State.Cooldown: TickCooldown(); break;
        }
    }

    public void AssignTarget(Transform t)
    {
        target = t;
        if (t != null && state != State.Retreat && state != State.Cooldown) state = State.Seek;
        else if (t == null && state == State.Seek) state = State.Idle;
    }

    public Transform GetTarget() => target;

    // ---------- States ----------

    private void TickIdle()
    {
        if (owner == null) return;

        // Target a very close orbit: min(custom idle radius, 30% of attack range)
        float closeOrbit = Mathf.Min(owner.idleOrbitRadius, owner.GetAttackRange() * 0.3f);

        Vector2 toCenter = (Vector2)(owner.transform.position - transform.position);
        float dist = toCenter.magnitude;

        // Tangent (CW/CCW per shark index)
        Vector2 tangent = orbitDir * new Vector2(-toCenter.y, toCenter.x).normalized;

        // Base orbit speed
        Vector2 desiredVel = tangent * owner.GetSharkMoveSpeed();

        // Stronger radial correction to snap into close orbit
        float radialErr = dist - closeOrbit;
        // was 0.5f — increase to 1.0f for tighter pull
        desiredVel += toCenter.normalized * Mathf.Clamp(-radialErr, -owner.GetSharkMoveSpeed(), owner.GetSharkMoveSpeed()) * 1.0f;

        // Keep spacing and avoid the player a bit
        desiredVel += SeparationVector() + AvoidPlayerVector();

        // Leash still helps if we somehow drift near range
        desiredVel += LeashVector();

        velocity = Vector2.Lerp(velocity, desiredVel, 1f - turnSmooth);
        transform.position += (Vector3)(velocity * Time.deltaTime);
        FaceVelocity();
    }

    private void TickSeek()
    {
        if (target == null) { state = State.Idle; return; }

        Vector2 toTarget = (Vector2)(target.position - transform.position);
        float distToTarget = toTarget.magnitude;

        float speed = owner.GetSharkMoveSpeed();
        Vector2 desired = toTarget.normalized * speed;

        // Keep spacing
        desired += SeparationVector() + AvoidPlayerVector();

        // LEASH: don’t chase beyond PlayerStats.attackRange
        desired += LeashVector();

        velocity = Vector2.Lerp(velocity, desired, 1f - turnSmooth);
        transform.position += (Vector3)(velocity * Time.deltaTime);
        FaceVelocity();

        if (distToTarget <= biteRange && attackCooldown <= 0f)
            state = State.BiteWindup;
    }

    private void TickBite()
    {
        if (target == null) { state = State.Idle; return; }

        var eh = target.GetComponent<EnemyHealth>();
        if (eh != null && owner != null)
        {
            float dmg = owner.GetSharkDamage() * damageMultiplier;
            eh.TakeDamage(dmg);
        }

        BeginRetreat();
    }

    private void BeginRetreat()
    {
        retreatTimer = retreatDuration;
        state = State.Retreat;

        attackCooldown = owner != null ? owner.GetAttackDelay() : 0.4f;

        Vector2 away;
        if (target != null) away = ((Vector2)transform.position - (Vector2)target.position).normalized;
        else if (owner != null) away = ((Vector2)transform.position - (Vector2)owner.transform.position).normalized;
        else away = Random.insideUnitCircle.normalized;

        // Peel sideways a bit
        Vector2 tangent = orbitDir * new Vector2(-away.y, away.x) * 0.7f;
        Vector2 retreatDir = (away + tangent).normalized;

        velocity = retreatDir * owner.GetSharkMoveSpeed();
    }

    private void TickRetreat()
    {
        retreatTimer -= Time.deltaTime;

        Vector2 desired = velocity + SeparationVector() + AvoidPlayerVector() + LeashVector();
        transform.position += (Vector3)(desired * Time.deltaTime);
        FaceVelocity();

        velocity = Vector2.Lerp(velocity, Vector2.zero, Time.deltaTime * 2f);

        if (retreatTimer <= 0f) state = State.Cooldown;
    }

    private void TickCooldown()
    {
        if (attackCooldown <= 0f)
            state = (target != null) ? State.Seek : State.Idle;
        else
            TickIdle();
    }

    // ---------- Steering helpers ----------

    private Vector2 SeparationVector()
    {
        if (owner == null || owner.sharks == null) return Vector2.zero;

        Vector2 repel = Vector2.zero;
        Vector2 myPos = transform.position;

        for (int i = 0; i < owner.sharks.Length; i++)
        {
            var other = owner.sharks[i];
            if (other == null || other == this) continue;

            Vector2 oPos = other.transform.position;
            Vector2 toMe = myPos - oPos;
            float d = toMe.magnitude;
            if (d < 0.0001f) continue;

            if (d < separationRadius)
            {
                float strength = (separationRadius - d) / separationRadius; // 0..1
                repel += toMe.normalized * (strength * separationStrength);
            }
        }
        return repel;
    }

    private Vector2 AvoidPlayerVector()
    {
        if (owner == null) return Vector2.zero;
        Vector2 toPlayer = (Vector2)(transform.position - owner.transform.position);
        float d = toPlayer.magnitude;
        if (d < avoidPlayerRadius && d > 0.0001f)
        {
            float strength = (avoidPlayerRadius - d) / avoidPlayerRadius;
            return toPlayer.normalized * (strength * avoidPlayerStrength);
        }
        return Vector2.zero;
    }

    private Vector2 LeashVector()
    {
        if (owner == null) return Vector2.zero;

        float max = owner.GetAttackRange();
        float buffer = Mathf.Max(0f, leashBuffer);
        float leashEdge = max * (1f + buffer);

        Vector2 toCenter = (Vector2)(owner.transform.position - transform.position);
        float dist = toCenter.magnitude;

        if (dist > leashEdge)
        {
            // Strong pull inward when outside the buffered ring
            float over = Mathf.Clamp01((dist - leashEdge) / (max * 0.5f + 0.001f));
            return toCenter.normalized * (leashStrength * over);
        }

        // Soft bias inward as we approach the limit to avoid surfing the edge
        if (dist > max * 0.98f)
        {
            float t = Mathf.InverseLerp(max, leashEdge, dist); // 0 at max, 1 at leashEdge
            return toCenter.normalized * (leashStrength * 0.5f * t);
        }

        return Vector2.zero;
    }

    private void FaceVelocity()
    {
        if (velocity.sqrMagnitude > 0.0001f)
        {
            float ang = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, ang);
        }
    }
}