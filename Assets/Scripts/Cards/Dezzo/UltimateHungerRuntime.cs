// Assets/Scripts/Cards/Dezzo/UltimateHungerRuntime.cs
using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class UltimateHungerRuntime : MonoBehaviour
{
    [Header("Hit → Mark")]
    public int hitsToMark = 3;
    public float hitCounterDecayDelay = 4f;
    public float biteMarkDuration = 6f;

    [Header("Frenzy vs Bite-Marked")]
    public float frenzyMoveSpeedMul = 1.25f;
    public float frenzyAttackDelayMul = 0.80f;
    public float frenzyDamageMul = 1.15f;

    [Header("Execute")]
    public float executeThreshold = 0.10f;

    [Header("Visual")]
    public Sprite biteMarkSprite;
    public Color biteMarkTint = new Color(1f, 0.15f, 0.15f, 1f);
    public Vector2 biteMarkOffset = new Vector2(0f, 0.1f);
    public int sortingOrder = 300;

    private PlayerStats _stats;
    private Transform _self;
    private float _scanTimer;

    // enemy → (count, lastHitTime)
    private readonly Dictionary<EnemyHealth, (int count, float last)> _counters = new();

    private void Awake()
    {
        _self = transform;
        _stats = GetComponent<PlayerStats>();
    }

    private void OnEnable() { DezzoShark.OnSharkHit += OnSharkHit; }
    private void OnDisable() { DezzoShark.OnSharkHit -= OnSharkHit; _counters.Clear(); }

    private void Update()
    {
        if (_counters.Count == 0) return;
        float now = Time.time;
        var keys = new List<EnemyHealth>(_counters.Keys);
        foreach (var eh in keys)
        {
            if (!eh) { _counters.Remove(eh); continue; }
            var data = _counters[eh];
            if (now - data.last >= hitCounterDecayDelay)
                _counters.Remove(eh);
        }
    }

    private void OnSharkHit(EnemyHealth target, Transform shark, float dealtDamage)
    {
        if (!target || dealtDamage <= 0f) return;

        float now = Time.time;
        if (_counters.TryGetValue(target, out var data))
            _counters[target] = (data.count + 1, now);
        else
            _counters[target] = (1, now);

        if (_counters[target].count >= hitsToMark)
        {
            _counters[target] = (0, now);

            var bm = target.GetComponent<EnemyBiteMark>();
            if (!bm) bm = target.gameObject.AddComponent<EnemyBiteMark>();

            bm.Apply(
                duration: biteMarkDuration,
                frenzyMoveMul: frenzyMoveSpeedMul,
                frenzyAtkDelayMul: frenzyAttackDelayMul,
                frenzyDmgMul: frenzyDamageMul,
                execThreshold: executeThreshold,
                sigilSprite: biteMarkSprite,
                tint: biteMarkTint,
                offset: biteMarkOffset,
                sortingOrder: sortingOrder
            );
        }
    }
}
