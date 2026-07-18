using UnityEngine;

[DisallowMultipleComponent]
public class XPCoin : MonoBehaviour
{
    [Header("XP Coin Settings")]
    [Tooltip("How much XP this coin gives toward the next level.")]
    public int xpValue = 5;

    [Tooltip("How long this coin exists before it despawns. 0 or negative = never despawn.")]
    public float lifetime = 10f;

    [Header("Magnetic Coin Runtime")]
    [Tooltip("True when this coin is being pulled by a magnetic coin. Pulled coins do not charge the magnetic counter.")]
    public bool pulledByMagnet;

    [Tooltip("Distance from target position where the pulled coin gets collected.")]
    public float magnetCollectDistance = 0.15f;

    private float _lifeTimer;

    private bool _isBeingPulled;
    private bool _collected;

    private Vector3 _magnetTargetPosition;
    private float _magnetPullSpeed;
    private PlayerXP _magnetPlayerXP;

    private void Awake()
    {
        _lifeTimer = lifetime;
    }

    private void Update()
    {
        UpdateLifetime();
        UpdateMagnetPull();
    }

    private void UpdateLifetime()
    {
        if (_isBeingPulled)
            return;

        if (lifetime <= 0f)
            return;

        _lifeTimer -= Time.deltaTime;

        if (_lifeTimer <= 0f)
            Destroy(gameObject);
    }

    private void UpdateMagnetPull()
    {
        if (!_isBeingPulled || _collected)
            return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            _magnetTargetPosition,
            Mathf.Max(0.1f, _magnetPullSpeed) * Time.deltaTime
        );

        float distance = Vector3.Distance(transform.position, _magnetTargetPosition);

        if (distance <= Mathf.Max(0.01f, magnetCollectDistance))
        {
            Collect(_magnetPlayerXP, true);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_collected)
            return;

        if (!other.CompareTag("Player"))
            return;

        PlayerXP playerXP = other.GetComponent<PlayerXP>();
        if (playerXP == null)
            return;

        MagneticCoinEffect magneticEffect = other.GetComponent<MagneticCoinEffect>();

        bool shouldActivateMagnet =
            !pulledByMagnet &&
            magneticEffect != null &&
            magneticEffect.ShouldActivateMagneticCoin();

        if (shouldActivateMagnet)
        {
            magneticEffect.ActivateMagneticCoin(transform.position, playerXP, this);
            Collect(playerXP, false);
            return;
        }

        if (!pulledByMagnet && magneticEffect != null)
        {
            magneticEffect.RegisterDirectPlayerCoin();
        }

        Collect(playerXP, pulledByMagnet);
    }

    public void PullToMagneticCoin(Vector3 targetPosition, float pullSpeed, PlayerXP playerXP)
    {
        if (_collected)
            return;

        pulledByMagnet = true;
        _isBeingPulled = true;

        _magnetTargetPosition = targetPosition;
        _magnetPullSpeed = Mathf.Max(0.1f, pullSpeed);
        _magnetPlayerXP = playerXP;
    }

    private void Collect(PlayerXP playerXP, bool collectedByMagnet)
    {
        if (_collected)
            return;

        _collected = true;

        if (playerXP != null)
        {
            playerXP.AddCoinPickup(xpValue);
        }

        Destroy(gameObject);
    }
}