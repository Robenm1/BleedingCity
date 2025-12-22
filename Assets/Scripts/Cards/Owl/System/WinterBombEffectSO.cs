using UnityEngine;

[CreateAssetMenu(fileName = "WinterBombEffect", menuName = "Game/Cards/Effects/Owl/Winter Bomb")]
public class WinterBombEffectSO : CardEffectSO
{
    [Header("Explosion")]
    [Tooltip("Damage is Stats.baseDamage * damageMultiplier.")]
    public float damageMultiplier = 1.25f;
    [Tooltip("Explosion radius for damage + frost.")]
    public float explosionRadius = 3.5f;
    [Tooltip("Which layers count as enemies.")]
    public LayerMask enemyLayers;

    [Header("Frosted Debuff")]
    [Range(0.1f, 1f)] public float slowFactor = 0.6f;
    public float slowDuration = 3f;
    public Sprite frostMarkSprite;
    public Vector2 frostMarkOffset = new Vector2(0f, 1f);
    public Vector2 frostMarkSize = new Vector2(0.5f, 0.5f);

    [Header("Bonus Damage for Frosted Enemies")]
    [Tooltip("Frosted enemies take this much extra damage (1.15 = 15% more).")]
    public float frostedDamageMultiplier = 1.15f;

    [Header("Circle Pulse VFX")]
    public bool spawnCircle = true;
    public Color circleColor = new Color(0.6f, 0.8f, 1f, 0.55f);
    public float circleDuration = 0.5f;
    public float circleLineWidth = 0.08f;
    [Range(8, 256)] public int circleSegments = 72;
    [Range(0f, 1f)] public float circleStartRadiusFraction = 0.55f;

    public override void Apply(GameObject player)
    {
        if (!player) return;
        var eff = player.GetComponent<WinterBombEffect>();
        if (!eff) eff = player.AddComponent<WinterBombEffect>();

        eff.damageMultiplier = Mathf.Max(0f, damageMultiplier);
        eff.explosionRadius = Mathf.Max(0.01f, explosionRadius);
        eff.enemyLayers = enemyLayers;
        eff.slowFactor = Mathf.Clamp(slowFactor, 0.1f, 1f);
        eff.slowDuration = Mathf.Max(0.01f, slowDuration);
        eff.frostMarkSprite = frostMarkSprite;
        eff.frostMarkOffset = frostMarkOffset;
        eff.frostMarkSize = frostMarkSize;
        eff.frostedDamageMultiplier = Mathf.Max(1f, frostedDamageMultiplier);
        eff.spawnCircle = spawnCircle;
        eff.circleColor = circleColor;
        eff.circleDuration = Mathf.Max(0.05f, circleDuration);
        eff.circleLineWidth = Mathf.Max(0.001f, circleLineWidth);
        eff.circleSegments = Mathf.Clamp(circleSegments, 8, 256);
        eff.circleStartRadiusFraction = Mathf.Clamp01(circleStartRadiusFraction);
    }
}