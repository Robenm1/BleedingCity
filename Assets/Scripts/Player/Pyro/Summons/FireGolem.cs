using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class FireGolem : MonoBehaviour
{
    [Header("Owner")]
    public Transform owner;

    [Header("Stats")]
    public float maxHP = 500f;
    public float moveSpeed = 2f;
    public float aggroRange = 20f;

    [Header("Combat")]
    public float attackRange = 3f;
    public float attackCooldown = 2.5f;
    public float hit1Damage = 50f;
    public float hit2Damage = 50f;
    public float hit3Damage = 100f;
    public float hit1KnockbackForce = 2f;
    public float hit2KnockbackForce = 2f;
    public float hit3KnockbackForce = 3f;
    public float knockbackStunDuration = 0.2f;
    public float aoeRadius = 5f;

    [Header("Slow Zone")]
    public GameObject slowZonePrefab;
    public float slowZoneDuration = 5f;
    public float slowZoneRadius = 5f;
    public float slowPercent = 0.5f;

    [Header("Layers")]
    public LayerMask enemyLayers;
    public LayerMask coinLayers;

    [Header("Visuals")]
    public SpriteRenderer bodySprite;
    public Color damageFlashColor = Color.red;
    public float damageFlashDuration = 0.1f;

    [Header("HP Bar UI")]
    public RectTransform hpUIRoot;
    public Slider hpSlider;
    public float visibleForSecondsAfterHit = 3f;
    public Vector3 worldOffset = new Vector3(0f, 1.5f, 0f);
    public Camera uiCamera;
    [Range(0.01f, 0.3f)] public float followSmoothTime = 0.1f;

    [Header("Passive Aggro")]
    public bool drawsPassiveAggro = true;
    public float passiveAggroRadius = 15f;
    public float passiveAggroUpdateInterval = 1f;

    [Header("Player Avoidance")]
    public bool avoidBlockingPlayer = true;
    public float playerAvoidanceDistance = 3.5f;
    public float playerAvoidanceCheckInterval = 0.05f;
    public float avoidanceMoveDistance = 4f;
    public float avoidanceSpeedMultiplier = 2.5f;
    [Tooltip("Minimum player speed to trigger avoidance")]
    public float minPlayerSpeedToAvoid = 0.5f;
    [Tooltip("How directly the player must be moving towards golem (0-1, higher = more direct)")]
    [Range(0.3f, 1f)] public float playerMovementDotThreshold = 0.4f;

    [Header("Coin Avoidance")]
    public bool avoidCoins = true;
    public float coinAvoidanceRadius = 1.5f;
    public float coinRepulsionForce = 2f;

    [Header("Debug")]
    public bool showDebug = true;

    private float currentHP;
    private float attackTimer;
    private int comboStep = 0;
    private bool isAttacking;
    private Transform currentTarget;
    private Transform comboTarget;
    private Color originalColor;
    private Rigidbody2D rb;

    private float hideTimer = 0f;
    private Canvas hpCanvas;
    private RectTransform canvasRect;
    private bool isWorldSpaceCanvas;
    private Vector2 _uiVel;
    private Vector2 _lastAnchoredPos;

    private float passiveAggroTimer = 0f;
    private float playerAvoidanceTimer = 0f;
    private bool isAvoidingPlayer = false;
    private Vector2 avoidanceTargetPos;
    private float avoidanceDuration = 0f;

    private Vector2 lastOwnerPosition;
    private Vector2 ownerVelocity;

    private float fireCircleTimer = 0f;

    private void Awake()
    {
        currentHP = maxHP;
        rb = GetComponent<Rigidbody2D>();

        if (!rb)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.bodyType = RigidbodyType2D.Kinematic;

        if (bodySprite)
        {
            originalColor = bodySprite.color;
        }

        if (hpUIRoot != null)
        {
            hpCanvas = hpUIRoot.GetComponentInParent<Canvas>();
            if (hpCanvas != null)
            {
                canvasRect = hpCanvas.GetComponent<RectTransform>();
                isWorldSpaceCanvas = (hpCanvas.renderMode == RenderMode.WorldSpace);
            }
        }

        if (uiCamera == null) uiCamera = Camera.main;

        InitHPUI();
        HideHPUIImmediate();
    }

    private void Start()
    {
        if (owner)
        {
            lastOwnerPosition = owner.position;
        }
    }

    private void Update()
    {
        if (!owner)
        {
            Destroy(gameObject);
            return;
        }

        UpdateOwnerVelocity();

        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
        }

        if (fireCircleTimer > 0f)
        {
            fireCircleTimer -= Time.deltaTime;
        }

        CheckPlayerAvoidance();

        if (!isAttacking)
        {
            if (isAvoidingPlayer)
            {
                PerformAvoidanceMovement();
            }
            else
            {
                FindAndMoveToTarget();
            }
        }

        UpdateHPBarVisibility();
        UpdatePassiveAggro();
    }

    private void LateUpdate()
    {
        UpdateHPBarPosition();
    }

    private void UpdateOwnerVelocity()
    {
        if (!owner) return;

        Vector2 currentOwnerPos = owner.position;
        ownerVelocity = (currentOwnerPos - lastOwnerPosition) / Time.deltaTime;
        lastOwnerPosition = currentOwnerPos;
    }

    private void CheckPlayerAvoidance()
    {
        if (!avoidBlockingPlayer || isAttacking || !owner) return;

        playerAvoidanceTimer -= Time.deltaTime;
        if (playerAvoidanceTimer > 0f) return;

        playerAvoidanceTimer = playerAvoidanceCheckInterval;

        float distToPlayer = Vector2.Distance(transform.position, owner.position);

        if (distToPlayer > playerAvoidanceDistance)
        {
            if (isAvoidingPlayer && distToPlayer > playerAvoidanceDistance * 1.5f)
            {
                isAvoidingPlayer = false;
                if (showDebug) Debug.Log("[FireGolem] Player moved away - resuming normal behavior");
            }
            return;
        }

        float playerSpeed = ownerVelocity.magnitude;

        if (playerSpeed < minPlayerSpeedToAvoid)
        {
            if (isAvoidingPlayer)
            {
                isAvoidingPlayer = false;
                if (showDebug) Debug.Log("[FireGolem] Player stopped - resuming normal behavior");
            }
            return;
        }

        Vector2 playerMovementDir = ownerVelocity.normalized;
        Vector2 playerToGolem = ((Vector2)transform.position - (Vector2)owner.position).normalized;

        float movementDot = Vector2.Dot(playerMovementDir, playerToGolem);

        if (showDebug && Time.frameCount % 30 == 0)
        {
            Debug.Log($"[FireGolem] Distance: {distToPlayer:F1}, Speed: {playerSpeed:F1}, Dot: {movementDot:F2} (threshold: {playerMovementDotThreshold})");
        }

        if (movementDot > playerMovementDotThreshold)
        {
            Vector2 awayFromPlayer = playerToGolem;

            Vector2 perpendicularDir = new Vector2(-playerMovementDir.y, playerMovementDir.x);

            float sidePreference = Vector2.Dot(playerToGolem, perpendicularDir);
            if (sidePreference < 0f)
            {
                perpendicularDir = -perpendicularDir;
            }

            Vector2 escapeDirection = (awayFromPlayer + perpendicularDir).normalized;
            Vector2 awayPos = (Vector2)transform.position + escapeDirection * avoidanceMoveDistance;

            if (!isAvoidingPlayer || Vector2.Distance(transform.position, avoidanceTargetPos) < 0.5f)
            {
                isAvoidingPlayer = true;
                avoidanceTargetPos = awayPos;
                avoidanceDuration = 0.8f;

                if (showDebug) Debug.Log($"[FireGolem] Player approaching (speed: {playerSpeed:F1}, dot: {movementDot:F2}) - DODGING!");
            }
        }
        else
        {
            if (isAvoidingPlayer && movementDot < 0f)
            {
                isAvoidingPlayer = false;
                if (showDebug) Debug.Log("[FireGolem] Player changed direction - resuming normal behavior");
            }
        }
    }


    private void PerformAvoidanceMovement()
    {
        float distToAvoidTarget = Vector2.Distance(transform.position, avoidanceTargetPos);

        if (distToAvoidTarget < 0.2f)
        {
            rb.linearVelocity = Vector2.zero;
            avoidanceDuration -= Time.deltaTime;
            if (avoidanceDuration <= 0f)
            {
                isAvoidingPlayer = false;
                if (showDebug) Debug.Log("[FireGolem] Avoidance complete");
            }
        }
        else
        {
            Vector2 direction = (avoidanceTargetPos - (Vector2)transform.position).normalized;
            rb.linearVelocity = direction * moveSpeed * avoidanceSpeedMultiplier;
        }
    }

    private void FindAndMoveToTarget()
    {
        Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, aggroRange, enemyLayers);

        if (enemies.Length == 0)
        {
            FollowOwner();
            currentTarget = null;
            return;
        }

        Transform closest = null;
        float closestDist = float.MaxValue;

        foreach (var enemy in enemies)
        {
            if (!enemy) continue;
            float dist = Vector2.Distance(transform.position, enemy.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = enemy.transform;
            }
        }

        if (closest)
        {
            currentTarget = closest;
            float distToTarget = Vector2.Distance(transform.position, closest.position);

            if (distToTarget <= attackRange)
            {
                if (attackTimer <= 0f && !isAttacking)
                {
                    StartCoroutine(PerformComboAttack(closest));
                }
                else
                {
                    rb.linearVelocity = Vector2.zero;
                }
            }
            else
            {
                MoveTowardsTarget(closest.position);
            }
        }
    }

    private void FollowOwner()
    {
        if (!owner) return;

        float distToOwner = Vector2.Distance(transform.position, owner.position);
        if (distToOwner > 3f)
        {
            MoveTowardsTarget(owner.position);
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void MoveTowardsTarget(Vector3 targetPos)
    {
        Vector2 direction = ((Vector2)targetPos - (Vector2)transform.position).normalized;
        Vector2 desiredVelocity = direction * moveSpeed;

        if (avoidCoins && coinLayers != 0)
        {
            Collider2D[] nearbyCoins = Physics2D.OverlapCircleAll(transform.position, coinAvoidanceRadius, coinLayers);
            if (nearbyCoins.Length > 0)
            {
                Vector2 repulsion = Vector2.zero;
                foreach (var coin in nearbyCoins)
                {
                    if (!coin) continue;
                    Vector2 awayFromCoin = (Vector2)transform.position - (Vector2)coin.transform.position;
                    float dist = awayFromCoin.magnitude;
                    if (dist > 0.01f)
                    {
                        repulsion += awayFromCoin.normalized * (coinRepulsionForce / dist);
                    }
                }
                desiredVelocity += repulsion;
            }
        }

        rb.linearVelocity = desiredVelocity;
    }

    private void UpdatePassiveAggro()
    {
        if (!drawsPassiveAggro) return;

        passiveAggroTimer -= Time.deltaTime;
        if (passiveAggroTimer > 0f) return;

        passiveAggroTimer = passiveAggroUpdateInterval;

        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(transform.position, passiveAggroRadius, enemyLayers);

        foreach (var enemy in nearbyEnemies)
        {
            if (!enemy) continue;

            var enemyFollow = enemy.GetComponent<EnemyFollow>();
            if (enemyFollow && enemyFollow.playerTarget != transform)
            {
                float distToGolem = Vector2.Distance(transform.position, enemy.transform.position);
                float distToPlayer = owner ? Vector2.Distance(owner.position, enemy.transform.position) : float.MaxValue;

                if (distToGolem < distToPlayer * 0.7f)
                {
                    enemyFollow.playerTarget = transform;
                }
            }
        }
    }

    private IEnumerator PerformComboAttack(Transform target)
    {
        isAttacking = true;
        comboStep = 0;
        comboTarget = target;
        rb.linearVelocity = Vector2.zero;
        isAvoidingPlayer = false;

        yield return new WaitForSeconds(0.3f);

        if (!IsTargetValid(comboTarget))
        {
            EndCombo();
            yield break;
        }

        PerformHit1();
        yield return new WaitForSeconds(0.5f);

        if (!IsTargetValid(comboTarget))
        {
            EndCombo();
            yield break;
        }

        PerformHit2();
        yield return new WaitForSeconds(0.5f);

        if (!IsTargetValid(comboTarget))
        {
            EndCombo();
            yield break;
        }

        PerformHit3();

        EndCombo();
    }

    private void EndCombo()
    {
        attackTimer = attackCooldown;
        isAttacking = false;
        comboStep = 0;
        comboTarget = null;
    }

    private bool IsTargetValid(Transform target)
    {
        if (target == null) return false;

        var enemyHealth = target.GetComponent<EnemyHealth>();
        return enemyHealth != null;
    }

    private void PerformHit1()
    {
        comboStep = 1;

        if (comboTarget == null) return;

        var enemyHealth = comboTarget.GetComponent<EnemyHealth>();
        if (enemyHealth)
        {
            enemyHealth.TakeDamage(hit1Damage);
        }

        ApplyKnockback(comboTarget, hit1KnockbackForce, knockbackStunDuration);

        if (showDebug) Debug.Log($"[FireGolem] Hit 1! Damage: {hit1Damage} to {comboTarget.name}");
    }

    private void PerformHit2()
    {
        comboStep = 2;

        if (comboTarget == null) return;

        var enemyHealth = comboTarget.GetComponent<EnemyHealth>();
        if (enemyHealth)
        {
            enemyHealth.TakeDamage(hit2Damage);
        }

        ApplyKnockback(comboTarget, hit2KnockbackForce, knockbackStunDuration);

        if (showDebug) Debug.Log($"[FireGolem] Hit 2! Damage: {hit2Damage} to {comboTarget.name}");
    }

    private void PerformHit3()
    {
        comboStep = 3;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, aoeRadius, enemyLayers);

        foreach (var hit in hits)
        {
            if (!hit) continue;

            var enemyHealth = hit.GetComponent<EnemyHealth>();
            if (enemyHealth)
            {
                enemyHealth.TakeDamage(hit3Damage);
            }

            ApplyKnockback(hit.transform, hit3KnockbackForce, knockbackStunDuration);
        }

        SpawnSlowZone();

        if (showDebug) Debug.Log($"[FireGolem] Hit 3 (AOE)! Damage: {hit3Damage}, Radius: {aoeRadius}");
    }

    private void ApplyKnockback(Transform target, float force, float stunDuration)
    {
        if (!target) return;

        Vector2 knockbackDir = (target.position - transform.position).normalized;

        var enemyRb = target.GetComponent<Rigidbody2D>();
        if (enemyRb)
        {
            enemyRb.linearVelocity = knockbackDir * force;
        }

        var enemyFollow = target.GetComponent<EnemyFollow>();
        if (enemyFollow)
        {
            StartCoroutine(StunEnemy(enemyFollow, stunDuration));
        }
    }

    private IEnumerator StunEnemy(EnemyFollow enemy, float duration)
    {
        if (!enemy) yield break;

        enemy.enabled = false;
        yield return new WaitForSeconds(duration);

        if (enemy)
        {
            enemy.enabled = true;
        }
    }

    private void SpawnSlowZone()
    {
        if (!slowZonePrefab) return;

        GameObject zone = Instantiate(slowZonePrefab, transform.position, Quaternion.identity);
        var slowZone = zone.GetComponent<GolemSlowZone>();
        if (slowZone)
        {
            slowZone.duration = slowZoneDuration;
            slowZone.radius = slowZoneRadius;
            slowZone.slowPercent = slowPercent;
            slowZone.enemyLayers = enemyLayers;
        }
    }

    public void ActivateFireCircle(float duration, float radius, float shieldAmount, float dotDamage, GameObject circlePrefab)
    {
        if (fireCircleTimer > 0f)
        {
            if (showDebug) Debug.Log($"[FireGolem] Fire Circle on internal cooldown: {fireCircleTimer:F1}s");
            return;
        }

        if (!circlePrefab)
        {
            if (showDebug) Debug.LogError("[FireGolem] No Fire Circle prefab provided!");
            return;
        }

        GameObject circle = Instantiate(circlePrefab, transform.position, Quaternion.identity);

        var fireCircle = circle.GetComponent<GolemFireCircle>();

        if (fireCircle)
        {
            fireCircle.golem = transform;
            fireCircle.duration = duration;
            fireCircle.radius = radius;
            fireCircle.shieldAmount = shieldAmount;
            fireCircle.dotDamagePerSecond = dotDamage;
            fireCircle.enemyLayers = enemyLayers;

            if (owner)
            {
                var pyroAbility = owner.GetComponent<PyroAbility1>();
                if (pyroAbility && pyroAbility.shieldVisual)
                {
                    fireCircle.playerShieldVisual = pyroAbility.shieldVisual;
                }
            }

            if (showDebug) Debug.Log($"[FireGolem] Fire Circle spawned! Shield: {shieldAmount}, DoT: {dotDamage}/s, Radius: {radius}");
        }
        else
        {
            if (showDebug) Debug.LogError("[FireGolem] GolemFireCircle component not found on prefab!");
        }

        fireCircleTimer = 1f;

        StartCoroutine(DisableAvoidanceDuringFireCircle(duration));
    }

    private IEnumerator DisableAvoidanceDuringFireCircle(float duration)
    {
        bool wasAvoidingPlayer = avoidBlockingPlayer;
        avoidBlockingPlayer = false;
        isAvoidingPlayer = false;

        if (showDebug) Debug.Log("[FireGolem] Player avoidance DISABLED during fire circle - golem can still fight!");

        yield return new WaitForSeconds(duration);

        avoidBlockingPlayer = wasAvoidingPlayer;

        if (showDebug) Debug.Log("[FireGolem] Player avoidance RESTORED");
    }






    public void TakeDamage(float damage)
    {
        currentHP -= damage;

        if (bodySprite)
        {
            StartCoroutine(FlashDamage());
        }

        UpdateHPUI();
        ShowHPUI();

        if (showDebug) Debug.Log($"[FireGolem] Took {damage} damage. HP: {currentHP}/{maxHP}");

        if (currentHP <= 0f)
        {
            Die();
        }
    }

    private IEnumerator FlashDamage()
    {
        if (bodySprite)
        {
            bodySprite.color = damageFlashColor;
            yield return new WaitForSeconds(damageFlashDuration);
            bodySprite.color = originalColor;
        }
    }

    private void Die()
    {
        if (showDebug) Debug.Log("[FireGolem] Golem destroyed!");

        var tracker = owner?.GetComponent<SummonEvolutionTracker>();
        if (tracker)
        {
            tracker.OnGolemDied();
        }

        if (hpUIRoot != null) Destroy(hpUIRoot.gameObject);
        Destroy(gameObject);
    }

    public void ActivateTauntMode(float duration)
    {
        StartCoroutine(TauntCoroutine(duration));
    }

    private IEnumerator TauntCoroutine(float duration)
    {
        if (showDebug) Debug.Log($"[FireGolem] TAUNT MODE for {duration}s - All enemies attack me!");

        float elapsed = 0f;
        while (elapsed < duration)
        {
            Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, aggroRange * 2f, enemyLayers);

            foreach (var enemy in enemies)
            {
                if (!enemy) continue;

                var enemyFollow = enemy.GetComponent<EnemyFollow>();
                if (enemyFollow)
                {
                    enemyFollow.playerTarget = transform;
                }
            }

            elapsed += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }

        if (showDebug) Debug.Log("[FireGolem] Taunt mode ended - Resetting enemy targets!");

        Collider2D[] allEnemies = Physics2D.OverlapCircleAll(transform.position, aggroRange * 2f, enemyLayers);
        foreach (var enemy in allEnemies)
        {
            if (!enemy) continue;

            var enemyFollow = enemy.GetComponent<EnemyFollow>();
            if (enemyFollow && owner)
            {
                enemyFollow.playerTarget = owner;
            }
        }
    }

    public float GetHealthPercent()
    {
        return maxHP > 0f ? currentHP / maxHP : 0f;
    }

    private void InitHPUI()
    {
        if (hpSlider != null)
        {
            hpSlider.minValue = 0f;
            hpSlider.maxValue = 1f;
            hpSlider.value = 1f;
            hpSlider.interactable = false;
        }
    }

    private void UpdateHPUI()
    {
        if (hpSlider != null)
        {
            float t = (maxHP > 0f) ? (currentHP / maxHP) : 0f;
            hpSlider.value = Mathf.Clamp01(t);
        }
    }

    private void ShowHPUI()
    {
        if (hpUIRoot == null) return;

        if (!hpUIRoot.gameObject.activeSelf)
            hpUIRoot.gameObject.SetActive(true);

        _lastAnchoredPos = hpUIRoot.anchoredPosition;
        _uiVel = Vector2.zero;

        hideTimer = visibleForSecondsAfterHit;
    }

    private void HideHPUIImmediate()
    {
        if (hpUIRoot == null) return;
        if (hpUIRoot.gameObject.activeSelf)
            hpUIRoot.gameObject.SetActive(false);
    }

    private void UpdateHPBarVisibility()
    {
        if (hpUIRoot != null && hpUIRoot.gameObject.activeSelf)
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0f && Mathf.Approximately(currentHP, maxHP))
            {
                HideHPUIImmediate();
            }
        }
    }

    private void UpdateHPBarPosition()
    {
        if (hpUIRoot == null || hpCanvas == null) return;

        Vector3 worldPos = transform.position + worldOffset;

        if (isWorldSpaceCanvas)
        {
            hpUIRoot.position = Vector3.Lerp(
                hpUIRoot.position,
                worldPos,
                1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.0001f, followSmoothTime))
            );
            hpUIRoot.rotation = Quaternion.identity;
            return;
        }

        Camera cam = (hpCanvas.renderMode == RenderMode.ScreenSpaceCamera) ? uiCamera : null;

        Vector2 screenPoint = (cam != null)
            ? (Vector2)cam.WorldToScreenPoint(worldPos)
            : (Camera.main != null ? (Vector2)Camera.main.WorldToScreenPoint(worldPos) : (Vector2)worldPos);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, cam, out var localPoint))
        {
            Vector2 target = localPoint;
            Vector2 smoothed = Vector2.SmoothDamp(_lastAnchoredPos, target, ref _uiVel, followSmoothTime);
            hpUIRoot.anchoredPosition = smoothed;
            _lastAnchoredPos = smoothed;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRange);

        if (drawsPassiveAggro)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, passiveAggroRadius);
        }

        if (avoidBlockingPlayer && owner)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, playerAvoidanceDistance);
        }

        if (avoidCoins)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, coinAvoidanceRadius);
        }

        if (comboStep == 3)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, aoeRadius);
        }

        if (isAvoidingPlayer)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, avoidanceTargetPos);
            Gizmos.DrawWireSphere(avoidanceTargetPos, 0.5f);
        }
    }
}
