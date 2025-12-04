using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerStats))]
public class FeatherRecallAbility : MonoBehaviour
{
    public InputActionReference recallAction; // tap to recall
    public float recallSpeedMul = 1.2f;

    [Header("Cooldown")]
    [Tooltip("Base cooldown for recall ability (scaled by PlayerStats.cooldownMultiplier).")]
    public float baseCooldown = 3f;

    private int _ownerId;
    private Transform _ownerTf;
    private PlayerStats _stats;
    private float _cooldownTimer;

    private void Awake()
    {
        _ownerTf = transform;
        _stats = GetComponent<PlayerStats>();
        if (_ownerId == 0) _ownerId = gameObject.GetInstanceID();
    }

    private void OnEnable()
    {
        if (recallAction && recallAction.action != null)
        {
            if (!recallAction.action.enabled) recallAction.action.Enable();
            recallAction.action.performed += OnRecall;
        }
    }

    private void OnDisable()
    {
        if (recallAction && recallAction.action != null)
            recallAction.action.performed -= OnRecall;
    }

    private void Update()
    {
        if (_cooldownTimer > 0f)
        {
            _cooldownTimer -= Time.deltaTime;
            if (_cooldownTimer < 0f) _cooldownTimer = 0f;
        }
    }

    public void SetSharedOwnerId(int id) => _ownerId = id;

    private void OnRecall(InputAction.CallbackContext ctx)
    {
        if (_cooldownTimer > 0f) return; // Still on cooldown
        Recall();
    }

    public void Recall()
    {
        if (_cooldownTimer > 0f) return; // Still on cooldown

        // Get all feathers belonging to this owner from the registry
        var feathers = FeatherRegistry.GetAll(_ownerId);
        if (feathers == null || feathers.Count == 0) return;

        // Recall each feather (iterate backwards to avoid issues if feathers destroy themselves)
        for (int i = feathers.Count - 1; i >= 0; i--)
        {
            var feather = feathers[i];
            if (feather != null)
            {
                feather.BeginRecall(_ownerTf, recallSpeedMul);
            }
        }

        // Apply cooldown respecting PlayerStats cooldownMultiplier
        float cd = baseCooldown * Mathf.Max(0.05f, _stats ? _stats.GetCooldownMultiplier() : 1f);
        _cooldownTimer = cd;
    }

    /// <summary> Returns true if recall is ready to use. </summary>
    public bool IsReady() => _cooldownTimer <= 0f;

    /// <summary> Normalized remaining cooldown (1 = just used, 0 = ready). </summary>
    public float GetCooldownNormalized()
    {
        if (baseCooldown <= 0f) return 0f;
        return Mathf.Clamp01(_cooldownTimer / baseCooldown);
    }

    /// <summary> How many seconds remain on cooldown. </summary>
    public float GetCooldownRemaining() => Mathf.Max(0f, _cooldownTimer);
}