using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyHealth))]
public class DeathTouchDot : MonoBehaviour
{
    [Header("Runtime DoT")]
    [Tooltip("Remaining total damage waiting to be dealt.")]
    public float remainingDamage;

    [Tooltip("How often damage ticks are applied.")]
    public float tickInterval = 0.25f;

    private EnemyHealth _enemy;
    private float _tickTimer;
    private float _damagePerTick;

    private void Awake()
    {
        _enemy = GetComponent<EnemyHealth>();
    }

    private void Update()
    {
        if (!_enemy || remainingDamage <= 0f)
        {
            Destroy(this);
            return;
        }

        _tickTimer -= Time.deltaTime;
        if (_tickTimer > 0f) return;

        _tickTimer = Mathf.Max(0.05f, tickInterval);

        float tickDamage = Mathf.Min(_damagePerTick, remainingDamage);
        remainingDamage -= tickDamage;

        _enemy.TakeDamageDirect(tickDamage);
    }

    public void AddDamage(float totalDamage, float duration, float interval)
    {
        totalDamage = Mathf.Max(0f, totalDamage);
        duration = Mathf.Max(0.05f, duration);
        tickInterval = Mathf.Max(0.05f, interval);

        remainingDamage += totalDamage;

        int tickCount = Mathf.Max(1, Mathf.CeilToInt(duration / tickInterval));
        _damagePerTick = Mathf.Max(_damagePerTick, totalDamage / tickCount);

        _tickTimer = Mathf.Min(_tickTimer, tickInterval);
    }
}