using UnityEngine;

[CreateAssetMenu(fileName = "BulletFromThePastEffect", menuName = "Game/Cards/Effects/Global/Bullet from the Past")]
public class BulletFromThePastEffectSO : CardEffectSO
{
    [Header("Bullet from the Past")]
    public GameObject bulletPrefab;

    [Tooltip("Fire one bullet every X successful enemy hits.")]
    public int attacksRequired = 5;

    [Header("Targeting")]
    public LayerMask enemyLayers;
    public float targetSearchRadius = 14f;

    [Header("Projectile")]
    public float bulletSpeed = 14f;
    public float bulletLifetime = 3f;
    public float hitRadius = 0.15f;

    [Header("Explosion")]
    public float explosionRadius = 3f;

    [Tooltip("1 = 100% player attack damage.")]
    public float damageScaling = 1f;

    [Header("Slow")]
    public float slowDuration = 2f;
    public float slowMultiplier = 0.5f;

    [Header("Visuals")]
    public string explosionStateName = "Explosion";
    public float explosionDuration = 0.45f;

    [Header("Debug")]
    public bool showDebug = true;

    public override void Apply(GameObject player)
    {
        var effect = player.GetComponent<BulletFromThePastEffect>();
        if (!effect) effect = player.AddComponent<BulletFromThePastEffect>();

        effect.bulletPrefab = bulletPrefab;
        effect.attacksRequired = Mathf.Max(1, attacksRequired);

        effect.enemyLayers = enemyLayers;
        effect.targetSearchRadius = Mathf.Max(0.1f, targetSearchRadius);

        effect.bulletSpeed = Mathf.Max(0.1f, bulletSpeed);
        effect.bulletLifetime = Mathf.Max(0.1f, bulletLifetime);
        effect.hitRadius = Mathf.Max(0.01f, hitRadius);

        effect.explosionRadius = Mathf.Max(0.1f, explosionRadius);
        effect.damageScaling = Mathf.Max(0f, damageScaling);

        effect.slowDuration = Mathf.Max(0f, slowDuration);
        effect.slowMultiplier = Mathf.Clamp(slowMultiplier, 0.01f, 1f);

        effect.explosionStateName = explosionStateName;
        effect.explosionDuration = Mathf.Max(0.05f, explosionDuration);

        effect.showDebug = showDebug;
        effect.enabled = true;
    }
}