using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerStats))]
public class AbyssalDollEffect : MonoBehaviour, IActiveCardEffect, IManualActiveCardCooldown, IActiveCardHUDReceiver
{
    public bool UsesManualCooldown => true;

    [Header("Abyssal Doll")]
    public GameObject dollPrefab;
    public float duration = 5f;
    public int maxWaves = 5;

    [Header("Cooldown")]
    public float cooldown = 8f;

    [Header("Wave")]
    public float waveRadius = 4f;
    public float damageScaling = 1f;
    public LayerMask enemyLayers;

    [Header("Slow")]
    public float slowDuration = 2f;
    public float slowMultiplier = 0.5f;

    [Header("Debug")]
    public bool showDebug = true;

    private PlayerStats _stats;
    private ActiveCardInputRouter _router;
    private ActiveCardHUD _hud;
    private ActiveCardInputRouter.ActiveSlot _slot;

    private float _cooldownTimer;
    private GameObject _activeDoll;
    private Coroutine _waitForDollRoutine;

    private void Awake()
    {
        _stats = GetComponent<PlayerStats>();
        _router = GetComponent<ActiveCardInputRouter>();
    }

    private void OnEnable()
    {
        if (!_stats)
            _stats = GetComponent<PlayerStats>();

        if (!_router)
            _router = GetComponent<ActiveCardInputRouter>();
    }

    private void Update()
    {
        if (_cooldownTimer <= 0f)
            return;

        _cooldownTimer -= Time.deltaTime;

        if (_cooldownTimer < 0f)
            _cooldownTimer = 0f;
    }

    public void SetActiveCardHUD(ActiveCardHUD hud, ActiveCardInputRouter.ActiveSlot slot)
    {
        _hud = hud;
        _slot = slot;
    }

    public void Activate()
    {
        if (_cooldownTimer > 0f)
        {
            if (showDebug)
                Debug.Log($"[AbyssalDollEffect] On cooldown: {_cooldownTimer:F1}s remaining.");

            return;
        }

        if (_activeDoll != null)
        {
            if (showDebug)
                Debug.Log("[AbyssalDollEffect] Doll is already active.");

            return;
        }

        if (!_stats)
            _stats = GetComponent<PlayerStats>();

        if (!_router)
            _router = GetComponent<ActiveCardInputRouter>();

        if (dollPrefab == null)
        {
            Debug.LogWarning("[AbyssalDollEffect] Doll prefab is not assigned.");
            return;
        }

        SpawnDoll();

        // Gray card while doll exists. No timer yet.
        SetHUDBusy(true);
    }

    private void SpawnDoll()
    {
        GameObject obj = Instantiate(dollPrefab, transform.position, Quaternion.identity);

        AbyssalDollObject doll = obj.GetComponent<AbyssalDollObject>();

        if (!doll)
        {
            Debug.LogWarning("[AbyssalDollEffect] Doll prefab does not have AbyssalDollObject on the root.");
            Destroy(obj);
            SetHUDBusy(false);
            return;
        }

        _activeDoll = obj;

        doll.playerStats = _stats;
        doll.duration = Mathf.Max(0.1f, duration);
        doll.maxWaves = Mathf.Max(1, maxWaves);
        doll.waveRadius = Mathf.Max(0.1f, waveRadius);
        doll.damageScaling = Mathf.Max(0f, damageScaling);
        doll.enemyLayers = enemyLayers;
        doll.slowDuration = Mathf.Max(0f, slowDuration);
        doll.slowMultiplier = Mathf.Clamp(slowMultiplier, 0.01f, 1f);
        doll.showDebug = showDebug;

        if (_waitForDollRoutine != null)
            StopCoroutine(_waitForDollRoutine);

        _waitForDollRoutine = StartCoroutine(WaitForDollDestroyed());

        if (showDebug)
            Debug.Log("[AbyssalDollEffect] Doll spawned. HUD busy started. Cooldown has NOT started yet.");
    }

    private IEnumerator WaitForDollDestroyed()
    {
        while (_activeDoll != null)
            yield return null;

        _activeDoll = null;
        _waitForDollRoutine = null;

        StartCooldownAfterDollDestroyed();
    }

    private void StartCooldownAfterDollDestroyed()
    {
        if (_cooldownTimer > 0f)
            return;

        float multiplier = _stats != null ? _stats.GetCooldownMultiplier() : 1f;
        _cooldownTimer = Mathf.Max(0f, cooldown * multiplier);

        SetHUDBusy(false);

        if (_router != null)
            _router.StartCooldownForEffect(this, _cooldownTimer);
        else
            StartHUDCooldownDirect(_cooldownTimer);

        if (showDebug)
            Debug.Log($"[AbyssalDollEffect] Doll destroyed. Cooldown started ONCE: {_cooldownTimer:F1}s.");
    }

    private void SetHUDBusy(bool busy)
    {
        if (_router != null)
        {
            _router.SetBusyForEffect(this, busy);
            return;
        }

        if (_hud == null)
            return;

        if (_slot == ActiveCardInputRouter.ActiveSlot.Active1)
            _hud.SetActiveCard1Busy(busy);
        else
            _hud.SetActiveCard2Busy(busy);
    }

    private void StartHUDCooldownDirect(float duration)
    {
        if (_hud == null)
            return;

        if (_slot == ActiveCardInputRouter.ActiveSlot.Active1)
            _hud.StartCooldownActive1(duration);
        else
            _hud.StartCooldownActive2(duration);
    }

    public bool IsOnCooldown()
    {
        return _cooldownTimer > 0f;
    }

    public float GetCooldownRemaining()
    {
        return Mathf.Max(0f, _cooldownTimer);
    }

    public float GetCooldownNormalized()
    {
        if (cooldown <= 0f)
            return 0f;

        float multiplier = _stats != null ? _stats.GetCooldownMultiplier() : 1f;
        float finalCooldown = Mathf.Max(0.01f, cooldown * multiplier);

        return Mathf.Clamp01(_cooldownTimer / finalCooldown);
    }
}