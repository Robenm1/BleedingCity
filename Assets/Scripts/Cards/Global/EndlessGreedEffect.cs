using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerStats))]
public class EndlessGreedEffect : MonoBehaviour
{
    [Header("Endless Greed")]
    [Tooltip("Flat damage gained per coin collected. 1 = +1 damage.")]
    public float flatDamagePerStack = 1f;

    [Tooltip("Move speed gained per coin collected as percent of original move speed. 1 = +1% move speed.")]
    public float moveSpeedPercentPerStack = 1f;

    [Tooltip("If true, stacks have no limit.")]
    public bool infiniteStacks = true;

    [Tooltip("Only used if Infinite Stacks is false.")]
    public int maxStacks = 999;

    [Header("Reset")]
    [Tooltip("Lose all Endless Greed stacks when the player takes damage.")]
    public bool resetOnDamageTaken = true;

    [Header("Debug")]
    public bool showDebug = true;

    private PlayerStats _stats;
    private PlayerXP _xp;

    private int _stacks;
    private int _lastCoins;

    private float _moveSpeedReference;

    private float _appliedDamageBonus;
    private float _appliedMoveSpeedBonus;

    private float _lastHealth;
    private bool _registered;

    private void Awake()
    {
        _stats = GetComponent<PlayerStats>();

        _xp = GetComponent<PlayerXP>();
        if (_xp == null) _xp = GetComponentInChildren<PlayerXP>();
        if (_xp == null) _xp = GetComponentInParent<PlayerXP>();
    }

    private void OnEnable()
    {
        if (!_stats)
            _stats = GetComponent<PlayerStats>();

        if (!_xp)
        {
            _xp = GetComponent<PlayerXP>();
            if (_xp == null) _xp = GetComponentInChildren<PlayerXP>();
            if (_xp == null) _xp = GetComponentInParent<PlayerXP>();
        }

        if (!_stats)
        {
            Debug.LogWarning("[EndlessGreedEffect] PlayerStats not found.");
            enabled = false;
            return;
        }

        if (!_xp)
        {
            Debug.LogWarning("[EndlessGreedEffect] PlayerXP not found.");
            enabled = false;
            return;
        }

        _moveSpeedReference = Mathf.Max(0f, _stats.moveSpeed);

        _lastHealth = _stats.currentHealth;
        _lastCoins = _xp.GetCoins();

        Register();

        if (showDebug)
            Debug.Log("[EndlessGreedEffect] Activated.");
    }

    private void OnDisable()
    {
        Unregister();

        RemoveOnlyThisCardStats();

        _stacks = 0;
    }

    private void Update()
    {
        if (!_stats || !resetOnDamageTaken) return;

        if (_stats.currentHealth < _lastHealth)
        {
            ResetGreedStacks();
        }

        _lastHealth = _stats.currentHealth;
    }

    private void Register()
    {
        if (_registered || _xp == null) return;

        _xp.OnCoinsChanged += HandleCoinsChanged;
        _registered = true;
    }

    private void Unregister()
    {
        if (!_registered || _xp == null) return;

        _xp.OnCoinsChanged -= HandleCoinsChanged;
        _registered = false;
    }

    private void HandleCoinsChanged(int newCoinAmount)
    {
        int gainedCoins = newCoinAmount - _lastCoins;
        _lastCoins = newCoinAmount;

        if (gainedCoins <= 0)
            return;

        for (int i = 0; i < gainedCoins; i++)
        {
            AddGreedStack();
        }
    }

    private void AddGreedStack()
    {
        if (!_stats) return;

        if (!infiniteStacks && _stacks >= Mathf.Max(1, maxStacks))
            return;

        _stacks++;

        float damageBonus = Mathf.Max(0f, flatDamagePerStack);

        float moveSpeedBonus =
            _moveSpeedReference * (Mathf.Max(0f, moveSpeedPercentPerStack) / 100f);

        _stats.baseDamage += damageBonus;
        _stats.moveSpeed += moveSpeedBonus;

        _appliedDamageBonus += damageBonus;
        _appliedMoveSpeedBonus += moveSpeedBonus;

        if (showDebug)
        {
            Debug.Log(
                $"[EndlessGreedEffect] Stack gained: {_stacks}. " +
                $"Base Damage: {_stats.baseDamage:F1}, " +
                $"Move Speed: {_stats.moveSpeed:F2}, " +
                $"Card Damage Bonus: +{_appliedDamageBonus:F1}, " +
                $"Card MoveSpeed Bonus: +{_appliedMoveSpeedBonus:F2}"
            );
        }
    }

    private void ResetGreedStacks()
    {
        if (_stacks <= 0) return;

        if (showDebug)
            Debug.Log($"[EndlessGreedEffect] Damage taken. Lost {_stacks} stacks.");

        RemoveOnlyThisCardStats();

        _stacks = 0;
    }

    private void RemoveOnlyThisCardStats()
    {
        if (!_stats) return;

        if (_appliedDamageBonus > 0f)
        {
            _stats.baseDamage -= _appliedDamageBonus;
            _appliedDamageBonus = 0f;
        }

        if (_appliedMoveSpeedBonus > 0f)
        {
            _stats.moveSpeed -= _appliedMoveSpeedBonus;
            _appliedMoveSpeedBonus = 0f;
        }

        _stats.baseDamage = Mathf.Max(0f, _stats.baseDamage);
        _stats.moveSpeed = Mathf.Max(0f, _stats.moveSpeed);
    }

    public int GetStacks()
    {
        return _stacks;
    }

    public float GetAppliedDamageBonus()
    {
        return _appliedDamageBonus;
    }

    public float GetAppliedMoveSpeedBonus()
    {
        return _appliedMoveSpeedBonus;
    }
}