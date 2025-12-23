using UnityEngine;

[CreateAssetMenu(fileName = "DivineFeatherEffect", menuName = "Game/Cards/Effects/Owl/Divine Feather")]
public class DivineFeatherEffectSO : CardEffectSO
{
    [Header("Trigger Thresholds")]
    [Tooltip("HP threshold to spawn feathers (as percentage, e.g., 0.1 = 10%)")]
    [Range(0.05f, 0.5f)]
    public float triggerHpThreshold = 0.1f;

    [Tooltip("HP threshold to auto-shoot feathers (as percentage, e.g., 0.5 = 50%)")]
    [Range(0.1f, 0.9f)]
    public float shootHpThreshold = 0.5f;

    [Header("Feather Swarm")]
    [Tooltip("Owl's regular feather prefab to spawn")]
    public GameObject featherPrefab;

    [Tooltip("Number of feathers to spawn in orbit")]
    public int featherCount = 8;

    [Tooltip("Orbit radius around player")]
    public float orbitRadius = 2f;

    [Tooltip("Orbit rotation speed (degrees per second)")]
    public float orbitSpeed = 90f;

    [Header("Protection")]
    [Tooltip("Damage reduction while feathers are orbiting (0.5 = 50% damage reduction)")]
    [Range(0f, 0.9f)]
    public float damageReduction = 0.5f;

    [Tooltip("HP healed per second while feathers are active")]
    public float healPerSecond = 5f;

    [Header("Recall Settings")]
    [Tooltip("Speed multiplier when recalling feathers")]
    public float recallSpeedMultiplier = 2f;

    [Tooltip("Enemy layers for feather damage")]
    public LayerMask enemyLayers;

    [Header("Visual")]
    [Tooltip("Color tint for orbiting feathers")]
    public Color orbitColor = new Color(1f, 0.9f, 0.7f, 1f);

    public override void Apply(GameObject player)
    {
        if (!player) return;

        var effect = player.GetComponent<DivineFeatherEffect>();
        if (!effect) effect = player.AddComponent<DivineFeatherEffect>();

        effect.triggerHpThreshold = triggerHpThreshold;
        effect.shootHpThreshold = shootHpThreshold;
        effect.featherPrefab = featherPrefab;
        effect.featherCount = featherCount;
        effect.orbitRadius = orbitRadius;
        effect.orbitSpeed = orbitSpeed;
        effect.damageReduction = damageReduction;
        effect.healPerSecond = healPerSecond;
        effect.recallSpeedMultiplier = recallSpeedMultiplier;
        effect.enemyLayers = enemyLayers;
        effect.orbitColor = orbitColor;
    }
}
