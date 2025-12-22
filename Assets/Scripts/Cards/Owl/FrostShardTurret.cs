using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FrostShardTurret : MonoBehaviour
{
    [Header("Visual")]
    public Color turretTint = new(0.7f, 0.9f, 1f, 1f);
    public float tintLerp = 18f;

    [Header("Volley")]
    public int shardsToFire = 6;
    public float fireInterval = 0.12f;
    public float spreadAngle = 20f;  // +/- from aim
    public float range = 8f;
    public LayerMask enemyLayers;

    [Header("Projectile Setup")]
    public FrostShardProjectile shardPrefab;
    public float shardSpeed = 18f;
    public float shardLifetime = 2.5f;

    [Header("Damage/Frost from Player")]
    public float damageFromPlayer = 10f;
    public bool frostOnHit = true;
    public float frostSlow = 0.5f;
    public float frostDur = 3f;
    public Sprite frostIcon;
    public Vector2 frostIconPivot = new(0f, 0.85f);
    public Vector2 frostIconSize = new(0.35f, 0.35f);
    [Range(0f, 1f)] public float propagateOnKillChance = 0f;

    private SpriteRenderer _sr;
    private Color _base;
    private bool _started;

    public void InitFromEnemy(SpriteRenderer enemySr)
    {
        _sr = gameObject.AddComponent<SpriteRenderer>();
        if (enemySr)
        {
            _sr.sprite = enemySr.sprite;
            _sr.flipX = enemySr.flipX;
            _sr.flipY = enemySr.flipY;
            _sr.sortingLayerID = enemySr.sortingLayerID;
            _sr.sortingOrder = enemySr.sortingOrder;
        }
        _base = _sr.color;
        _sr.color = turretTint;
    }

    private void Update()
    {
        if (_sr && _sr.color != turretTint)
            _sr.color = Color.Lerp(_sr.color, turretTint, 1f - Mathf.Exp(-tintLerp * Time.deltaTime));
    }

    public void BeginFiring(System.Action onComplete)
    {
        if (_started) return;
        _started = true;
        StartCoroutine(FireRoutine(onComplete));
    }

    private IEnumerator FireRoutine(System.Action onComplete)
    {
        int count = Mathf.Max(1, shardsToFire);

        for (int i = 0; i < count; i++)
        {
            Vector2 aim = PickAimDir();
            float offset = Random.Range(-spreadAngle, +spreadAngle);
            aim = Quaternion.Euler(0f, 0f, offset) * aim;

            FireOne(aim);

            yield return new WaitForSeconds(fireInterval);
        }

        onComplete?.Invoke();
        Destroy(gameObject);
    }

    private Vector2 PickAimDir()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range, enemyLayers);
        Transform best = null;
        float bestD = float.PositiveInfinity;
        for (int i = 0; i < hits.Length; i++)
        {
            if (!hits[i]) continue;
            float d = ((Vector2)hits[i].transform.position - (Vector2)transform.position).sqrMagnitude;
            if (d < bestD)
            {
                bestD = d;
                best = hits[i].transform;
            }
        }

        if (best == null) return Vector2.right; // fallback
        return ((Vector2)best.position - (Vector2)transform.position).normalized;
    }

    private void FireOne(Vector2 dir)
    {
        if (!shardPrefab) return;

        var proj = Instantiate(shardPrefab, transform.position, Quaternion.identity);
        proj.enemyLayers = enemyLayers;
        proj.speed = shardSpeed;
        proj.lifetime = shardLifetime;
        proj.damage = damageFromPlayer;
        proj.frostOnHit = frostOnHit;
        proj.frostSlowFactor = frostSlow;
        proj.frostDuration = frostDur;
        proj.frostIcon = frostIcon;
        proj.iconPivot = frostIconPivot;
        proj.iconSize = frostIconSize;
        proj.propagateFrostOnKillChance = propagateOnKillChance;

        proj.Launch(dir);
    }
}
