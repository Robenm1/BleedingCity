using UnityEngine;

public class SummonEvolutionTracker : MonoBehaviour
{
    [Header("Current Summon State")]
    public int currentLevel = 0;
    public int loopCount = 0;

    [Header("Level 1: Fire Spirit → Eagle")]
    public float healingRequired = 60f;
    private float totalHealingDone = 0f;

    [Header("Level 2: Fire Eagle → Dog")]
    public int killsRequired = 50;
    private int totalKills = 0;

    [Header("Level 3: Fire Dog → Golem")]
    public float distanceRequired = 500f;
    private float totalDistance = 0f;
    private Vector3 lastPosition;

    // Base thresholds stored once so hard-mode scaling doesn't compound.
    private float baseHealingRequired;
    private int baseKillsRequired;
    private float baseDistanceRequired;

    [Header("Level 4: Fire Golem → Dragon")]
    [Tooltip("The Golem doesn't auto-evolve. It triggers the dragon, then loops back to Spirit when killed.")]
    public bool golemLoopsOnDeath = true;

    [Header("Dragon")]
    [Tooltip("Dragon prefab spawned when the Golem dies.")]
    public GameObject fireDragonPrefab;

    [Header("Hard Mode Evolution Multipliers (applied after loop 1)")]
    [Tooltip("Multiplier applied to healingRequired after the first loop.")]
    public float hardHealingMultiplier = 2f;

    [Tooltip("Multiplier applied to killsRequired after the first loop.")]
    public float hardKillsMultiplier = 1.5f;

    [Tooltip("Multiplier applied to distanceRequired after the first loop.")]
    public float hardDistanceMultiplier = 1.5f;

    [Header("Summon Prefabs")]
    public GameObject fireSpiritPrefab;
    public GameObject fireEaglePrefab;
    public GameObject fireDogPrefab;
    public GameObject fireGolemPrefab;

    [Header("Debug")]
    public bool showDebug = true;

    private GameObject currentSummon;

    private void Start()
    {
        // Cache base thresholds before any hard-mode scaling is applied.
        baseHealingRequired = healingRequired;
        baseKillsRequired = killsRequired;
        baseDistanceRequired = distanceRequired;

        lastPosition = transform.position;
        SpawnFireSpirit();
    }

    private void Update()
    {
        if (currentLevel == 3)
        {
            TrackDistance();
        }
    }

    private void OnEnable()
    {
        EnemyHealth.OnAnyEnemyDied += OnEnemyKilled;
    }

    private void OnDisable()
    {
        EnemyHealth.OnAnyEnemyDied -= OnEnemyKilled;
    }

    public GameObject GetCurrentSummon()
    {
        return currentSummon;
    }

    public void SpawnFireSpirit()
    {
        if (currentSummon != null)
        {
            Destroy(currentSummon);
        }

        if (fireSpiritPrefab)
        {
            currentSummon = Instantiate(fireSpiritPrefab, transform.position, Quaternion.identity);
            var spirit = currentSummon.GetComponent<FireSpirit>();
            if (spirit) spirit.owner = transform;

            currentLevel = 1;
            totalHealingDone = 0f;

            if (showDebug) Debug.Log($"[SummonEvolution] Fire Spirit spawned! (Loop: {loopCount}) Need {healingRequired} HP healed to evolve.");
        }
    }

    public void OnHealingDone(float amount)
    {
        if (currentLevel != 1) return;

        totalHealingDone += amount;

        if (showDebug && totalHealingDone % 10 < amount)
        {
            Debug.Log($"[SummonEvolution] Healing progress: {totalHealingDone:F1}/{healingRequired} HP");
        }

        if (totalHealingDone >= healingRequired)
        {
            EvolveToEagle();
        }
    }

    private void EvolveToEagle()
    {
        if (showDebug) Debug.Log("[SummonEvolution] Fire Spirit evolving to Fire Eagle!");

        if (currentSummon != null)
        {
            Destroy(currentSummon);
        }

        if (fireEaglePrefab)
        {
            currentSummon = Instantiate(fireEaglePrefab, transform.position, Quaternion.identity);
            var eagle = currentSummon.GetComponent<FireEagle>();
            if (eagle) eagle.owner = transform;

            currentLevel = 2;
            totalKills = 0;

            if (showDebug) Debug.Log($"[SummonEvolution] Fire Eagle spawned! Need {killsRequired} kills to evolve.");
        }
        else
        {
            currentLevel = 2;
        }
    }

    private void OnEnemyKilled(EnemyHealth enemyHealth)
    {
        if (currentLevel != 2) return;

        totalKills++;

        if (showDebug && totalKills % 10 == 0)
        {
            Debug.Log($"[SummonEvolution] Kill progress: {totalKills}/{killsRequired}");
        }

        if (totalKills >= killsRequired)
        {
            EvolveToDog();
        }
    }

