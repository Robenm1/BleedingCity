using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class FeatherChainOnHit : MonoBehaviour
{
    [Header("Owner")]
    public GameObject owner;

    [Header("Chain Settings")]
    [Tooltip("Maximum targets total, including the first enemy hit. 3 = hits max 3 enemies total.")]
    public int maxBounces = 3;

    public float damageMultiplier = 1f;

    [Header("Enemy Search")]
    [Tooltip("Maximum distance from one enemy to the next. Prevents across-map jumps.")]
    public float bounceSearchRadius = 6f;

    public LayerMask enemyLayers;

    [Tooltip("The feather only jumps forward inside this corridor.")]
    public float lineWidth = 2.25f;

    [Tooltip("Higher = stricter forward direction. Good values: 0.25 to 0.45.")]
    [Range(-1f, 1f)]
    public float minForwardDot = 0.25f;

    [Header("Real Feather Bounce")]
    public float bounceDelay = 0.03f;
    public float visualSpeed = 24f;

    [Tooltip("If true, the real feather moves in a straight line between enemies.")]
    public bool forceStraightLine = true;

    [Header("Final Stick Position")]
    [Tooltip("How far behind the last enemy the real feather sticks.")]
    public float stickBehindDistance = 0.45f;

    [Tooltip("Small side offset so the feather does not hide perfectly under the enemy.")]
    public float stickSideOffset = 0.15f;

    [Header("Renderer Control")]
    [Tooltip("Unused now. Kept only so FrostFeatherEffect.cs does not break.")]
    public bool hideRealFeatherDuringJump = false;

    [Tooltip("Unused now. Kept only so FrostFeatherEffect.cs does not break.")]
    public bool hideRealLineRendererDuringJump = false;

    [Tooltip("Unused now. Kept only so FrostFeatherEffect.cs does not break.")]
    public float visualScale = 1f;

    [Header("Debug")]
    public bool showDebug = false;

    private readonly HashSet<EnemyHealth> _hitEnemies = new HashSet<EnemyHealth>();

    private PlayerStats _stats;
    private Rigidbody2D _rb;

    private bool _chainStarted;
    private Vector3 _lastPosition;
    private Vector2 _lastMoveDirection = Vector2.right;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();

        if (owner != null)
            _stats = owner.GetComponent<PlayerStats>();

        if (_stats == null)
            _stats = FindObjectOfType<PlayerStats>();

        _lastPosition = transform.position;
    }

    private void Update()
    {
        CacheMoveDirection();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryStartChain(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryStartChain(collision.collider);
    }

    private void CacheMoveDirection()
    {
        Vector3 delta = transform.position - _lastPosition;

        if (delta.sqrMagnitude > 0.0001f)
            _lastMoveDirection = delta.normalized;

        if (_rb != null && _rb.linearVelocity.sqrMagnitude > 0.01f)
            _lastMoveDirection = _rb.linearVelocity.normalized;

        _lastPosition = transform.position;
    }

    private void TryStartChain(Collider2D col)
    {
        if (_chainStarted)
            return;

        EnemyHealth firstEnemy = GetEnemy(col);

        if (firstEnemy == null)
            return;

        _chainStarted = true;
        _hitEnemies.Add(firstEnemy);

        Vector2 chainDirection = GetChainDirection(firstEnemy);

        StartCoroutine(ChainRoutine(firstEnemy, chainDirection));
    }

    private Vector2 GetChainDirection(EnemyHealth firstEnemy)
    {
        Vector2 dir = _lastMoveDirection;

        if (dir.sqrMagnitude <= 0.001f && firstEnemy != null)
            dir = ((Vector2)firstEnemy.transform.position - (Vector2)transform.position).normalized;

        if (dir.sqrMagnitude <= 0.001f)
            dir = Vector2.right;

        return dir.normalized;
    }

    private IEnumerator ChainRoutine(EnemyHealth firstEnemy, Vector2 chainDirection)
    {
        StopFeatherPhysicsVelocity();

        Vector3 currentPosition = firstEnemy.transform.position;
        Vector3 lastTargetPosition = currentPosition;
        Vector2 lastDirection = chainDirection;

        transform.position = currentPosition;
        RotateToward(chainDirection);

        int maxTargetsTotal = Mathf.Clamp(maxBounces, 1, 3);
        int jumpsAllowed = maxTargetsTotal - 1;
        int jumpsDone = 0;

        while (jumpsDone < jumpsAllowed)
        {
            if (bounceDelay > 0f)
                yield return new WaitForSeconds(bounceDelay);

            EnemyHealth nextEnemy = FindNextEnemyInStraightLine(currentPosition, chainDirection);

            if (nextEnemy == null)
                break;

            Vector3 nextPosition = nextEnemy.transform.position;

            yield return MoveRealFeatherStraight(currentPosition, nextPosition);

            DealChainDamage(nextEnemy);

            _hitEnemies.Add(nextEnemy);

            Vector2 newDirection = ((Vector2)nextPosition - (Vector2)currentPosition).normalized;

            if (newDirection.sqrMagnitude > 0.001f)
                chainDirection = newDirection;

            lastDirection = chainDirection;
            lastTargetPosition = nextPosition;
            currentPosition = nextPosition;

            jumpsDone++;

            if (showDebug)
                Debug.Log($"[FeatherChainOnHit] Real feather bounced {jumpsDone}/{jumpsAllowed} to {nextEnemy.name}.");
        }

        Vector3 finalStickPosition = GetStickPositionBehindLastTarget(lastTargetPosition, lastDirection);

        transform.position = finalStickPosition;
        RotateToward(lastDirection);
        StopFeatherPhysicsVelocity();

        if (showDebug)
            Debug.Log("[FeatherChainOnHit] Feather stuck behind last bounced enemy.");
    }

    private IEnumerator MoveRealFeatherStraight(Vector3 start, Vector3 end)
    {
        float distance = Vector3.Distance(start, end);
        float duration = distance / Mathf.Max(0.1f, visualSpeed);
        duration = Mathf.Max(0.025f, duration);

        Vector3 direction = end - start;

        if (direction.sqrMagnitude <= 0.001f)
            direction = Vector3.right;

        RotateToward(direction);

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / duration);
            float smoothT = t * t * (3f - 2f * t);

            transform.position = Vector3.Lerp(start, end, smoothT);
            RotateToward(direction);
            StopFeatherPhysicsVelocity();

            yield return null;
        }

        transform.position = end;
        StopFeatherPhysicsVelocity();
    }

    private EnemyHealth FindNextEnemyInStraightLine(Vector3 fromPosition, Vector2 direction)
    {
        LayerMask searchLayer = enemyLayers.value == 0 ? ~0 : enemyLayers;

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            fromPosition,
            Mathf.Max(0.1f, bounceSearchRadius),
            searchLayer
        );

        EnemyHealth bestEnemy = null;
        float bestScore = float.MaxValue;

        direction.Normalize();

        for (int i = 0; i < hits.Length; i++)
        {
            EnemyHealth enemy = GetEnemy(hits[i]);

            if (!IsValidChainTarget(enemy))
                continue;

            Vector2 toEnemy = (Vector2)enemy.transform.position - (Vector2)fromPosition;
            float distance = toEnemy.magnitude;

            if (distance <= 0.05f)
                continue;

            if (distance > bounceSearchRadius)
                continue;

            Vector2 toEnemyDir = toEnemy / distance;
            float forwardDot = Vector2.Dot(direction, toEnemyDir);

            if (forwardDot < minForwardDot)
                continue;

            float sideDistance = Mathf.Abs(Vector3.Cross(direction, toEnemy).z);

            if (sideDistance > lineWidth)
                continue;

            float score = sideDistance * 10f + distance * 0.25f - forwardDot * 1.5f;

            if (score < bestScore)
            {
                bestScore = score;
                bestEnemy = enemy;
            }
        }

        return bestEnemy;
    }

    private Vector3 GetStickPositionBehindLastTarget(Vector3 lastTargetPosition, Vector2 lastDirection)
    {
        if (lastDirection.sqrMagnitude <= 0.001f)
            lastDirection = Vector2.right;

        Vector2 back = -lastDirection.normalized * Mathf.Max(0f, stickBehindDistance);
        Vector2 side = new Vector2(-lastDirection.y, lastDirection.x) * Random.Range(-stickSideOffset, stickSideOffset);

        return lastTargetPosition + (Vector3)(back + side);
    }

    private void StopFeatherPhysicsVelocity()
    {
        if (_rb == null)
            return;

        _rb.linearVelocity = Vector2.zero;
        _rb.angularVelocity = 0f;
    }

    private void RotateToward(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0.001f)
            return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private bool IsValidChainTarget(EnemyHealth enemy)
    {
        if (enemy == null)
            return false;

        if (_hitEnemies.Contains(enemy))
            return false;

        if (enemy.GetHealthPercent() <= 0f)
            return false;

        if (enemy.GetComponent<AbyssalDollObject>() != null)
            return false;

        return true;
    }

    private void DealChainDamage(EnemyHealth enemy)
    {
        if (enemy == null)
            return;

        if (_stats == null)
        {
            if (owner != null)
                _stats = owner.GetComponent<PlayerStats>();

            if (_stats == null)
                _stats = FindObjectOfType<PlayerStats>();
        }

        float damage = _stats != null
            ? _stats.GetDamage() * damageMultiplier
            : 10f * damageMultiplier;

        enemy.TakeDamageDirectFromSource(owner != null ? owner : gameObject, damage);
    }

    private EnemyHealth GetEnemy(Collider2D col)
    {
        if (col == null)
            return null;

        EnemyHealth enemy = col.GetComponent<EnemyHealth>();

        if (enemy == null)
            enemy = col.GetComponentInParent<EnemyHealth>();

        return enemy;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.1f, bounceSearchRadius));
    }
#endif
}