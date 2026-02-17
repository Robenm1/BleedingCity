// Assets/Scripts/Player/Dezzo/DezzoShark.cs
using UnityEngine;
using System.Reflection;

[RequireComponent(typeof(Collider))]
public class DezzoShark : MonoBehaviour
{
    public enum State { Idle, Seek, BiteWindup, Retreat, Cooldown }

    public static System.Action<EnemyHealth, Transform, float> OnSharkHit;

    [Header("Owner wiring")]
    public DezzoSharkManager owner;
    [HideInInspector] public int index = 0;
    [HideInInspector] public Collider playerCollider;

    [Header("Bite / Attack")]
    public float biteRange = 0.75f;
    public float damageMultiplier = 1f;

    [Header("Post-bite Retreat")]
    public float retreatDuration = 0.6f;
    public float retreatDistance = 2.5f;

    [Header("Motion")]
    [Range(0f, 1f)] public float turnSmooth = 0.15f;

    [Header("Anti-Stack / Separation")]
    public float separationRadius = 0.7f;
    public float separationStrength = 3.5f;

    [Header("Avoid Player Core")]
    public float avoidPlayerRadius = 0.85f;
    public float avoidPlayerStrength = 3.0f;

    [Header("Idle Orbit / Leash")]
    public float orbitAngularSpeed = 110f;
    [Range(0f, 0.4f)] public float outerOrbitInsetFraction = 0.05f;
    public float leashBuffer = 0.10f;
    public float leashStrength = 6f;

    [Header("Collision")]
    public bool forceTriggerCollider = true;

    [Header("Rotation (toward enemy)")]
    public float maxAngularSpeed = 220f;
    public float minAngleDelta = 2f;

    private Transform target;
    private State state = State.Idle;
    private float retreatTimer = 0f;
    private float attackCooldown = 0f;
    private Vector3 velocity;
    private Collider myCol;
    private float currentAngle;

    private float BaseOrbitAngleDeg => (Time.time * orbitAngularSpeed) % 360f;
    public bool IsBusy => state == State.BiteWindup || state == State.Retreat || state == State.Cooldown;

    private void Awake()
    {
        myCol = GetComponent<Collider>();
        if (owner == null) owner = GetComponentInParent<DezzoSharkManager>();
    }

    private void Start()
    {
        if (forceTriggerCollider && myCol != null) myCol.isTrigger = true;
        if (playerCollider != null && myCol != null)
            Physics.IgnoreCollision(myCol, playerCollider, true);
        currentAngle = transform.eulerAngles.y;
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

        FaceTowardTarget();
        HardLeashClamp();
    }

    public void AssignTarget(Transform t)
    {
        target = t;
        if (t != null && state != State.Retreat && state != State.Cooldown) state = State.Seek;
        else if (t == null && state == State.Seek) state = State.Idle;
    }

    public Transform GetTarget() => target;

    private void TickIdle()
    {
        if (owner == null) return;

        float detectR  = Mathf.Max(0.1f, owner.GetDetectionRadius());
        float targetR  = detectR * (1f - Mathf.Clamp01(outerOrbitInsetFraction));
        float angle    = BaseOrbitAngleDeg + (index * 180f);
        if (angle >= 360f) angle -= 360f;

        float rad = angle * Mathf.Deg2Rad;
        // Orbit on XZ plane
        Vector3 center = owner.transform.position;
        Vector3 anchor = center + new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * targetR;

        Vector3 toAnchor = anchor - transform.position;
        toAnchor.y = 0f;
        Vector3 desiredVel = toAnchor.sqrMagnitude > 0.0001f
            ? toAnchor.normalized * owner.GetSharkMoveSpeed()
            : Vector3.zero;

        desiredVel += SeparationVector() + AvoidPlayerVector();

        velocity = Vector3.Lerp(velocity, desiredVel, 1f - turnSmooth);
        Vector3 newPos = transform.position + velocity * Time.deltaTime;
        newPos.y = owner.transform.position.y;
        transform.position = newPos;
    }

    private void TickSeek()
    {
        if (target == null) { state = State.Idle; return; }

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        float speed = owner != null ? owner.GetSharkMoveSpeed() : 12f;

        var eh = target.GetComponent<EnemyHealth>();
        var bm = eh ? eh.GetComponent<EnemyBiteMark>() : null;
        if (bm != null && bm.isActive) speed *= Mathf.Max(0.01f, bm.moveMul);

        Vector3 desired = toTarget.normalized * speed;
        desired += SeparationVector() + AvoidPlayerVector();

        velocity = Vector3.Lerp(velocity, desired, 1f - turnSmooth);
        Vector3 newPos = transform.position + velocity * Time.deltaTime;
        newPos.y = owner.transform.position.y;
        transform.position = newPos;

        if (toTarget.magnitude <= biteRange && attackCooldown <= 0f)
            state = State.BiteWindup;
    }

    private void TickBite()
    {
        if (target == null) { state = State.Idle; return; }

        var eh = target.GetComponent<EnemyHealth>();
        float dealt = 0f;

        if (eh != null && owner != null)
        {
            float dmg = owner.GetSharkDamage() * damageMultiplier;

            // Execute & damage mul vs bite-marked
            var bm = eh.GetComponent<EnemyBiteMark>();
            if (bm != null && bm.isActive && TryGetEnemyHP(eh, out float cur, out float max))
            {
                if (bm.ShouldExecute(cur, max))
                {
                    eh.TakeDamage(999999f);
                    OnSharkHit?.Invoke(eh, this.transform, 999999f);
                    BeginRetreat();
                    return;
                }
                dmg *= Mathf.Max(0.01f, bm.damageMul);
            }

            dealt = Mathf.Max(0f, dmg);
            eh.TakeDamage(dealt);
        }

        // notify listeners (increments hit counters)
        if (eh != null) OnSharkHit?.Invoke(eh, this.transform, dealt);

        BeginRetreat();
    }