    private void EvolveToDog()
    {
        if (showDebug) Debug.Log("[SummonEvolution] Fire Eagle evolving to Fire Dog!");

        if (currentSummon != null)
        {
            Destroy(currentSummon);
        }

        if (fireDogPrefab)
        {
            currentSummon = Instantiate(fireDogPrefab, transform.position, Quaternion.identity);
            var dog = currentSummon.GetComponent<FireDog>();
            if (dog) dog.owner = transform;

            currentLevel = 3;
            totalDistance = 0f;
            lastPosition = transform.position;

            if (showDebug) Debug.Log($"[SummonEvolution] Fire Dog spawned! Need {distanceRequired} units walked to evolve.");
        }
        else
        {
            currentLevel = 3;
        }
    }

    private void TrackDistance()
    {
        Vector3 currentPosition = transform.position;
        float distanceThisFrame = Vector3.Distance(lastPosition, currentPosition);

        totalDistance += distanceThisFrame;
        lastPosition = currentPosition;

        if (showDebug && totalDistance % 50 < distanceThisFrame && distanceThisFrame > 0.01f)
        {
            Debug.Log($"[SummonEvolution] Distance progress: {totalDistance:F1}/{distanceRequired} units");
        }

        if (totalDistance >= distanceRequired)
        {
            EvolveToGolem();
        }
    }

    private void EvolveToGolem()
    {
        if (showDebug) Debug.Log("[SummonEvolution] Fire Dog evolving to Fire Golem!");

        if (currentSummon != null)
        {
            Destroy(currentSummon);
        }

        if (fireGolemPrefab)
        {
            currentSummon = Instantiate(fireGolemPrefab, transform.position, Quaternion.identity);
            var golem = currentSummon.GetComponent<FireGolem>();
            if (golem) golem.owner = transform;

            currentLevel = 4;

            if (showDebug) Debug.Log("[SummonEvolution] Fire Golem spawned! Golem will fight until destroyed.");
        }
        else
        {
            currentLevel = 4;
        }
    }

    public void OnGolemDied()
    {
        if (showDebug) Debug.Log($"[SummonEvolution] Golem destroyed! Summoning the Dragon...");

        loopCount++;
        SpawnDragon();
    }

    private void SpawnDragon()
    {
        currentLevel = 5;
        currentSummon = null;

        if (fireDragonPrefab != null)
        {
            // Dragon placement is handled internally by FireDragon.SetupFlightPath().
            Instantiate(fireDragonPrefab, Vector3.zero, Quaternion.identity);

            if (showDebug) Debug.Log("[SummonEvolution] Fire Dragon spawned! Waiting for it to finish...");

            // Return to Fire Spirit after the dragon's flight is done.
            // We read the flight duration directly from the prefab component.
            var dragonConfig = fireDragonPrefab.GetComponent<FireDragon>();
            float delay = dragonConfig != null ? dragonConfig.flightDuration : 4f;

            Invoke(nameof(ReturnToFireSpirit), delay);
        }
        else
        {
            if (showDebug) Debug.LogWarning("[SummonEvolution] No fireDragonPrefab assigned! Skipping dragon and returning to Spirit.");
            ReturnToFireSpirit();
        }
    }

    private void ReturnToFireSpirit()
    {
        ApplyHardModeMultipliers();
        SpawnFireSpirit();
    }

    /// <summary>
    /// After the first full loop, increase all evolution thresholds to make each
    /// subsequent loop harder to complete. Scales from the original base values so
    /// the multiplier never compounds across loops.
    /// </summary>
    private void ApplyHardModeMultipliers()
    {
        if (loopCount <= 1) return;

        healingRequired = Mathf.Round(baseHealingRequired * hardHealingMultiplier);
        killsRequired = Mathf.RoundToInt(baseKillsRequired * hardKillsMultiplier);
        distanceRequired = Mathf.Round(baseDistanceRequired * hardDistanceMultiplier);

        if (showDebug)
        {
            Debug.Log($"[SummonEvolution] Hard mode applied! " +
                      $"Heal: {healingRequired}, Kills: {killsRequired}, Distance: {distanceRequired}");
        }
    }

    public float GetEvolutionProgress()
    {
        switch (currentLevel)
        {
            case 1:
                return Mathf.Clamp01(totalHealingDone / healingRequired);
            case 2:
                return Mathf.Clamp01((float)totalKills / killsRequired);
            case 3:
                return Mathf.Clamp01(totalDistance / distanceRequired);
            case 4:
                return 1f;
            case 5:
                return 1f;
            default:
                return 0f;
        }
    }

    public string GetEvolutionGoal()
    {
        switch (currentLevel)
        {
            case 1:
                return $"Heal {totalHealingDone:F0}/{healingRequired} HP";
            case 2:
                return $"Kill {totalKills}/{killsRequired} enemies";
            case 3:
                return $"Walk {totalDistance:F0}/{distanceRequired} units";
            case 4:
                return $"Fire Golem (Loop {loopCount})";
            case 5:
                return "Fire Dragon!";
            default:
                return "No summon";
        }
    }
}
