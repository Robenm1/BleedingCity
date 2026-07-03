using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class WillOTheWispEffect : MonoBehaviour, IActiveCardEffect
{
    [Header("Wisp Gain")]
    [Tooltip("How many enemy kills are needed to gain 1 wisp.")]
    public int killsPerWisp = 3;

    [Tooltip("Maximum wisps Pyro can hold at the same time.")]
    public int maxWisps = 3;

    [Header("Wisp Damage")]
    [Tooltip("Damage dealt as percent of the target's max HP. 0.05 = 5%.")]
    public float targetMaxHpDamagePercent = 0.05f;

    [Tooltip("Enemy layers the wisps can target.")]
    public LayerMask enemyLayers;

    [Tooltip("How far the card searches for the nearest enemy.")]
    public float targetSearchRadius = 12f;

    [Header("Projectile")]
    [Tooltip("How fast fired wisps move toward the target.")]
    public float projectileSpeed = 14f;

    [Tooltip("How close the wisp must get before dealing damage.")]
    public float hitRadius = 0.2f;

    [Tooltip("How long a fired wisp can exist before being destroyed.")]
    public float projectileLifetime = 3f;

    [Tooltip("Delay between each fired wisp. Makes them fly one after another.")]
    public float fireInterval = 0.12f;

    [Tooltip("Small side/back offset between fired wisps so they do not stack perfectly.")]
    public float launchSpread = 0.18f;

    [Header("Visuals")]
    [Tooltip("Sprites used for the stored wisps. Assign your 3 different wisp sprites here.")]
    public Sprite[] wispSprites = new Sprite[3];

    [Tooltip("How far wisps can float away from Pyro.")]
    public float floatRadius = 0.75f;

    [Tooltip("How fast wisps drift around Pyro.")]
    public float floatSpeed = 2f;

    [Tooltip("How much each wisp bobs up and down.")]
    public float bobAmount = 0.12f;

    [Tooltip("How fast each wisp bobs.")]
    public float bobSpeed = 3f;

    [Tooltip("How long it takes a newly gained wisp to fade in.")]
    public float fadeInDuration = 0.35f;

    [Tooltip("Sorting order used by stored and fired wisp sprites.")]
    public int sortingOrder = 5;

    [Header("Debug")]
    public bool showDebug = true;

    private bool _registered;

    private int _killCounter;
    private int _currentWisps;

    private Coroutine _fireRoutine;

    private readonly List<GameObject> _storedWispObjects = new List<GameObject>();
    private readonly List<Vector2> _wispBaseOffsets = new List<Vector2>();
    private readonly List<float> _wispSeeds = new List<float>();

    private void OnEnable()
    {
        if (_registered) return;

        EnemyHealth.OnAnyEnemyDied += OnAnyEnemyDied;
        _registered = true;
    }

    private void OnDisable()
    {
        if (_registered)
            EnemyHealth.OnAnyEnemyDied -= OnAnyEnemyDied;

        if (_fireRoutine != null)
        {
            StopCoroutine(_fireRoutine);
            _fireRoutine = null;
        }

        ClearStoredWispVisuals();

        _killCounter = 0;
        _currentWisps = 0;
        _registered = false;
    }

    private void Update()
    {
        UpdateStoredWispVisuals();
    }

    // Called by ActiveCardInputRouter.
    public void Activate()
    {
        FireWisps();
    }

    private void OnAnyEnemyDied(EnemyHealth enemy)
    {
        if (enemy == null) return;
        if (_currentWisps >= Mathf.Max(1, maxWisps)) return;

        _killCounter++;

        if (_killCounter >= Mathf.Max(1, killsPerWisp))
        {
            _killCounter = 0;
            AddWisp();
        }
    }

    private void AddWisp()
    {
        int cappedMax = Mathf.Max(1, maxWisps);
        if (_currentWisps >= cappedMax) return;

        _currentWisps++;
        CreateStoredWispVisual(_currentWisps - 1);

        if (showDebug)
            Debug.Log($"[WillOTheWispEffect] Gained wisp. Current wisps: {_currentWisps}/{cappedMax}");
    }

    private void FireWisps()
    {
        if (_fireRoutine != null)
        {
            if (showDebug)
                Debug.Log("[WillOTheWispEffect] Already firing wisps.");

            return;
        }

        if (_currentWisps <= 0)
        {
            if (showDebug)
                Debug.Log("[WillOTheWispEffect] No wisps to fire.");

            return;
        }

        EnemyHealth target = FindNearestEnemy();
        if (target == null)
        {
            if (showDebug)
                Debug.Log("[WillOTheWispEffect] No enemy found to target.");

            return;
        }

        _fireRoutine = StartCoroutine(FireWispsOneByOne(target));
    }

    private IEnumerator FireWispsOneByOne(EnemyHealth firstTarget)
    {
        int wispsToFire = _currentWisps;

        Sprite[] spritesToFire = new Sprite[wispsToFire];
        for (int i = 0; i < wispsToFire; i++)
            spritesToFire[i] = GetSpriteForIndex(i);

        _currentWisps = 0;
        _killCounter = 0;
        ClearStoredWispVisuals();

        if (showDebug)
            Debug.Log($"[WillOTheWispEffect] Firing {wispsToFire} wisps one by one.");

        for (int i = 0; i < wispsToFire; i++)
        {
            EnemyHealth target = firstTarget != null ? firstTarget : FindNearestEnemy();

            if (target == null)
            {
                if (showDebug)
                    Debug.Log("[WillOTheWispEffect] No target left while firing wisps.");

                break;
            }

            SpawnProjectile(spritesToFire[i], target, i, wispsToFire);

            if (i < wispsToFire - 1)
                yield return new WaitForSeconds(Mathf.Max(0f, fireInterval));
        }

        _fireRoutine = null;
    }

    private EnemyHealth FindNearestEnemy()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            Mathf.Max(0.1f, targetSearchRadius),
            enemyLayers
        );

        EnemyHealth nearest = null;
        float bestDistSqr = float.MaxValue;

        foreach (var hit in hits)
        {
            if (hit == null) continue;

            var enemy = hit.GetComponent<EnemyHealth>();
            if (enemy == null) enemy = hit.GetComponentInParent<EnemyHealth>();
            if (enemy == null) continue;

            float distSqr = ((Vector2)enemy.transform.position - (Vector2)transform.position).sqrMagnitude;

            if (distSqr < bestDistSqr)
            {
                bestDistSqr = distSqr;
                nearest = enemy;
            }
        }

        return nearest;
    }

    private void SpawnProjectile(Sprite sprite, EnemyHealth target, int index, int total)
    {
        GameObject obj = new GameObject("Will-o'-the-wisp Projectile");

        Vector3 spawnOffset = GetLaunchOffset(index, total);
        obj.transform.position = transform.position + spawnOffset;

        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = sortingOrder;

        var projectile = obj.AddComponent<WillOTheWispProjectile>();
        projectile.target = target;
        projectile.enemyLayers = enemyLayers;
        projectile.targetMaxHpDamagePercent = Mathf.Max(0f, targetMaxHpDamagePercent);
        projectile.speed = Mathf.Max(0.1f, projectileSpeed);
        projectile.hitRadius = Mathf.Max(0.01f, hitRadius);
        projectile.lifetime = Mathf.Max(0.1f, projectileLifetime);
        projectile.searchRadius = Mathf.Max(0.1f, targetSearchRadius);
        projectile.showDebug = showDebug;
    }

    private Vector3 GetLaunchOffset(int index, int total)
    {
        if (total <= 1) return Vector3.zero;

        float center = (total - 1) * 0.5f;
        float sideOffset = (index - center) * Mathf.Max(0f, launchSpread);
        float backOffset = -0.15f * index;

        return new Vector3(sideOffset, backOffset, 0f);
    }

    private void CreateStoredWispVisual(int index)
    {
        GameObject obj = new GameObject($"Stored Wisp {index + 1}");
        obj.transform.SetParent(transform);
        obj.transform.localPosition = Vector3.zero;

        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = GetSpriteForIndex(index);
        sr.sortingOrder = sortingOrder;

        Color color = sr.color;
        color.a = 0f;
        sr.color = color;

        var fade = obj.AddComponent<WispFadeIn>();
        fade.fadeDuration = Mathf.Max(0.01f, fadeInDuration);

        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float radius = Random.Range(0.25f, Mathf.Max(0.25f, floatRadius));

        Vector2 baseOffset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

        _storedWispObjects.Add(obj);
        _wispBaseOffsets.Add(baseOffset);
        _wispSeeds.Add(Random.Range(0f, 100f));
    }

    private void UpdateStoredWispVisuals()
    {
        if (_storedWispObjects.Count <= 0) return;

        float radius = Mathf.Max(0f, floatRadius);
        float speed = Mathf.Max(0f, floatSpeed);

        for (int i = 0; i < _storedWispObjects.Count; i++)
        {
            if (_storedWispObjects[i] == null) continue;

            float seed = i < _wispSeeds.Count ? _wispSeeds[i] : 0f;
            Vector2 baseOffset = i < _wispBaseOffsets.Count ? _wispBaseOffsets[i] : Vector2.zero;

            float t = Time.time * speed + seed;

            Vector2 drift = new Vector2(
                Mathf.Sin(t * 1.13f),
                Mathf.Cos(t * 0.91f)
            ) * radius * 0.25f;

            float bob = Mathf.Sin(Time.time * Mathf.Max(0f, bobSpeed) + seed) * Mathf.Max(0f, bobAmount);

            Vector2 finalOffset = baseOffset + drift;
            finalOffset = Vector2.ClampMagnitude(finalOffset, radius);

            _storedWispObjects[i].transform.localPosition = new Vector3(finalOffset.x, finalOffset.y + bob, 0f);
        }
    }

    private Sprite GetSpriteForIndex(int index)
    {
        if (wispSprites == null || wispSprites.Length == 0) return null;

        int safeIndex = Mathf.Clamp(index, 0, wispSprites.Length - 1);
        return wispSprites[safeIndex];
    }

    private void ClearStoredWispVisuals()
    {
        for (int i = _storedWispObjects.Count - 1; i >= 0; i--)
        {
            if (_storedWispObjects[i] != null)
                Destroy(_storedWispObjects[i]);
        }

        _storedWispObjects.Clear();
        _wispBaseOffsets.Clear();
        _wispSeeds.Clear();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.45f, 0.8f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.1f, targetSearchRadius));

        Gizmos.color = new Color(0.8f, 0.95f, 1f, 0.45f);
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0f, floatRadius));
    }
#endif
}