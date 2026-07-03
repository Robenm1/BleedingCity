using UnityEngine;

[CreateAssetMenu(fileName = "WillOTheWispEffect", menuName = "Game/Cards/Effects/Pyro/Will-o'-the-wisp")]
public class WillOTheWispEffectSO : CardEffectSO
{
    [Header("Wisp Gain")]
    [Tooltip("How many enemy kills are needed to gain 1 wisp.")]
    public int killsPerWisp = 3;

    [Tooltip("Maximum wisps Pyro can hold at the same time.")]
    public int maxWisps = 3;

    [Header("Wisp Damage")]
    [Tooltip("Damage dealt as percent of the target's max HP. 0.05 = 5%.")]
    public float targetMaxHpDamagePercent = 0.05f;

    [Tooltip("Enemy layers the wisps can target.")]
    public LayerMask enemyLayers;

    [Tooltip("How far the card searches for the nearest enemy.")]
    public float targetSearchRadius = 12f;

    [Header("Projectile")]
    [Tooltip("How fast fired wisps move toward the target.")]
    public float projectileSpeed = 14f;

    [Tooltip("How close the wisp must get before dealing damage.")]
    public float hitRadius = 0.2f;

    [Tooltip("How long a fired wisp can exist before being destroyed.")]
    public float projectileLifetime = 3f;

    [Header("Visuals")]
    [Tooltip("Sprites used for the stored wisps. Assign your 3 different wisp sprites here.")]
    public Sprite[] wispSprites = new Sprite[3];

    [Tooltip("How far wisps can float away from Pyro.")]
    public float floatRadius = 0.75f;

    [Tooltip("How fast wisps drift around Pyro.")]
    public float floatSpeed = 2f;

    [Tooltip("How much each wisp bobs up and down.")]
    public float bobAmount = 0.12f;

    [Tooltip("Delay between each fired wisp. Makes them fly one after another.")]
    public float fireInterval = 0.12f;

    [Tooltip("Small side/back offset between fired wisps so they do not stack perfectly.")]
    public float launchSpread = 0.18f;

    [Tooltip("How fast each wisp bobs.")]
    public float bobSpeed = 3f;

    [Tooltip("How long it takes a newly gained wisp to fade in.")]
    public float fadeInDuration = 0.35f;

    [Tooltip("Sorting order used by stored and fired wisp sprites.")]
    public int sortingOrder = 5;

    [Header("Debug")]
    public bool showDebug = true;

    public override void Apply(GameObject player)
    {
        var effect = player.GetComponent<WillOTheWispEffect>();
        if (!effect) effect = player.AddComponent<WillOTheWispEffect>();

        effect.killsPerWisp = Mathf.Max(1, killsPerWisp);
        effect.maxWisps = Mathf.Max(1, maxWisps);
        effect.targetMaxHpDamagePercent = Mathf.Max(0f, targetMaxHpDamagePercent);
        effect.enemyLayers = enemyLayers;
        effect.targetSearchRadius = Mathf.Max(0.1f, targetSearchRadius);

        effect.projectileSpeed = Mathf.Max(0.1f, projectileSpeed);
        effect.hitRadius = Mathf.Max(0.01f, hitRadius);
        effect.projectileLifetime = Mathf.Max(0.1f, projectileLifetime);

        effect.wispSprites = wispSprites;
        effect.fireInterval = Mathf.Max(0f, fireInterval);
        effect.launchSpread = Mathf.Max(0f, launchSpread);
        effect.floatRadius = Mathf.Max(0f, floatRadius);
        effect.floatSpeed = Mathf.Max(0f, floatSpeed);
        effect.bobAmount = Mathf.Max(0f, bobAmount);
        effect.bobSpeed = Mathf.Max(0f, bobSpeed);
        effect.fadeInDuration = Mathf.Max(0.01f, fadeInDuration);
        effect.sortingOrder = sortingOrder;

        effect.showDebug = showDebug;

        effect.enabled = true;

        var router = player.GetComponent<ActiveCardInputRouter>();
        if (!router) router = player.AddComponent<ActiveCardInputRouter>();

        router.RegisterFirstFree(effect);

    }
}