using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Runtime slow zone for Dezzo's Dune Time.
/// - Call Init(...) after creation.
/// - Has owner, radius, duration, optional visual.
/// - Raises spawn/despawn events so other systems (e.g., HealingDuneEffect) can track it.
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
public class SlowZone : MonoBehaviour
{
    public static event Action<SlowZone> OnZoneSpawned;
    public static event Action<SlowZone> OnZoneDestroyed;

    [Header("Owner")]
    public Transform owner;

    [Header("Params (read-only at runtime)")]
    public float radius = 4.5f;
    [Range(0.1f, 1f)] public float enemySlowFactor = 0.4f;
    public float duration = 4f;
    public LayerMask enemyLayers;
    public bool isActive = true;

    [Header("Visuals")]
    public bool showCircle = true;
    public int circleSegments = 64;
    public float circleLineWidth = 0.06f;
    public Color circleColor = new Color(0.85f, 0.75f, 0.2f, 0.25f);

    private CircleCollider2D _col;
    private LineRenderer _lr;
    private float _life;

    // Cache slowed enemies to avoid re-applying every frame (optional)
    private readonly HashSet<EnemyFollow> _slowed = new();

    public void Init(float radius, float slowFactor, float duration, LayerMask enemyLayers, bool showCircle)
    {
        this.radius = Mathf.Max(0.1f, radius);
        this.enemySlowFactor = Mathf.Clamp(slowFactor, 0.1f, 1f);
        this.duration = Mathf.Max(0.1f, duration);
        this.enemyLayers = enemyLayers;
        this.showCircle = showCircle;

        if (!_col) _col = GetComponent<CircleCollider2D>();
        _col.isTrigger = true;
        // Scale-aware radius: keep collider radius in local units
        _col.radius = this.radius;

        BuildOrUpdateVisual();

        _life = this.duration;
        isActive = true;

        OnZoneSpawned?.Invoke(this);
    }

    private void Awake()
    {
        _col = GetComponent<CircleCollider2D>();
        _col.isTrigger = true;
    }

    private void Update()
    {
        if (!isActive) return;

        _life -= Time.deltaTime;
        if (_life <= 0f)
        {
            isActive = false;
            CleanupVisual();
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        OnZoneDestroyed?.Invoke(this);
    }

    private void BuildOrUpdateVisual()
    {
        if (!showCircle)
        {
            if (_lr) _lr.enabled = false;
            return;
        }

        if (!_lr)
        {
            _lr = gameObject.AddComponent<LineRenderer>();
            _lr.useWorldSpace = false;
            _lr.alignment = LineAlignment.View;
            _lr.loop = true;
            _lr.numCapVertices = 0;
            _lr.numCornerVertices = 0;
            _lr.sortingOrder = 10; // render above ground
            _lr.material = new Material(Shader.Find("Sprites/Default"));
        }

        _lr.enabled = true;
        _lr.positionCount = Mathf.Max(16, circleSegments);
        _lr.startWidth = circleLineWidth;
        _lr.endWidth = circleLineWidth;
        _lr.startColor = circleColor;
        _lr.endColor = circleColor;

        float step = Mathf.PI * 2f / _lr.positionCount;
        for (int i = 0; i < _lr.positionCount; i++)
        {
            float a = step * i;
            _lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f));
        }
    }

    private void CleanupVisual()
    {
        if (_lr) _lr.enabled = false;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Apply a simple slow to enemies inside the zone by scaling their moveSpeed temporarily.
        if (((1 << other.gameObject.layer) & enemyLayers.value) == 0) return;

        var ef = other.GetComponent<EnemyFollow>();
        if (!ef) return;
        if (_slowed.Contains(ef)) return;

        _slowed.Add(ef);
        StartCoroutine(TempSlowCoroutine(ef));
    }

    private System.Collections.IEnumerator TempSlowCoroutine(EnemyFollow ef)
    {
        if (!ef) yield break;

        float original = ef.moveSpeed;
        float target = original * enemySlowFactor;
        ef.moveSpeed = target;

        // While the enemy stays within the zone (approx), maintain slow
        float refresh = 0.1f;
        while (isActive && ef != null)
        {
            // if enemy left the radius significantly, break early
            if (Vector2.Distance(ef.transform.position, transform.position) > radius * 1.1f) break;
            yield return new WaitForSeconds(refresh);
        }

        if (ef != null) ef.moveSpeed = original;
        _slowed.Remove(ef);
    }
}
