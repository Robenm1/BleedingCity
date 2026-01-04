using UnityEngine;

public class FireSpirit : MonoBehaviour
{
    [Header("Follow Behavior")]
    [Tooltip("The player to follow")]
    public Transform owner;

    [Tooltip("Offset from player (relative position)")]
    public Vector3 followOffset = new Vector3(1.5f, 0.5f, 0f);

    [Tooltip("How fast the spirit follows")]
    public float followSpeed = 5f;

    [Tooltip("How close before it stops moving")]
    public float followStopDistance = 0.1f;

    [Header("Idle Animation")]
    [Tooltip("How much to bob up and down")]
    public float bobAmount = 0.2f;

    [Tooltip("How fast to bob")]
    public float bobSpeed = 2f;

    [Header("Healing")]
    [Tooltip("HP healed per second when active")]
    public float healPerSecond = 2.5f;

    [Tooltip("Seconds without damage before healing starts")]
    public float healDelayAfterDamage = 5f;

    [Tooltip("Stop healing if HP is at or above this percent (0.6 = 60%)")]
    [Range(0f, 1f)]
    public float healStopThreshold = 0.6f;

    [Header("Debug")]
    public bool showDebug = true;

    private PlayerHealth playerHealth;
    private PlayerStats playerStats;
    private float timeSinceLastDamage;
    private bool isHealing;
    private Vector3 baseFollowPosition;
    private float bobTimer;
    private SummonEvolutionTracker evolutionTracker;

    private void Awake()
    {
        if (!owner)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player) owner = player.transform;
        }

        if (owner)
        {
            playerHealth = owner.GetComponent<PlayerHealth>();
            playerStats = owner.GetComponent<PlayerStats>();
            evolutionTracker = owner.GetComponent<SummonEvolutionTracker>();
        }
    }

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDamaged += OnPlayerDamaged;
        }

        timeSinceLastDamage = 0f;
        isHealing = false;

        if (showDebug) Debug.Log("[FireSpirit] Summoned! Will heal after 5s of no damage.");
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDamaged -= OnPlayerDamaged;
        }
    }

    private void Update()
    {
        if (!owner) return;

        FollowOwner();
        UpdateHealing();
    }

    private void FollowOwner()
    {
        baseFollowPosition = owner.position + followOffset;

        bobTimer += Time.deltaTime * bobSpeed;
        float bobOffset = Mathf.Sin(bobTimer) * bobAmount;
        Vector3 targetPosition = baseFollowPosition + Vector3.up * bobOffset;

        float distance = Vector3.Distance(transform.position, targetPosition);

        if (distance > followStopDistance)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
        }
    }

    private void UpdateHealing()
    {
        if (playerStats == null) return;

        float healthPercent = playerHealth != null ? playerHealth.GetHealthNormalized() :
                               (playerStats.currentHealth / Mathf.Max(1f, playerStats.maxHealth));

        if (healthPercent >= healStopThreshold)
        {
            if (isHealing)
            {
                isHealing = false;
                if (showDebug) Debug.Log($"[FireSpirit] Stopped healing - HP at {healthPercent * 100:F0}% (threshold: {healStopThreshold * 100:F0}%)");
            }
            return;
        }

        timeSinceLastDamage += Time.deltaTime;

        if (timeSinceLastDamage >= healDelayAfterDamage)
        {
            if (!isHealing)
            {
                isHealing = true;
                if (showDebug) Debug.Log("[FireSpirit] Started healing Pyro!");
            }

            if (playerStats.currentHealth < playerStats.maxHealth)
            {
                float healAmount = healPerSecond * Time.deltaTime;
                float oldHealth = playerStats.currentHealth;

                if (playerHealth != null)
                {
                    playerHealth.Heal(healAmount);
                }
                else
                {
                    playerStats.currentHealth = Mathf.Min(playerStats.currentHealth + healAmount, playerStats.maxHealth);
                }

                float actualHealed = playerStats.currentHealth - oldHealth;

                if (evolutionTracker != null && actualHealed > 0f)
                {
                    evolutionTracker.OnHealingDone(actualHealed);
                }

                float newPercent = playerStats.currentHealth / Mathf.Max(1f, playerStats.maxHealth);
                if (newPercent >= healStopThreshold)
                {
                    isHealing = false;
                    if (showDebug) Debug.Log($"[FireSpirit] Reached healing threshold ({healStopThreshold * 100:F0}%)");
                }
            }
        }
        else
        {
            if (isHealing)
            {
                isHealing = false;
                if (showDebug) Debug.Log("[FireSpirit] Stopped healing (took damage)");
            }
        }
    }

    private void OnPlayerDamaged(float damage)
    {
        timeSinceLastDamage = 0f;
        isHealing = false;
    }

    public bool IsCurrentlyHealing()
    {
        return isHealing;
    }
}
