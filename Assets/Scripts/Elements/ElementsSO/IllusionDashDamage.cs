using UnityEngine;

[DisallowMultipleComponent]
public class IllusionDashDamage : MonoBehaviour
{
    [Header("Runtime")]
    [SerializeField] private int dashStacks;

    [SerializeField] private float dashCooldownReductionPercent = 0.2f;
    [SerializeField] private float bonusDamagePercentPerDash = 0.2f;
    [SerializeField] private int maxDashStacks = 5;
    [SerializeField] private bool consumeStacksOnAttack = true;
    [SerializeField] private bool showDebug = false;

    private PlayerControls _controls;
    private PlayerStats _stats;

    private bool _registered;
    private bool _dashCooldownApplied;
    private float _dashCooldownAmountRemoved;

    private void Awake()
    {
        CacheReferences();
    }

    private void OnEnable()
    {
        Register();
        ApplyDashCooldownReduction();
    }

    private void OnDisable()
    {
        Unregister();
        RemoveDashCooldownReduction();
    }

    public void Configure(
        float cooldownReduction,
        float bonusPerDash,
        int maxStacks,
        bool consumeOnAttack,
        bool debug
    )
    {
        dashCooldownReductionPercent = Mathf.Clamp(cooldownReduction, 0f, 0.9f);
        bonusDamagePercentPerDash = Mathf.Max(0f, bonusPerDash);
        maxDashStacks = Mathf.Max(1, maxStacks);
        consumeStacksOnAttack = consumeOnAttack;
        showDebug = debug;

        if (dashStacks > maxDashStacks)
            dashStacks = maxDashStacks;

        Register();
        ApplyDashCooldownReduction();
    }

    private void CacheReferences()
    {
        _controls = GetComponent<PlayerControls>();
        if (_controls == null)
            _controls = GetComponentInParent<PlayerControls>();

        _stats = GetComponent<PlayerStats>();
        if (_stats == null)
            _stats = GetComponentInParent<PlayerStats>();
    }

    private void Register()
    {
        if (_registered)
            return;

        if (_controls == null)
            CacheReferences();

        if (_controls == null)
            return;

        _controls.OnDash += HandleDash;
        _registered = true;
    }

    private void Unregister()
    {
        if (!_registered || _controls == null)
            return;

        _controls.OnDash -= HandleDash;
        _registered = false;
    }

    private void HandleDash()
    {
        dashStacks = Mathf.Min(dashStacks + 1, maxDashStacks);

        if (showDebug)
        {
            Debug.Log(
                $"[IllusionDashDamage] {name} dashed. " +
                $"Stacks: {dashStacks}/{maxDashStacks}"
            );
        }
    }

    private void ApplyDashCooldownReduction()
    {
        if (_dashCooldownApplied)
            return;

        if (_stats == null)
            CacheReferences();

        if (_stats == null)
            return;

        if (dashCooldownReductionPercent <= 0f)
            return;

        float currentCooldown = Mathf.Max(0f, _stats.dashCooldown);
        _dashCooldownAmountRemoved = currentCooldown * dashCooldownReductionPercent;

        _stats.dashCooldown = Mathf.Max(0f, currentCooldown - _dashCooldownAmountRemoved);
        _dashCooldownApplied = true;

        if (showDebug)
        {
            Debug.Log(
                $"[IllusionDashDamage] Dash cooldown reduced. " +
                $"Removed: {_dashCooldownAmountRemoved:F2}, " +
                $"New cooldown: {_stats.dashCooldown:F2}"
            );
        }
    }

    public void RemoveDashCooldownReduction()
    {
        if (!_dashCooldownApplied)
            return;

        if (_stats == null)
            CacheReferences();

        if (_stats != null)
            _stats.dashCooldown += _dashCooldownAmountRemoved;

        _dashCooldownAmountRemoved = 0f;
        _dashCooldownApplied = false;

        if (showDebug)
            Debug.Log("[IllusionDashDamage] Dash cooldown reduction removed.");
    }

    public float ApplyDashBonusToDamage(float damage)
    {
        if (damage <= 0f)
            return damage;

        if (dashStacks <= 0)
            return damage;

        float bonusMultiplier = 1f + (dashStacks * bonusDamagePercentPerDash);
        float finalDamage = damage * bonusMultiplier;

        if (showDebug)
        {
            Debug.Log(
                $"[IllusionDashDamage] Applying dash bonus. " +
                $"Stacks: {dashStacks}, Multiplier: {bonusMultiplier:F2}, " +
                $"Damage: {damage:F1} -> {finalDamage:F1}"
            );
        }

        if (consumeStacksOnAttack)
            dashStacks = 0;

        return finalDamage;
    }

    public void ClearStacks()
    {
        dashStacks = 0;
    }

    public int GetDashStacks()
    {
        return dashStacks;
    }
}