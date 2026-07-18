using UnityEngine;

[CreateAssetMenu(fileName = "MagneticCoinEffect", menuName = "Game/Cards/Effects/Global/Magnetic Coin")]
public class MagneticCoinEffectSO : CardEffectSO
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

    public override void Apply(GameObject player)
    {
        var effect = player.GetComponent<MagneticCoinEffect>();
        if (!effect) effect = player.AddComponent<MagneticCoinEffect>();

        effect.coinsRequired = Mathf.Max(1, coinsRequired);

        effect.pullRadius = Mathf.Max(0.1f, pullRadius);
        effect.pullSpeed = Mathf.Max(0.1f, pullSpeed);
        effect.coinLayers = coinLayers;

        effect.magneticWavePrefab = magneticWavePrefab;
        effect.waveLifetime = Mathf.Max(0.05f, waveLifetime);
        effect.waveBaseDiameter = Mathf.Max(0.01f, waveBaseDiameter);
        effect.waveScaleMultiplier = Mathf.Max(0.01f, waveScaleMultiplier);

        effect.showDebug = showDebug;
        effect.enabled = true;
    }
}