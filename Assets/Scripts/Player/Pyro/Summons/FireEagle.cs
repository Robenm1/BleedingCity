using UnityEngine;

public class FireEagle : MonoBehaviour
{
    [Header("Follow Behavior")]
    [Tooltip("The player to follow")]
    public Transform owner;

    [Tooltip("Height offset above player")]
    public float hoverHeight = 2f;

    [Tooltip("Radius of circle around player")]
    public float circleRadius = 2.5f;

    [Tooltip("How fast the eagle circles")]
    public float circleSpeed = 90f;

    [Header("Attack")]
    [Tooltip("Fireball projectile prefab")]
    public GameObject fireballPrefab;

    [Tooltip("Attacks per second")]
    public float attackRate = 1f;

    [Tooltip("How far the eagle can detect enemies")]
    public float detectionRange = 10f;

    [Tooltip("Damage per fireball")]
    public float fireballDamage = 15f;

    [Tooltip("Fireball speed")]
    public float fireballSpeed = 8f;

    [Tooltip("Enemy layers")]
    public LayerMask enemyLayers;

    [Header("Dual Barrage Settings")]
    [Tooltip("Horizontal offset for dual fireballs")]
    public float dualFireballOffset = 0.4f;

    [Tooltip("Delay between shooting both fireballs (seconds)")]
    public float dualFireballDelay = 0.1f;

    [Header("Empowered Mode")]
    [Tooltip("Visual tint when empowered")]
    public Color empoweredTint = new Color(1f, 0.5f, 0f, 1f);

    [Header("Visual")]
    [Tooltip("Should the eagle face the movement direction?")]
    public bool faceMovementDirection = true;

    [Header("Debug")]
    public bool showDebug = true;

    private float circleAngle = 0f;
    private float attackTimer = 0f;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    private bool isEmpowered = false;
    private float empoweredTimer = 0f;

