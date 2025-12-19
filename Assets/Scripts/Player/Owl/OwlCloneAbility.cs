using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerStats))]
public class OwlCloneAbility : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Hold to preview, release to spawn clone.")]
    public InputActionReference abilityAction;
    [Tooltip("Your WASD/LeftStick vector2 for aiming the preview.")]
    public InputActionReference moveAction;

    [Header("Clone")]
    public GameObject clonePrefab;
    [Tooltip("Min distance for a quick tap spawn.")]
    public float minLaunchDistance = 2f;
    [Tooltip("Max distance the preview can extend to while holding.")]
    public float maxLaunchDistance = 12f;
    [Tooltip("How fast the preview distance grows per second while held.")]
    public float distancePerSecond = 8f;
    [Tooltip("Seconds the clone stays alive.")]
    public float cloneDuration = 8f;

    [Header("Cooldown")]
    [Tooltip("Base cooldown (scaled by PlayerStats.cooldownMultiplier).")]
    public float baseCooldown = 18f;

    [Header("Preview")]
    [Range(0f, 1f)] public float ghostAlpha = 0.45f;

    private PlayerStats _stats;
    private OwlFeatherShooter _playerShooter;
    private FeatherRecallAbility _recall;

    private float _cooldownTimer;
    private GameObject _activeClone;
    private GameObject _ghost;
    private float _chargeTime;
    private bool _charging;

    private int _sharedOwnerId;
    private Vector2 _lastNonZeroAim = Vector2.right;

    private void Awake()
    {
        _stats = GetComponent<PlayerStats>();
        _playerShooter = GetComponent<OwlFeatherShooter>();
        _recall = GetComponent<FeatherRecallAbility>();

        _sharedOwnerId = gameObject.GetInstanceID();
        if (_playerShooter) _playerShooter.SetSharedOwnerId(_sharedOwnerId);
        if (_recall) _recall.SetSharedOwnerId(_sharedOwnerId);
    }

    private void OnEnable()
    {
        if (abilityAction && abilityAction.action != null)
        {
            if (!abilityAction.action.enabled) abilityAction.action.Enable();
            abilityAction.action.started += OnAbilityStarted;
            abilityAction.action.canceled += OnAbilityCanceled;
        }
    }

    private void OnDisable()
    {
        if (abilityAction && abilityAction.action != null)
        {
            abilityAction.action.started -= OnAbilityStarted;
            abilityAction.action.canceled -= OnAbilityCanceled;
        }

        DestroyGhost();
    }

    private void Update()
    {
        if (_cooldownTimer > 0f)
        {
            _cooldownTimer -= Time.deltaTime;
            if (_cooldownTimer < 0f) _cooldownTimer = 0f;
        }

        if (_charging)
        {
            _chargeTime += Time.deltaTime;
            UpdateGhost();
        }
    }

    private void OnAbilityStarted(InputAction.CallbackContext ctx)
    {
        if (_cooldownTimer > 0f) return;
        _charging = true;
        _chargeTime = 0f;
        CreateGhost();
    }

    private void OnAbilityCanceled(InputAction.CallbackContext ctx)
    {
        if (!_charging) return;
        _charging = false;

        if (_cooldownTimer > 0f) { DestroyGhost(); return; }
        SpawnCloneFromGhost();
    }

    private void CreateGhost()
    {
        if (!clonePrefab) return;

        DestroyGhost();

        _ghost = Instantiate(clonePrefab, transform.position, Quaternion.identity);
        SetRenderersAlpha(_ghost, ghostAlpha);
        FreezeClone(_ghost);
        DisableShooter(_ghost);
        DisableColliders(_ghost); // NEW: Disable all colliders on ghost
        UpdateGhost();
    }

    private void UpdateGhost()
    {
        if (!_ghost) return;

        Vector2 aim = ReadAimDir();
        if (aim.sqrMagnitude > 0.0001f)
            _lastNonZeroAim = aim;

        float dist = Mathf.Clamp(_chargeTime * distancePerSecond, minLaunchDistance, maxLaunchDistance);
        Vector3 targetPos = transform.position + (Vector3)(_lastNonZeroAim * dist);
        _ghost.transform.position = targetPos;
    }

    private void DestroyGhost()
    {
        if (_ghost) Destroy(_ghost);
        _ghost = null;
    }

    private Vector2 ReadAimDir()
    {
        if (moveAction && moveAction.action != null)
        {
            var v = moveAction.action.ReadValue<Vector2>();
            if (v.sqrMagnitude > 0.0001f) return v.normalized;
        }
        return Vector2.zero;
    }

    private void SpawnCloneFromGhost()
    {
        if (!clonePrefab) return;

        float cd = baseCooldown * Mathf.Max(0.05f, _stats ? _stats.GetCooldownMultiplier() : 1f);
        _cooldownTimer = cd;

        Vector3 spawnPos;
        if (_ghost)
        {
            spawnPos = _ghost.transform.position;
            DestroyGhost();
        }
        else
        {
            float dist = Mathf.Clamp(_chargeTime * distancePerSecond, minLaunchDistance, maxLaunchDistance);
            spawnPos = transform.position + (Vector3)(_lastNonZeroAim * dist);
        }

        if (_activeClone) Destroy(_activeClone);
        _activeClone = Instantiate(clonePrefab, spawnPos, Quaternion.identity);

        SetRenderersAlpha(_activeClone, 1f);
        FreezeClone(_activeClone);
        EnableColliders(_activeClone); // NEW: Re-enable colliders on real clone

        var cloneShooter = _activeClone.GetComponent<OwlFeatherShooter>();
        if (!cloneShooter) cloneShooter = _activeClone.AddComponent<OwlFeatherShooter>();
        if (_playerShooter)
        {
            cloneShooter.featherPrefab = _playerShooter.featherPrefab;
            cloneShooter.enemyLayers = _playerShooter.enemyLayers;
        }
        cloneShooter.SetSharedOwnerId(_sharedOwnerId);
        cloneShooter.SetRangeSource(_stats);
        cloneShooter.enabled = true;

        var life = _activeClone.GetComponent<SimpleLifetime>();
        if (!life) life = _activeClone.AddComponent<SimpleLifetime>();
        life.lifetime = cloneDuration;

        var onDestroy = _activeClone.GetComponent<OnDestroyCallback>();
        if (!onDestroy) onDestroy = _activeClone.AddComponent<OnDestroyCallback>();
        onDestroy.Init(() =>
        {
            _activeClone = null;
        });
    }

    /// <summary>
    /// Called by PlayerMovement when dash button is pressed.
    /// Returns true if swap occurred (clone exists), false if dash should proceed normally.
    /// </summary>
    public bool TrySwapWithClone()
    {
        if (_activeClone == null) return false;

        // Swap positions
        Vector3 myPos = transform.position;
        transform.position = _activeClone.transform.position;
        _activeClone.transform.position = myPos;

        return true; // Swap occurred, don't dash
    }

    /// <summary>
    /// Check if a clone is currently active (for external scripts).
    /// </summary>
    public bool HasActiveClone() => _activeClone != null;

    public GameObject GetActiveClone() => _activeClone;


    private void FreezeClone(GameObject obj)
    {
        if (!obj) return;

        var rb = obj.GetComponent<Rigidbody2D>();
        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        foreach (var m in obj.GetComponents<MonoBehaviour>())
        {
            if (!m) continue;
            // Don't disable OwlFeatherShooter here - we control it separately
            if (ReferenceEquals(m, obj.GetComponent<OwlFeatherShooter>())) continue;

            string tn = m.GetType().Name.ToLowerInvariant();
            if (tn.Contains("dash") || tn.Contains("move") || tn.Contains("controller"))
                m.enabled = false;
        }
    }

    private void DisableShooter(GameObject obj)
    {
        if (!obj) return;
        var shooter = obj.GetComponent<OwlFeatherShooter>();
        if (shooter) shooter.enabled = false;
    }

    private void DisableColliders(GameObject obj)
    {
        if (!obj) return;
        foreach (var col in obj.GetComponentsInChildren<Collider2D>(true))
        {
            col.enabled = false;
        }
    }

    private void EnableColliders(GameObject obj)
    {
        if (!obj) return;
        foreach (var col in obj.GetComponentsInChildren<Collider2D>(true))
        {
            col.enabled = true;
        }
    }

    private void SetRenderersAlpha(GameObject obj, float a)
    {
        if (!obj) return;
        foreach (var r in obj.GetComponentsInChildren<SpriteRenderer>(true))
        {
            var c = r.color; c.a = a; r.color = c;
        }
    }
}