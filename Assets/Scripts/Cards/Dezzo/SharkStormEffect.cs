using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SharkStormEffect : MonoBehaviour
{
    [Header("Starting Setup")]
    public int startSharks = 1;
    [Range(0.05f, 2f)] public float damageScaleOnStart = 0.7f;

    [Header("Progression")]
    public int killsPerNewShark = 5;
    public int maxSharks = 0; // 0 = unlimited

    private DezzoSharkManager _mgr;
    private int _killCounter = 0;
    private bool _initialized = false;

    private void OnEnable()
    {
        EnemyHealth.OnAnyEnemyDied += HandleEnemyDied;
    }

    private void OnDisable()
    {
        EnemyHealth.OnAnyEnemyDied -= HandleEnemyDied;
    }

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        _mgr = GetComponent<DezzoSharkManager>();
        if (_mgr == null)
        {
            Debug.LogWarning("[SharkStormEffect] No DezzoSharkManager on this character. Effect skipped.", this);
            return;
        }

        // Apply weaker damage using manager’s overdrive multiplier (clean, non-invasive)
        _mgr.overdriveActive = true;
        _mgr.overdriveDamageMul = Mathf.Clamp(damageScaleOnStart, 0.05f, 2f);
        _mgr.overdriveSpeedMul = 1f;

        // Start with exactly 'startSharks'
        EnsureExactSharkCount(startSharks);
    }

    private void HandleEnemyDied(EnemyHealth eh)
    {
        if (_mgr == null) return;

        _killCounter++;
        if (_killCounter >= Mathf.Max(1, killsPerNewShark))
        {
            _killCounter = 0;
            TrySpawnOneMore();
        }
    }

    private void TrySpawnOneMore()
    {
        if (_mgr == null || _mgr.sharkPrefab == null) return;

        var list = new List<DezzoShark>();
        if (_mgr.sharks != null)
        {
            foreach (var s in _mgr.sharks)
                if (s != null) list.Add(s);
        }

        if (maxSharks > 0 && list.Count >= maxSharks)
            return;

        float radius = Mathf.Max(0.7f, _mgr.idleOrbitRadius);
        int newIndex = list.Count;
        float angle = (Mathf.PI * 2f) * (newIndex / Mathf.Max(1f, (float)(newIndex + 1)));
        Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;

        var spawned = Instantiate(_mgr.sharkPrefab, _mgr.transform.position + offset, Quaternion.identity, _mgr.transform);
        spawned.gameObject.SetActive(true);
        spawned.transform.localPosition = offset;

        spawned.owner = _mgr;
        spawned.index = newIndex;

        list.Add(spawned);
        _mgr.sharks = list.ToArray();
    }

    private void EnsureExactSharkCount(int target)
    {
        target = Mathf.Max(0, target);

        var list = new List<DezzoShark>();
        if (_mgr.sharks != null)
        {
            foreach (var s in _mgr.sharks)
                if (s != null) list.Add(s);
        }

        while (list.Count > target)
        {
            var last = list[list.Count - 1];
            list.RemoveAt(list.Count - 1);
            if (last) Destroy(last.gameObject);
        }

        while (list.Count < target)
        {
            float radius = Mathf.Max(0.7f, _mgr.idleOrbitRadius);
            int i = list.Count;
            float angle = (Mathf.PI * 2f) * (i / Mathf.Max(1f, (float)target));
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;

            var spawned = Instantiate(_mgr.sharkPrefab, _mgr.transform.position + offset, Quaternion.identity, _mgr.transform);
            spawned.gameObject.SetActive(true);
            spawned.transform.localPosition = offset;

            spawned.owner = _mgr;
            spawned.index = i;

            list.Add(spawned);
        }

        _mgr.sharks = list.ToArray();
    }
}
