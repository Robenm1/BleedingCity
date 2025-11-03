using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
[ExecuteAlways]
#endif
[RequireComponent(typeof(PlayerStats))]
public class DezzoSharkManager : MonoBehaviour
{
    [Header("References")]
    public PlayerStats stats;
    public LayerMask enemyLayers;

    [Header("Shark Setup")]
    [Tooltip("Single prefab used for all sharks. Must have DezzoShark + SpriteRenderer.")]
    public DezzoShark sharkPrefab;
    [Min(1)] public int autoSpawnCount = 2;
    [Tooltip("If you place sharks manually, assign them here. If empty, we'll auto-spawn.")]
    public DezzoShark[] sharks;

    [Header("Targeting/Retarget")]
    public float retargetInterval = 0.2f;
    public bool preferMarkedTargets = true;

    [Header("Idle Orbit")]
    [Tooltip("Only used as a fallback visual spacing when very close to player.")]
    public float idleOrbitRadius = 1.8f;

    [Header("Overdrive (runtime)")]
    [Tooltip("Runtime state toggled by abilities (e.g., Dune Time).")]
    public bool overdriveActive = false;
    [Tooltip("Shark move-speed multiplier while overdrive is active.")]
    public float overdriveSpeedMul = 1f;
    [Tooltip("Shark damage multiplier while overdrive is active.")]
    public float overdriveDamageMul = 1f;

    [Header("Gizmo")]
    [Tooltip("Draw the detection ring even when not selected (Editor only).")]
    public bool alwaysDrawDetectionGizmo = true;

    private float retargetTimer = 0f;
    private readonly Dictionary<Transform, int> targetReservations = new();

    private void Awake()
    {
        if (stats == null) stats = GetComponent<PlayerStats>();
#if UNITY_EDITOR
        if (!Application.isPlaying) return;
#endif
    }

    private void Start()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) return;
