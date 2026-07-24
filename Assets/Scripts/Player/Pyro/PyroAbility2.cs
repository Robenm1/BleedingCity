using System;
using UnityEngine;

/// <summary>
/// Pyro's second ability — Hell Bomb.
/// Plants a bomb at Pyro's feet. The bomb explodes on enemy contact,
/// marking all nearby enemies with Hell's Justice. Marked enemies take
/// extra damage from all of Pyro's summons.
/// </summary>
public class PyroAbility2 : MonoBehaviour
{
    public event Action<int, int> OnTrapChargesChanged;
    public event Action<float, float> OnCooldownChanged;

    [Header("References")]
    public GameObject hellBombPrefab;

    [Header("Cooldown")]
    public float baseCooldown = 6f;

    [Tooltip("How many bombs Pyro can plant before the ability goes on cooldown.")]
    public int bombsBeforeCooldown = 1;

    private float _cooldownTimer;
    private float _currentCooldownDuration;
    private int _bombsPlantedThisCycle = 0;

    [Header("Debug")]
    public bool showDebug = true;

    private PlayerControls _controls;
    private PlayerStats _stats;

    private void Awake()
    {
        _controls = GetComponent<PlayerControls>();
        _stats = GetComponent<PlayerStats>();
    }

    private void OnEnable()
    {
        if (_controls != null)
            _controls.OnAbility2 += OnAbility2Pressed;

        BroadcastUI();
    }

    private void OnDisable()
    {
        if (_controls != null)
            _controls.OnAbility2 -= OnAbility2Pressed;
    }

    private void Update()
    {
        if (_cooldownTimer > 0f)
        {
            _cooldownTimer -= Time.deltaTime;

            if (_cooldownTimer <= 0f)
            {
                _cooldownTimer = 0f;
                _bombsPlantedThisCycle = 0;
                BroadcastTrapCharges();
            }

            BroadcastCooldown();
        }
    }

    private void OnAbility2Pressed()
    {
        if (_cooldownTimer > 0f)
        {
            if (showDebug)
                Debug.Log($"[PyroAbility2] On cooldown: {_cooldownTimer:F1}s remaining.");

            return;
        }

        PlantBomb();
    }

    private void PlantBomb()
    {
        if (hellBombPrefab == null)
        {
            Debug.LogWarning("[PyroAbility2] hellBombPrefab is not assigned.");
            return;
        }

        Instantiate(hellBombPrefab, transform.position, Quaternion.identity);

        _bombsPlantedThisCycle++;

        int maxBombs = Mathf.Max(1, bombsBeforeCooldown);
        BroadcastTrapCharges();

        if (_bombsPlantedThisCycle >= maxBombs)
        {
            StartCooldown();

            if (showDebug)
                Debug.Log($"[PyroAbility2] Hell Bomb planted! Cooldown: {_currentCooldownDuration:F1}s");
        }
        else
        {
            if (showDebug)
                Debug.Log($"[PyroAbility2] Hell Bomb planted! {_bombsPlantedThisCycle}/{maxBombs} before cooldown.");
        }
    }

    private void StartCooldown()
    {
        float multiplier = _stats != null ? _stats.GetCooldownMultiplier() : 1f;
        _currentCooldownDuration = Mathf.Max(0.01f, baseCooldown * multiplier);
        _cooldownTimer = _currentCooldownDuration;

        BroadcastCooldown();
    }

    public void SetBombsBeforeCooldown(int value)
    {
        bombsBeforeCooldown = Mathf.Max(1, value);
        _bombsPlantedThisCycle = Mathf.Clamp(_bombsPlantedThisCycle, 0, bombsBeforeCooldown);

        BroadcastTrapCharges();

        if (showDebug)
            Debug.Log($"[PyroAbility2] Bombs before cooldown set to {bombsBeforeCooldown}.");
    }

    public int GetBombsBeforeCooldown()
    {
        return Mathf.Max(1, bombsBeforeCooldown);
    }

    public int GetBombsRemainingBeforeCooldown()
    {
        int maxBombs = Mathf.Max(1, bombsBeforeCooldown);
        return Mathf.Clamp(maxBombs - _bombsPlantedThisCycle, 0, maxBombs);
    }

    public bool HasTrapCounter()
    {
        return GetBombsBeforeCooldown() > 1;
    }

    public bool IsOnCooldown()
    {
        return _cooldownTimer > 0f;
    }

    public float GetCooldownNormalized()
    {
        float duration = _currentCooldownDuration > 0f ? _currentCooldownDuration : baseCooldown;
        return duration > 0f ? Mathf.Clamp01(_cooldownTimer / duration) : 0f;
    }

    public float GetCooldownRemaining()
    {
        return Mathf.Max(0f, _cooldownTimer);
    }

    public float GetCurrentCooldownDuration()
    {
        return _currentCooldownDuration > 0f ? _currentCooldownDuration : baseCooldown;
    }

    public void BroadcastUI()
    {
        BroadcastTrapCharges();
        BroadcastCooldown();
    }

    private void BroadcastTrapCharges()
    {
        OnTrapChargesChanged?.Invoke(GetBombsRemainingBeforeCooldown(), GetBombsBeforeCooldown());
    }

    private void BroadcastCooldown()
    {
        OnCooldownChanged?.Invoke(GetCooldownRemaining(), GetCurrentCooldownDuration());
    }
}