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
    public float damageRequired = 1000f;
    private float totalDamage = 0f;

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
        SpawnFireSpirit();
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

            if (showDebug) Debug.Log($"[SummonEvolution] Fire Spirit spawned! Need {healingRequired} HP healed to evolve.");
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
            totalDamage = 0f;

            if (showDebug) Debug.Log($"[SummonEvolution] Fire Dog spawned! Need {damageRequired} damage to evolve.");
        }
        else
        {
            currentLevel = 3;
        }
    }

    public void OnDamageDealt(float damage)
    {
        if (currentLevel != 3) return;

        totalDamage += damage;

        if (showDebug && totalDamage % 100 < damage)
        {
            Debug.Log($"[SummonEvolution] Damage progress: {totalDamage:F0}/{damageRequired}");
        }

        if (totalDamage >= damageRequired)
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

        currentLevel = 4;
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
                return Mathf.Clamp01(totalDamage / damageRequired);
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
                return $"Deal {totalDamage:F0}/{damageRequired} damage";
            case 4:
                return "Max evolution (Golem)";
            default:
                return "No summon";
        }
    }
}