#endif
        EnsureSharksPresent();
        WireOwners();
        AssignTargets();
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) return;
#endif
        retargetTimer -= Time.deltaTime;
        if (retargetTimer <= 0f)
        {
            retargetTimer = retargetInterval;
            AssignTargets();
        }
        CleanReservations();
    }

    // ====== Public stat access ======
    public float GetOverdriveSpeedMul() => overdriveActive ? Mathf.Max(0.1f, overdriveSpeedMul) : 1f;
    public float GetOverdriveDamageMul() => overdriveActive ? Mathf.Max(0.1f, overdriveDamageMul) : 1f;

    public float GetSharkDamage()
    {
        float baseDmg = stats != null ? stats.GetDamage() : 10f;
        return baseDmg * GetOverdriveDamageMul();
    }

    public float GetSharkMoveSpeed()
    {
        float baseSpeed = stats != null ? Mathf.Max(1f, stats.GetProjectileSpeed()) : 12f;
        return baseSpeed * GetOverdriveSpeedMul();
    }

    public float GetAttackDelay() => stats != null ? Mathf.Max(0.05f, stats.GetAttackDelay()) : 0.25f;

    // >>> CRITICAL: read the *getter* so buffs/multipliers apply
    public float GetDetectionRadius()
    {
        if (stats == null) stats = GetComponent<PlayerStats>();
        return stats != null ? Mathf.Max(0.1f, stats.GetAttackRange()) : 6f;
    }

    public void StartOverdrive(float duration, float speedMul, float dmgMul)
    {
        StopAllCoroutines();
        StartCoroutine(OverdriveRoutine(duration, speedMul, dmgMul));
    }

    private IEnumerator OverdriveRoutine(float duration, float speedMul, float dmgMul)
    {
        overdriveActive = true;
        overdriveSpeedMul = Mathf.Max(0.1f, speedMul);
        overdriveDamageMul = Mathf.Max(0.1f, dmgMul);
        float t = duration;
        while (t > 0f) { t -= Time.deltaTime; yield return null; }
        overdriveActive = false;
        overdriveSpeedMul = 1f;
        overdriveDamageMul = 1f;
    }

    private void EnsureSharksPresent()
    {
        bool hasSceneSharks = sharks != null && sharks.Length > 0;
        if (hasSceneSharks)
        {
            for (int i = 0; i < sharks.Length; i++)
                if (sharks[i] != null) { Debug.Log("[DezzoSharkManager] Using assigned scene sharks."); return; }
        }

        if (sharkPrefab == null) { Debug.LogError("[DezzoSharkManager] No sharkPrefab set. Cannot auto-spawn."); return; }
        if (!sharkPrefab.GetComponent<DezzoShark>()) { Debug.LogError("[DezzoSharkManager] sharkPrefab has no DezzoShark component."); return; }

        var list = new List<DezzoShark>();
        float radius = Mathf.Max(0.5f, idleOrbitRadius);
        int count = Mathf.Max(1, autoSpawnCount);

        for (int i = 0; i < count; i++)
        {
            float angle = (Mathf.PI * 2f) * (i / Mathf.Max(1f, (float)count));
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
            var spawned = Instantiate(sharkPrefab, transform.position + offset, Quaternion.identity, this.transform);
            if (spawned == null) continue;
            spawned.gameObject.SetActive(true);
            spawned.transform.localPosition = offset;
            list.Add(spawned);
        }

        sharks = list.ToArray();
        Debug.Log($"[DezzoSharkManager] Auto-spawned {sharks.Length} shark(s).");
    }

    private void WireOwners()
    {
        if (sharks == null || sharks.Length == 0) return;
        for (int i = 0; i < sharks.Length; i++)
        {
            var s = sharks[i];
            if (s == null) continue;
            s.owner = this;
            s.index = i;
        }
    }

    private void AssignTargets()
    {
        if (sharks == null || sharks.Length == 0) return;

        var candidates = FindCandidates();
        candidates.Sort((a, b) =>
        {
            bool am = IsMarked(a);
            bool bm = IsMarked(b);
            if (am != bm) return bm.CompareTo(am);
            float da = (a.position - transform.position).sqrMagnitude;
            float db = (b.position - transform.position).sqrMagnitude;
            return da.CompareTo(db);
        });

        if (candidates.Count > 1)
        {
            var used = new HashSet<Transform>();
            for (int i = 0; i < sharks.Length; i++)
            {
                var s = sharks[i];
                if (s == null) continue;
                Transform best = PickUniqueTarget(candidates, used);
                s.AssignTarget(best);
                Reserve(best, i);
                if (best != null) used.Add(best);
            }
        }
        else
        {
            Transform single = candidates.Count == 1 ? candidates[0] : null;
            if (single != null)
            {
                int chosen = ChooseAvailableSharkIndex();
                for (int i = 0; i < sharks.Length; i++)
                {
                    var s = sharks[i];
                    if (s == null) continue;
                    if (i == chosen) { s.AssignTarget(single); Reserve(single, i); }
                    else s.AssignTarget(null);
                }
            }
            else
            {
                for (int i = 0; i < sharks.Length; i++)
                    if (sharks[i] != null) sharks[i].AssignTarget(null);
            }
        }
    }

    private List<Transform> FindCandidates()
    {
        float range = GetDetectionRadius();
        var list = new List<Transform>();

        // Pull any collider in range on enemyLayers,
        // then walk up to the root that actually has EnemyHealth.
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range, enemyLayers);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null) continue;

            // IMPORTANT: EnemyHealth might be on the parent/root, not on this collider
            var eh = hits[i].GetComponentInParent<EnemyHealth>();
            if (eh == null) continue;
            if (!eh.isActiveAndEnabled || !eh.gameObject.activeInHierarchy) continue;

            // Use the EnemyHealth's transform as the target (root), not the child collider
            list.Add(eh.transform);
        }

        return list;
    }


    private Transform PickUniqueTarget(List<Transform> candidates, HashSet<Transform> used)
    {
        foreach (var t in candidates)
        {
            if (t == null || used.Contains(t)) continue;
            if (!IsReservedByOther(t, -1)) return t;
        }
        foreach (var t in candidates)
        {
            if (t == null || used.Contains(t)) continue;
            return t;
        }
        return null;
    }

    private bool IsMarked(Transform t)
    {
        if (!preferMarkedTargets || t == null) return false;
        var m = t.GetComponent<EnemyMark>();
        return m != null && m.isMarked;
    }

    private int ChooseAvailableSharkIndex()
    {
        int fallback = 0;
        float bestScore = float.NegativeInfinity;
        for (int i = 0; i < sharks.Length; i++)
        {
            var s = sharks[i];
            if (s == null) continue;
            float score = 0f;
            if (!s.IsBusy) score += 100f;
            float dist = (s.transform.position - transform.position).sqrMagnitude;
            score -= dist * 0.01f;
            if (score > bestScore) { bestScore = score; fallback = i; }
        }
        return fallback;
    }

    private void Reserve(Transform t, int sharkIndex)
    {
        if (t == null) return;
        targetReservations[t] = sharkIndex;
    }

    private bool IsReservedByOther(Transform t, int sharkIndex)
    {
        if (t == null) return false;
        if (targetReservations.TryGetValue(t, out int who))
            return who != sharkIndex;
        return false;
    }

    private void CleanReservations()
    {
        var keys = new List<Transform>(targetReservations.Keys);
        foreach (var k in keys)
            if (k == null) targetReservations.Remove(k);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!alwaysDrawDetectionGizmo) return;
        DrawDetectionGizmo();
    }

    private void OnDrawGizmosSelected()
    {
        DrawDetectionGizmo();
    }

    private void DrawDetectionGizmo()
    {
        if (stats == null) stats = GetComponent<PlayerStats>();
        if (stats == null) return;

        Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.5f); // cyan-ish
        Gizmos.DrawWireSphere(transform.position, GetDetectionRadius());
    }
#endif
}
