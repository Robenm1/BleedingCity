using UnityEngine;

[CreateAssetMenu(fileName = "SacrificialDummyEffect", menuName = "Game/Cards/Effects/Owl/Sacrificial Dummy")]
public class SacrificialDummyEffectSO : CardEffectSO
{
    [Header("Turret Spawn Chances")]
    [Range(0f, 1f)] public float baseTurretChance = 0.20f;
    [Range(0f, 1f)] public float frostedTurretChance = 1.0f;
    [Range(0f, 1f)] public float shardFrostChance = 0.20f;

    [Header("Turret Settings")]
    public Color turretTint = new Color(0.7f, 0.9f, 1f, 1f);
    public int shardsToFire = 6;
    public float fireInterval = 0.12f;
    public float spreadAngle = 20f;
    public float seekRange = 8f;

    [Header("Projectile")]
    public GameObject frostShardPrefab;
    public float shardSpeed = 18f;
    public float shardLifetime = 2.5f;
    public float shardDamage = 12f;
    public LayerMask enemyLayers;

    [Header("Frost Effect")]
    [Range(0.05f, 1f)] public float frostSlow = 0.6f;
    public float frostDuration = 2.5f;
    public Sprite frostIcon;
    public Vector2 frostIconPivot = new Vector2(0f, 0.85f);
    public Vector2 frostIconSize = new Vector2(0.35f, 0.35f);

    public override void Apply(GameObject player)
    {
        if (!player) return;

        var eff = player.GetComponent<SacrificialDummyEffect>();
        if (!eff) eff = player.AddComponent<SacrificialDummyEffect>();

        eff.baseTurretChance = baseTurretChance;
        eff.frostedTurretChance = frostedTurretChance;
        eff.shardFrostChance = shardFrostChance;
        eff.turretTint = turretTint;
        eff.shardsToFire = Mathf.Max(1, shardsToFire);
        eff.fireInterval = Mathf.Max(0.01f, fireInterval);
        eff.spreadAngle = Mathf.Max(0f, spreadAngle);
        eff.seekRange = Mathf.Max(0.1f, seekRange);
        eff.frostShardPrefab = frostShardPrefab;
        eff.shardSpeed = Mathf.Max(0.1f, shardSpeed);
        eff.shardLifetime = Mathf.Max(0.05f, shardLifetime);
        eff.shardDamage = Mathf.Max(0f, shardDamage);
        eff.enemyLayers = enemyLayers;
        eff.frostSlow = Mathf.Clamp(frostSlow, 0.05f, 1f);
        eff.frostDuration = Mathf.Max(0f, frostDuration);
        eff.frostIcon = frostIcon;
        eff.frostIcon = frostIcon;
        eff.frostIconPivot = frostIconPivot;
        eff.frostIconSize = frostIconSize;
    }
}