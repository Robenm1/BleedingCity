using UnityEngine;

[DisallowMultipleComponent]
public class FireElementDot : MonoBehaviour
{
    private EnemyHealth _enemy;
    private PlayerHealth _player;
    private GameObject _attacker;

    private float _remainingDamage;
    private float _remainingDuration;
    private float _tickInterval;
    private float _tickTimer;

    private bool _active;
    private bool _showDebug;

    private void Awake()
    {
        CacheHealthTargets();
    }

    private void Update()
    {
        if (!_active)
            return;

        CacheHealthTargets();

        if (_enemy == null && _player == null)
        {
            Destroy(this);
            return;
        }

        _remainingDuration -= Time.deltaTime;
        _tickTimer -= Time.deltaTime;

        if (_tickTimer <= 0f)
        {
            TickBurn();
            _tickTimer = Mathf.Max(0.01f, _tickInterval);
        }

        if (_remainingDuration <= 0f || _remainingDamage <= 0f)
        {
            _active = false;
            Destroy(this);
        }
    }

    public void ApplyBurn(
        GameObject attacker,
        float totalBurnDamage,
        float duration,
        float tickInterval,
        bool stackDamage,
        bool showDebug
    )
    {
        CacheHealthTargets();

        if (_enemy == null && _player == null)
            return;

        _attacker = attacker;
        _tickInterval = Mathf.Max(0.01f, tickInterval);
        _remainingDuration = Mathf.Max(0.01f, duration);
        _tickTimer = _tickInterval;
        _showDebug = showDebug;

        if (stackDamage)
            _remainingDamage += Mathf.Max(0f, totalBurnDamage);
        else
            _remainingDamage = Mathf.Max(_remainingDamage, totalBurnDamage);

        _active = _remainingDamage > 0f;

        if (_showDebug)
        {
            Debug.Log(
                $"[FireElementDot] Burn applied on {name}. " +
                $"Remaining damage: {_remainingDamage:F1}, Duration: {_remainingDuration:F1}s"
            );
        }
    }

    private void TickBurn()
    {
        if (_remainingDamage <= 0f || _remainingDuration <= 0f)
            return;

        float ticksLeft = Mathf.Max(
            1f,
            Mathf.Ceil(_remainingDuration / Mathf.Max(0.01f, _tickInterval))
        );

        float tickDamage = _remainingDamage / ticksLeft;

        _remainingDamage -= tickDamage;
        if (_remainingDamage < 0f)
            _remainingDamage = 0f;

        FireElementSO.BeginFireDotTick();

        try
        {
            if (_enemy != null)
            {
                _enemy.TakeDamageDirectFromSource(_attacker, tickDamage);
            }
            else if (_player != null)
            {
                _player.ApplyDamageDirectFromSource(_attacker, tickDamage);
            }
        }
        finally
        {
            FireElementSO.EndFireDotTick();
        }

        if (_showDebug)
            Debug.Log($"[FireElementDot] Tick damage on {name}: {tickDamage:F1}");
    }

    private void CacheHealthTargets()
    {
        if (_enemy == null)
            _enemy = GetComponent<EnemyHealth>();

        if (_player == null)
            _player = GetComponent<PlayerHealth>();
    }
}