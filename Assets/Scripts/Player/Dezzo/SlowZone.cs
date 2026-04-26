using UnityEngine;
using System.Collections.Generic;

public class SlowZone : MonoBehaviour
{
    public static event System.Action<SlowZone> OnZoneSpawned;
    public static event System.Action<SlowZone> OnZoneDestroyed;

    public Transform owner;
    public float duration = 5f;
    public float radius = 5f;
    public float slowPercent = 0.5f;
    public LayerMask enemyLayers;
    public bool isActive { get; private set; }

    [Header("Visuals")]
    public SpriteRenderer zoneSprite;
    public Color startColor = new Color(1f, 0.5f, 0f, 0.5f);
    public Color endColor = new Color(1f, 0.5f, 0f, 0f);

    private float timer;
    private HashSet<EnemyFollow> affectedEnemies = new HashSet<EnemyFollow>();
    private bool initialized = false;

    public void Init(float r, float slowFactor, float dur, LayerMask layers, bool showCircle)
    {
        radius = r;
        slowPercent = slowFactor;
        duration = dur;
        enemyLayers = layers;
        initialized = true;

        if (showCircle && zoneSprite == null)
        {
            GameObject spriteGO = new GameObject("ZoneVisual");
            spriteGO.transform.SetParent(transform, false);
            zoneSprite = spriteGO.AddComponent<SpriteRenderer>();
            zoneSprite.sprite = CreateCircleSprite();
            zoneSprite.color = startColor;
        }

        if (zoneSprite)
        {
            float scale = radius * 2f;
            zoneSprite.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    private void Start()
    {
        if (!initialized)
        {
            initialized = true;
        }

        timer = duration;
        isActive = true;

        if (zoneSprite)
        {
            zoneSprite.color = startColor;
            float scale = radius * 2f;
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        OnZoneSpawned?.Invoke(this);
    }

    private void Update()
    {
        timer -= Time.deltaTime;

        if (zoneSprite)
        {
            float normalizedTime = 1f - (timer / duration);
            zoneSprite.color = Color.Lerp(startColor, endColor, normalizedTime);
        }

        ApplySlowToEnemies();

        if (timer <= 0f)
        {
            RemoveSlowFromAll();
            isActive = false;
            Destroy(gameObject);
        }
    }

    private void ApplySlowToEnemies()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, enemyLayers);

        HashSet<EnemyFollow> currentEnemies = new HashSet<EnemyFollow>();

        foreach (var hit in hits)
        {
            if (!hit) continue;

            var enemyFollow = hit.GetComponent<EnemyFollow>();
            if (enemyFollow)
            {
                currentEnemies.Add(enemyFollow);

                if (!affectedEnemies.Contains(enemyFollow))
                {
                    enemyFollow.ApplySlow(slowPercent);
                    affectedEnemies.Add(enemyFollow);
                }
            }
        }

        List<EnemyFollow> toRemove = new List<EnemyFollow>();
        foreach (var enemy in affectedEnemies)
        {
            if (!currentEnemies.Contains(enemy))
            {
                toRemove.Add(enemy);
            }
        }

        foreach (var enemy in toRemove)
        {
            if (enemy) enemy.RemoveSlow(slowPercent);
            affectedEnemies.Remove(enemy);
        }
    }

    private void RemoveSlowFromAll()
    {
        foreach (var enemy in affectedEnemies)
        {
            if (enemy) enemy.RemoveSlow(slowPercent);
        }
        affectedEnemies.Clear();
    }

    private void OnDestroy()
    {
        RemoveSlowFromAll();
        isActive = false;
        OnZoneDestroyed?.Invoke(this);
    }

    private Sprite CreateCircleSprite()
    {
        int resolution = 64;
        Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[resolution * resolution];

        Vector2 center = new Vector2(resolution / 2f, resolution / 2f);
        float maxDist = resolution / 2f;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = dist < maxDist ? 0.3f : 0f;
                pixels[y * resolution + x] = new Color(1f, 0.5f, 0f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), resolution);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, radius);
    }
}
