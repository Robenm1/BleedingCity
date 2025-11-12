using UnityEngine;

[DisallowMultipleComponent]
public class HealingDuneEffect : MonoBehaviour
{
    [Header("Config (set by SO)")]
    public float healPerSecond = 6f;
    public bool useUnscaledTime = false;

    // Fallbacks not needed anymore because we listen to spawn/despawn events
    private PlayerHealth _health;
    private Transform _self;

    // The current active slow zone that belongs to this player (if any)
    private SlowZone _myActiveZone;

    private void OnEnable()
    {
        SlowZone.OnZoneSpawned += HandleZoneSpawned;
        SlowZone.OnZoneDestroyed += HandleZoneDestroyed;
    }

    private void OnDisable()
    {
        SlowZone.OnZoneSpawned -= HandleZoneSpawned;
        SlowZone.OnZoneDestroyed -= HandleZoneDestroyed;
    }

    private void Awake()
    {
        _self = transform;
        _health = GetComponent<PlayerHealth>();
        if (_health == null)
            Debug.LogWarning("[HealingDuneEffect] PlayerHealth not found on owner.", this);
    }

    private void Update()
    {
        if (_health == null || healPerSecond <= 0f) return;
        if (_myActiveZone == null || !_myActiveZone.isActive) return;

        // Are we inside our own zone?
        float r = Mathf.Max(0f, _myActiveZone.radius);
        float d = Vector2.Distance(_self.position, _myActiveZone.transform.position);
        if (d <= r)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            if (dt > 0f) _health.Heal(healPerSecond * dt);
        }
    }

    private void HandleZoneSpawned(SlowZone z)
    {
        if (z != null && z.owner == _self)
            _myActiveZone = z;
    }

    private void HandleZoneDestroyed(SlowZone z)
    {
        if (z == _myActiveZone)
            _myActiveZone = null;
    }
}
