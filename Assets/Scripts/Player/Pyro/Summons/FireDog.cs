using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FireDog : MonoBehaviour
{
    public enum DogState { Following, SeekingCoin, SeekingEnemy, Biting, Retreating }

    [Header("Owner")]
    public Transform owner;

    [Header("Coin Collection (Priority 1)")]
    [Tooltip("How far the dog can detect coins")]
    public float coinDetectionRange = 10f;

    [Tooltip("Distance to collect a coin")]
    public float coinCollectRange = 0.5f;

    [Tooltip("Tag for coins/collectables")]
    public string coinTag = "Collectable";

    [Header("Combat (Priority 2)")]
    [Tooltip("How far the dog can detect enemies")]
    public float enemyDetectionRange = 8f;

    [Tooltip("Bite damage")]
    public float biteDamage = 25f;

    [Tooltip("Bite range")]
    public float biteRange = 1.2f;

    [Tooltip("Bite cooldown")]
    public float biteCooldown = 1.5f;

    [Tooltip("Enemy layers")]
    public LayerMask enemyLayers;

    [Header("Movement")]
    [Tooltip("Base move speed")]
    public float moveSpeed = 7f;

    [Tooltip("How smooth the turning is (0 = instant, 1 = no turn)")]
    [Range(0f, 1f)]
    public float turnSmooth = 0.15f;

    [Header("Post-Bite Retreat")]
    [Tooltip("How long to retreat after biting")]
    public float retreatDuration = 0.6f;

    [Header("Idle Wandering")]
    [Tooltip("Radius around player to wander")]
    public float wanderRadius = 3f;

    [Tooltip("Min distance from player while wandering")]
    public float wanderMinDistance = 1.5f;

    [Tooltip("How often to pick a new wander point (seconds)")]
    public float wanderInterval = 2f;

    [Tooltip("Speed while wandering (multiplier of moveSpeed)")]
    [Range(0.3f, 1f)]
    public float wanderSpeedMultiplier = 0.6f;

    [Header("Rotation")]
    [Tooltip("Max rotation speed (degrees/sec)")]
    public float maxAngularSpeed = 220f;

    [Tooltip("Minimum angle delta to rotate")]
    public float minAngleDelta = 2f;

    [Header("Alpha Strike")]
    public float alphaStrikeInvulnerabilityDuration = 1f;

    [Header("Debug")]
    public bool showDebug = true;

    private DogState currentState = DogState.Following;
    private Transform currentTarget;
    private Vector2 velocity;
    private float biteTimer = 0f;
    private float retreatTimer = 0f;
    private float currentAngle = 0f;
    private SpriteRenderer spriteRenderer;
    private SpriteRenderer dogSpriteRenderer;
    private Collider2D dogCollider;
    private SummonEvolutionTracker evolutionTracker;

    private Vector2 wanderPoint;
    private float wanderTimer = 0f;
    private bool hasWanderPoint = false;

    private bool isAlphaStriking = false;

    private void Awake()
    {
        if (!owner)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player) owner = player.transform;
        }

        if (owner)
        {
            evolutionTracker = owner.GetComponent<SummonEvolutionTracker>();
        }

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        dogSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        dogCollider = GetComponent<Collider2D>();
        currentAngle = transform.eulerAngles.z;
    }

    private void OnEnable()
    {
        if (showDebug) Debug.Log("[FireDog] Summoned! Collecting coins and hunting enemies!");
        PickNewWanderPoint();
    }

    private void Update()
    {
        if (isAlphaStriking) return;

        if (!owner) return;

        if (biteTimer > 0f) biteTimer -= Time.deltaTime;
        wanderTimer -= Time.deltaTime;

        switch (currentState)
        {
            case DogState.Following:
                TickFollowing();
                break;
            case DogState.SeekingCoin:
                TickSeekingCoin();
                break;
            case DogState.SeekingEnemy:
                TickSeekingEnemy();
                break;
            case DogState.Biting:
                TickBiting();
                break;
            case DogState.Retreating:
                TickRetreating();
                break;
        }

        FaceTowardTarget();
    }

    private void TickFollowing()
    {
        Transform coin = FindClosestCoin();
        if (coin != null)
        {
            currentTarget = coin;
            currentState = DogState.SeekingCoin;
            hasWanderPoint = false;
            return;
        }

        Transform enemy = FindClosestEnemy();
        if (enemy != null)
        {
            currentTarget = enemy;
            currentState = DogState.SeekingEnemy;
            hasWanderPoint = false;
            return;
        }

        if (wanderTimer <= 0f || !hasWanderPoint)
        {
            PickNewWanderPoint();
        }

        Vector2 toWander = wanderPoint - (Vector2)transform.position;
        float distanceToWander = toWander.magnitude;

        if (distanceToWander > 0.3f)
        {
            currentTarget = null;
            Vector2 desired = toWander.normalized * (moveSpeed * wanderSpeedMultiplier);
            velocity = Vector2.Lerp(velocity, desired, 1f - turnSmooth);
            transform.position += (Vector3)(velocity * Time.deltaTime);

            Vector2 lookDir = toWander.normalized;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                float targetAngle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
                float delta = Mathf.DeltaAngle(currentAngle, targetAngle);
                float maxStep = maxAngularSpeed * Time.deltaTime * 0.5f;
                delta = Mathf.Clamp(delta, -maxStep, maxStep);
                currentAngle += delta;
                transform.rotation = Quaternion.Euler(0f, 0f, currentAngle);
            }
        }
        else
        {
            PickNewWanderPoint();
        }
    }

    private void PickNewWanderPoint()
    {
        if (!owner) return;

        Vector2 randomDir = Random.insideUnitCircle.normalized;
        float randomDist = Random.Range(wanderMinDistance, wanderRadius);
        wanderPoint = (Vector2)owner.position + randomDir * randomDist;

        wanderTimer = wanderInterval;
        hasWanderPoint = true;
    }

    private void TickSeekingCoin()
    {
        if (currentTarget == null)
        {
            currentState = DogState.Following;
            return;
        }

        Vector2 toTarget = (Vector2)(currentTarget.position - transform.position);
        float distance = toTarget.magnitude;

        if (distance <= coinCollectRange)
        {
            CollectCoin(currentTarget.gameObject);
            currentTarget = null;
            currentState = DogState.Following;
            return;
        }

        Transform newCoin = FindClosestCoin();
        if (newCoin != null && newCoin != currentTarget)
        {
            currentTarget = newCoin;
            toTarget = (Vector2)(currentTarget.position - transform.position);
        }
        else if (newCoin == null)
        {
            currentTarget = null;
            currentState = DogState.Following;
            return;
        }

        Vector2 desired = toTarget.normalized * moveSpeed;
        velocity = Vector2.Lerp(velocity, desired, 1f - turnSmooth);
        transform.position += (Vector3)(velocity * Time.deltaTime);
    }

    private void TickSeekingEnemy()
    {
        Transform coin = FindClosestCoin();
        if (coin != null)
        {
            currentTarget = coin;
            currentState = DogState.SeekingCoin;
            return;
        }

        if (currentTarget == null)
        {
            currentState = DogState.Following;
            return;
        }

        Vector2 toTarget = (Vector2)(currentTarget.position - transform.position);
        float distance = toTarget.magnitude;

        if (distance <= biteRange && biteTimer <= 0f)
        {
            currentState = DogState.Biting;
            return;
        }

        Vector2 desired = toTarget.normalized * moveSpeed;
        velocity = Vector2.Lerp(velocity, desired, 1f - turnSmooth);
        transform.position += (Vector3)(velocity * Time.deltaTime);
    }

    private void TickBiting()
    {
        if (currentTarget == null)
        {
            currentState = DogState.Following;
            return;
        }

        var enemyHealth = currentTarget.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            float damage = biteDamage;
            enemyHealth.TakeDamage(damage);

            if (evolutionTracker != null)
            {
                evolutionTracker.OnDamageDealt(damage);
            }

            if (showDebug) Debug.Log($"[FireDog] Bit {currentTarget.name} for {damage} damage!");
        }

        BeginRetreat();
    }

    private void BeginRetreat()
    {
        retreatTimer = retreatDuration;
        biteTimer = biteCooldown;
        currentState = DogState.Retreating;

        Vector2 away;
        if (currentTarget != null)
        {
            away = ((Vector2)transform.position - (Vector2)currentTarget.position).normalized;
        }
        else if (owner != null)
        {
            away = ((Vector2)transform.position - (Vector2)owner.position).normalized;
        }
        else
        {
            away = Random.insideUnitCircle.normalized;
        }

        Vector2 tangent = new Vector2(-away.y, away.x) * 0.7f;
        Vector2 retreatDir = (away + tangent).normalized;

        velocity = retreatDir * moveSpeed;
    }

    private void TickRetreating()
    {
        retreatTimer -= Time.deltaTime;

        transform.position += (Vector3)(velocity * Time.deltaTime);
        velocity = Vector2.Lerp(velocity, Vector2.zero, Time.deltaTime * 2f);

        if (retreatTimer <= 0f)
        {
            currentState = DogState.Following;
            currentTarget = null;
        }
    }

    private void CollectCoin(GameObject coinObject)
    {
        if (!coinObject) return;

        var coin = coinObject.GetComponent<XPCoin>();
        if (coin != null && owner != null)
        {
            var playerXP = owner.GetComponent<PlayerXP>();
            if (playerXP != null)
            {
                playerXP.AddCoinPickup(coin.xpValue);

                if (showDebug) Debug.Log($"[FireDog] Collected coin worth {coin.xpValue} XP!");

                Destroy(coinObject);
            }
        }
    }

    private Transform FindClosestCoin()
    {
        GameObject[] coins = GameObject.FindGameObjectsWithTag(coinTag);

        Transform closest = null;
        float closestDistance = coinDetectionRange;

        foreach (var coin in coins)
        {
            if (!coin) continue;

            float distance = Vector2.Distance(transform.position, coin.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = coin.transform;
            }
        }

        return closest;
    }

    private Transform FindClosestEnemy()
    {
        Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, enemyDetectionRange, enemyLayers);

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

    private void FaceTowardTarget()
    {
        if (currentTarget == null) return;

        Vector2 toTarget = (Vector2)(currentTarget.position - transform.position);
        if (toTarget.sqrMagnitude < 0.0001f) return;

        float targetAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
        float delta = Mathf.DeltaAngle(currentAngle, targetAngle);

        if (Mathf.Abs(delta) < minAngleDelta) return;

        float maxStep = maxAngularSpeed * Time.deltaTime;
        delta = Mathf.Clamp(delta, -maxStep, maxStep);
        currentAngle += delta;
        transform.rotation = Quaternion.Euler(0f, 0f, currentAngle);
    }

    public void PerformAlphaStrike(int slashCount, float damagePerSlash, float range, float interval, GameObject slashEffectPrefab)
    {
        if (isAlphaStriking) return;

        StartCoroutine(AlphaStrikeCoroutine(slashCount, damagePerSlash, range, interval, slashEffectPrefab));
    }

    private IEnumerator AlphaStrikeCoroutine(int slashCount, float damagePerSlash, float range, float interval, GameObject slashEffectPrefab)
    {
        isAlphaStriking = true;

        if (dogSpriteRenderer) dogSpriteRenderer.enabled = false;
        if (dogCollider) dogCollider.enabled = false;

        Collider2D[] enemiesInRange = Physics2D.OverlapCircleAll(transform.position, range, enemyLayers);
        List<Transform> allEnemies = new List<Transform>();

        foreach (var enemy in enemiesInRange)
        {
            if (enemy != null && enemy.GetComponent<EnemyHealth>() != null)
            {
                allEnemies.Add(enemy.transform);
            }
        }

        if (allEnemies.Count == 0)
        {
            if (showDebug) Debug.Log("[FireDog] No enemies found for Alpha Strike!");
            EndAlphaStrike();
            yield break;
        }

        List<Transform> availableTargets = new List<Transform>(allEnemies);

        for (int i = 0; i < slashCount; i++)
        {
            if (availableTargets.Count == 0)
            {
                availableTargets = new List<Transform>(allEnemies);
                availableTargets.RemoveAll(e => e == null);

                if (availableTargets.Count == 0)
                {
                    if (showDebug) Debug.Log("[FireDog] All enemies eliminated during Alpha Strike!");
                    break;
                }
            }

            Transform target = availableTargets[Random.Range(0, availableTargets.Count)];

            availableTargets.Remove(target);

            if (target != null)
            {
                transform.position = target.position;

                var enemyHealth = target.GetComponent<EnemyHealth>();
                if (enemyHealth != null)
                {
                    enemyHealth.TakeDamage(damagePerSlash);

                    if (evolutionTracker != null)
                    {
                        evolutionTracker.OnDamageDealt(damagePerSlash);
                    }

                    if (showDebug) Debug.Log($"[FireDog] Alpha Strike slash {i + 1}/{slashCount} hit {target.name}!");
                }

                if (slashEffectPrefab != null)
                {
                    Instantiate(slashEffectPrefab, target.position, Quaternion.identity);
                }

                if (target == null || enemyHealth == null)
                {
                    allEnemies.Remove(target);
                }
            }

            yield return new WaitForSeconds(interval);
        }

        EndAlphaStrike();
    }


    private void EndAlphaStrike()
    {
        isAlphaStriking = false;

        if (dogSpriteRenderer) dogSpriteRenderer.enabled = true;
        if (dogCollider) dogCollider.enabled = true;

        if (showDebug) Debug.Log("[FireDog] Alpha Strike complete!");
    }

    private void OnDrawGizmosSelected()
    {
        if (owner)
        {
            Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.2f);
            Gizmos.DrawWireSphere(owner.position, wanderRadius);

            Gizmos.color = new Color(0.3f, 0.3f, 0.8f, 0.3f);
            Gizmos.DrawWireSphere(owner.position, wanderMinDistance);
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, coinDetectionRange);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, coinCollectRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, enemyDetectionRange);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, biteRange);

        if (owner)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, owner.position);
        }

        if (hasWanderPoint && currentState == DogState.Following)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(wanderPoint, 0.2f);
            Gizmos.DrawLine(transform.position, wanderPoint);
        }

        if (currentTarget)
        {
            Gizmos.color = currentState == DogState.SeekingCoin ? Color.yellow : Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }
    }
}
