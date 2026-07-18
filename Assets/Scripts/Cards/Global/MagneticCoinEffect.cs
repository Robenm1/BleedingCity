using UnityEngine;

[DisallowMultipleComponent]
public class MagneticCoinEffect : MonoBehaviour
{
    [Header("Magnetic Coin")]
    [Tooltip("How many direct player coin pickups are needed before the next coin becomes magnetic.")]
    public int coinsRequired = 5;

    [Tooltip("How far the magnetic coin pulls other coins.")]
    public float pullRadius = 6f;

    [Tooltip("How fast pulled coins move toward the magnetic coin location.")]
    public float pullSpeed = 12f;

    [Tooltip("Coin layers. Put your coin prefab on a Coin layer if possible.")]
    public LayerMask coinLayers;

    [Header("Wave Visual")]
    [Tooltip("Prefab with your magnetic wave animation.")]
    public GameObject magneticWavePrefab;

    [Tooltip("How long before the wave visual is destroyed.")]
    public float waveLifetime = 0.8f;

    [Tooltip("Original diameter of the wave animation in world units before scaling.")]
    public float waveBaseDiameter = 1f;

    [Tooltip("Extra scale correction for the wave animation.")]
    public float waveScaleMultiplier = 1f;

    [Header("Debug")]
    public bool showDebug = true;

    private int _coinCounter;
    private bool _magneticReady;

    public bool ShouldActivateMagneticCoin()
    {
        return _magneticReady;
    }

    public void RegisterDirectPlayerCoin()
    {
        if (_magneticReady)
            return;

        _coinCounter++;

        if (showDebug)
            Debug.Log($"[MagneticCoinEffect] Coin counter: {_coinCounter}/{coinsRequired}");

        if (_coinCounter >= Mathf.Max(1, coinsRequired))
        {
            _coinCounter = 0;
            _magneticReady = true;

            if (showDebug)
                Debug.Log("[MagneticCoinEffect] Next coin is magnetic.");
        }
    }

    public void ActivateMagneticCoin(Vector3 coinPosition, PlayerXP playerXP, XPCoin magneticCoin)
    {
        if (!_magneticReady)
            return;

        _magneticReady = false;

        SpawnWaveVisual(coinPosition);
        PullCoinsToPosition(coinPosition, playerXP, magneticCoin);

        if (showDebug)
            Debug.Log("[MagneticCoinEffect] Magnetic coin activated.");
    }

    private void SpawnWaveVisual(Vector3 position)
    {
        if (magneticWavePrefab == null)
            return;

        GameObject obj = Instantiate(magneticWavePrefab, position, Quaternion.identity);

        float targetDiameter = Mathf.Max(0.1f, pullRadius) * 2f;
        float baseDiameter = Mathf.Max(0.01f, waveBaseDiameter);

        float scale = targetDiameter / baseDiameter;
        scale *= Mathf.Max(0.01f, waveScaleMultiplier);

        obj.transform.localScale *= scale;

        Destroy(obj, Mathf.Max(0.05f, waveLifetime));
    }

    private void PullCoinsToPosition(Vector3 position, PlayerXP playerXP, XPCoin magneticCoin)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            position,
            Mathf.Max(0.1f, pullRadius),
            coinLayers
        );

        foreach (var hit in hits)
        {
            if (hit == null) continue;

            var coin = hit.GetComponent<XPCoin>();
            if (coin == null) coin = hit.GetComponentInParent<XPCoin>();
            if (coin == null) continue;

            // Do not pull the coin that activated the magnet.
            if (coin == magneticCoin) continue;

            coin.PullToMagneticCoin(position, pullSpeed, playerXP);
        }
    }

    public int GetCoinCounter() => _coinCounter;
    public bool IsMagneticReady() => _magneticReady;

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.1f, pullRadius));
    }
#endif
}