    private void Awake()
    {
        if (!owner)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player) owner = player.transform;
        }

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer)
        {
            originalColor = spriteRenderer.color;
        }
    }

    private void OnEnable()
    {
        if (showDebug) Debug.Log("[FireEagle] Summoned! Circling and attacking enemies.");
    }

    private void Update()
    {
        if (!owner) return;

        CircleAroundOwner();
        UpdateAttack();
        UpdateEmpoweredMode();
    }

    private void CircleAroundOwner()
    {
        circleAngle += circleSpeed * Time.deltaTime;
        if (circleAngle >= 360f) circleAngle -= 360f;

        float radians = circleAngle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(radians) * circleRadius, hoverHeight + Mathf.Sin(radians) * 0.3f, 0f);
        Vector3 targetPosition = owner.position + offset;

        Vector3 previousPosition = transform.position;
        transform.position = Vector3.Lerp(transform.position, targetPosition, 10f * Time.deltaTime);

        if (faceMovementDirection && spriteRenderer)
        {
            Vector3 moveDirection = transform.position - previousPosition;
            if (moveDirection.x < 0)
            {
                spriteRenderer.flipX = true;
            }
            else if (moveDirection.x > 0)
            {
                spriteRenderer.flipX = false;
            }
        }
    }

    private void UpdateAttack()
    {
        attackTimer -= Time.deltaTime;

        if (attackTimer <= 0f)
        {
            if (isEmpowered)
            {
                ShootDualFireballs();
            }
            else
            {
                Transform target = FindClosestEnemy();
                if (target != null)
                {
                    ShootFireball(target, transform.position);
                }
            }

            attackTimer = 1f / attackRate;
        }
    }

    private void UpdateEmpoweredMode()
    {
        if (isEmpowered)
        {
            empoweredTimer -= Time.deltaTime;

            if (empoweredTimer <= 0f)
            {
                DeactivateEmpoweredMode();
            }
        }
    }

    private Transform FindClosestEnemy()
    {
        Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, detectionRange, enemyLayers);

        Transform closest = null;
        float closestDistance = float.MaxValue;

        foreach (var enemy in enemies)
        {
            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = enemy.transform;
            }
        }

        return closest;
    }

    private Transform FindSecondClosestEnemy(Transform firstTarget)
    {
        Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, detectionRange, enemyLayers);

        Transform secondClosest = null;
        float secondClosestDistance = float.MaxValue;

        foreach (var enemy in enemies)
        {
            if (enemy.transform == firstTarget) continue;

            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            if (distance < secondClosestDistance)
            {
                secondClosestDistance = distance;
                secondClosest = enemy.transform;
            }
        }

        return secondClosest;
    }

    private void ShootDualFireballs()
    {
        Transform target1 = FindClosestEnemy();
        if (target1 == null) return;

        Transform target2 = FindSecondClosestEnemy(target1);

        Vector3 leftOffset = transform.up * dualFireballOffset;
        Vector3 rightOffset = -transform.up * dualFireballOffset;

        Vector3 leftSpawnPos = transform.position + leftOffset;
        Vector3 rightSpawnPos = transform.position + rightOffset;

        ShootFireball(target1, leftSpawnPos);

        if (dualFireballDelay > 0f)
        {
            if (target2 != null)
            {
                StartCoroutine(DelayedFireball(target2, rightSpawnPos, dualFireballDelay));
            }
            else
            {
                StartCoroutine(DelayedFireball(target1, rightSpawnPos, dualFireballDelay));
            }
        }
        else
        {
            if (target2 != null)
            {
                ShootFireball(target2, rightSpawnPos);
            }
            else
            {
                ShootFireball(target1, rightSpawnPos);
            }
        }

        if (showDebug) Debug.Log($"[FireEagle] Dual Barrage! Target 1: {target1.name}, Target 2: {(target2 ? target2.name : target1.name)}");
    }

    private System.Collections.IEnumerator DelayedFireball(Transform target, Vector3 spawnPos, float delay)
    {
        yield return new WaitForSeconds(delay);
        ShootFireball(target, spawnPos);
    }

    private void ShootFireball(Transform target, Vector3 spawnPosition)
    {
        if (!fireballPrefab)
        {
            if (showDebug) Debug.LogWarning("[FireEagle] No fireball prefab assigned!");
            return;
        }

        GameObject fireball = Instantiate(fireballPrefab, spawnPosition, Quaternion.identity);

        Vector2 direction = (target.position - spawnPosition).normalized;

        var rb = fireball.GetComponent<Rigidbody2D>();
        if (rb)
        {
            rb.linearVelocity = direction * fireballSpeed;
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        fireball.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        var projectile = fireball.GetComponent<FireEagleFireball>();
        if (projectile)
        {
            projectile.damage = fireballDamage;
            projectile.enemyLayers = enemyLayers;
        }
    }

    public void ActivateEmpoweredMode(float duration)
    {
        isEmpowered = true;
        empoweredTimer = duration;

        if (spriteRenderer)
        {
            spriteRenderer.color = empoweredTint;
        }

        if (showDebug) Debug.Log($"[FireEagle] Empowered mode activated for {duration}s! Shooting 2 fireballs!");
    }

    private void DeactivateEmpoweredMode()
    {
        isEmpowered = false;

        if (spriteRenderer)
        {
            spriteRenderer.color = originalColor;
        }

        if (showDebug) Debug.Log("[FireEagle] Empowered mode ended.");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        if (owner)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(owner.position, circleRadius);
        }

        Gizmos.color = Color.cyan;
        Vector3 leftOffset = transform.up * dualFireballOffset;
        Vector3 rightOffset = -transform.up * dualFireballOffset;
        Gizmos.DrawWireSphere(transform.position + leftOffset, 0.15f);
        Gizmos.DrawWireSphere(transform.position + rightOffset, 0.15f);
    }
}