    private void BeginRetreat()
    {
        retreatTimer = retreatDuration;
        state        = State.Retreat;

        float cd = owner != null ? owner.GetAttackDelay() : 0.4f;

        if (target != null)
        {
            var eh = target.GetComponent<EnemyHealth>();
            var bm = eh ? eh.GetComponent<EnemyBiteMark>() : null;
            if (bm != null && bm.isActive) cd *= Mathf.Max(0.01f, bm.attackDelayMul);
        }

        attackCooldown = cd;

        Vector3 away;
        if (target != null)       away = (transform.position - target.position).normalized;
        else if (owner != null)   away = (transform.position - owner.transform.position).normalized;
        else                      away = new Vector3(Random.insideUnitCircle.x, 0f, Random.insideUnitCircle.y).normalized;

        away.y = 0f;
        Vector3 tangent    = new Vector3(-away.z, 0f, away.x) * 0.7f;
        Vector3 retreatDir = (away + tangent).normalized;
        velocity = retreatDir * (owner != null ? owner.GetSharkMoveSpeed() : 12f);
    }

    private void TickRetreat()
    {
        retreatTimer -= Time.deltaTime;
        Vector3 desired = velocity + SeparationVector() + AvoidPlayerVector();
        Vector3 newPos  = transform.position + desired * Time.deltaTime;
        if (owner != null) newPos.y = owner.transform.position.y;
        transform.position = newPos;
        velocity = Vector3.Lerp(velocity, Vector3.zero, Time.deltaTime * 2f);
        if (retreatTimer <= 0f) state = State.Cooldown;
    }

    private void TickCooldown()
    {
        if (attackCooldown <= 0f)
            state = (target != null) ? State.Seek : State.Idle;
        else
            TickIdle();
    }

    private Vector3 SeparationVector()
    {
        if (owner == null || owner.sharks == null) return Vector3.zero;

        Vector3 repel = Vector3.zero;

        for (int i = 0; i < owner.sharks.Length; i++)
        {
            var other = owner.sharks[i];
            if (other == null || other == this) continue;

            Vector3 toMe = transform.position - other.transform.position;
            toMe.y = 0f;
            float d = toMe.magnitude;
            if (d < 0.0001f) continue;

            if (d < separationRadius)
            {
                float strength = (separationRadius - d) / separationRadius;
                repel += toMe.normalized * (strength * separationStrength);
            }
        }
        return repel;
    }

    private Vector3 AvoidPlayerVector()
    {
        if (owner == null) return Vector3.zero;
        Vector3 toPlayer = transform.position - owner.transform.position;
        toPlayer.y = 0f;
        float d = toPlayer.magnitude;
        if (d < avoidPlayerRadius && d > 0.0001f)
        {
            float strength = (avoidPlayerRadius - d) / avoidPlayerRadius;
            return toPlayer.normalized * (strength * avoidPlayerStrength);
        }
        return Vector3.zero;
    }

    private void HardLeashClamp()
    {
        if (owner == null) return;

        float detectR    = Mathf.Max(0.1f, owner.GetDetectionRadius());
        float bufferEdge = detectR * (1f + Mathf.Max(0f, leashBuffer));
        float hardMax    = detectR;

        Vector3 center = owner.transform.position;
        Vector3 pos    = transform.position;
        Vector3 toMe   = pos - center;
        toMe.y         = 0f;
        float d        = toMe.magnitude;

        if (d > bufferEdge)
        {
            float over    = Mathf.Clamp01((d - bufferEdge) / (detectR * 0.5f + 0.001f));
            Vector3 pull  = (-toMe.normalized) * (leashStrength * over);
            Vector3 delta = pull * Time.deltaTime;
            delta.y       = 0f;
            transform.position += delta;
        }

        if (d > hardMax)
        {
            Vector3 clamped    = center + toMe.normalized * hardMax;
            clamped.y          = center.y;
            transform.position = clamped;

            Vector3 pullDir = (center - clamped).normalized;
            float pullSpd   = (owner != null ? owner.GetSharkMoveSpeed() : 12f) * 0.25f;
            velocity        = Vector3.Lerp(velocity, pullDir * pullSpd, 0.5f);
        }
    }

    private void FaceTowardTarget()
    {
        if (target == null) return;

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(toTarget, Vector3.up);
        transform.rotation   = Quaternion.RotateTowards(transform.rotation, targetRot, maxAngularSpeed * Time.deltaTime);
    }

    private static bool TryGetEnemyHP(EnemyHealth eh, out float current, out float max)
    {
        current = 0f; max = 0f;
        if (!eh) return false;

        var getCur = eh.GetType().GetMethod("GetCurrentHP", BindingFlags.Public | BindingFlags.Instance);
        var getMax = eh.GetType().GetMethod("GetMaxHP", BindingFlags.Public | BindingFlags.Instance);
        if (getCur != null && getMax != null)
        {
            current = (float)getCur.Invoke(eh, null);
            max = (float)getMax.Invoke(eh, null);
            return true;
        }

        var fCur = eh.GetType().GetField("currentHP", BindingFlags.NonPublic | BindingFlags.Instance);
        var fMax = eh.GetType().GetField("maxHP", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
        if (fCur != null && fMax != null)
        {
            current = (float)fCur.GetValue(eh);
            max = (float)fMax.GetValue(eh);
            return true;
        }

        return false;
    }
}
