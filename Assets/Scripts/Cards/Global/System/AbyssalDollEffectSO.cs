using UnityEngine;

[CreateAssetMenu(fileName = "AbyssalDollEffect", menuName = "Game/Cards/Effects/Global/Abyssal Doll")]
public class AbyssalDollEffectSO : CardEffectSO
{
    [Header("Abyssal Doll")]
    [Tooltip("The Abyssal Doll prefab dropped by the player.")]
    public GameObject dollPrefab;

    [Tooltip("How long the doll stays alive.")]
    public float duration = 5f;

    [Tooltip("Maximum waves before the doll disappears.")]
    public int maxWaves = 5;

    [Header("Cooldown")]
    [Tooltip("Cooldown before Abyssal Doll can be used again.")]
    public float cooldown = 8f;

    [Header("Wave")]
    [Tooltip("Radius of each wave.")]
    public float waveRadius = 4f;

    [Tooltip("Damage scaling from PlayerStats.GetDamage(). 1 = 100% attack.")]
    public float damageScaling = 1f;

    [Tooltip("Enemy layers damaged by the wave.")]
    public LayerMask enemyLayers;

    [Header("Slow")]
    [Tooltip("How long enemies are slowed by the wave.")]
    public float slowDuration = 2f;

    [Tooltip("Enemy speed multiplier while slowed. 0.5 = 50% speed.")]
    public float slowMultiplier = 0.5f;

    [Header("Debug")]
    public bool showDebug = true;

    public override void Apply(GameObject player)
    {
        var effect = player.GetComponent<AbyssalDollEffect>();
        if (!effect) effect = player.AddComponent<AbyssalDollEffect>();

        effect.dollPrefab = dollPrefab;

        effect.duration = Mathf.Max(0.1f, duration);
        effect.maxWaves = Mathf.Max(1, maxWaves);

        effect.cooldown = Mathf.Max(0f, cooldown);

        effect.waveRadius = Mathf.Max(0.1f, waveRadius);
        effect.damageScaling = Mathf.Max(0f, damageScaling);
        effect.enemyLayers = enemyLayers;

        effect.slowDuration = Mathf.Max(0f, slowDuration);
        effect.slowMultiplier = Mathf.Clamp(slowMultiplier, 0.01f, 1f);

        effect.showDebug = showDebug;
        effect.enabled = true;

        var router = player.GetComponent<ActiveCardInputRouter>();
        if (!router) router = player.AddComponent<ActiveCardInputRouter>();

        router.RegisterFirstFree(effect);
    }
}