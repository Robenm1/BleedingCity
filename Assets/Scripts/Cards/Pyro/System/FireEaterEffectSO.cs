using UnityEngine;

[CreateAssetMenu(fileName = "FireEaterEffect", menuName = "Game/Cards/Effects/Pyro/Fire Eater")]
public class FireEaterEffectSO : CardEffectSO
{
    [Header("Dash Damage")]
    [Tooltip("Damage multiplier based on PlayerStats.GetDamage(). If Pyro has 10 damage and this is 2, dash deals 20 damage.")]
    public float damageScaling = 2f;

    [Tooltip("Small radius around Pyro used to detect enemies while dashing.")]
    public float hitRadius = 0.65f;

    [Tooltip("Enemy layers that can be damaged and passed through during dash.")]
    public LayerMask enemyLayers;

    [Header("Collision")]
    [Tooltip("If true, Pyro ignores collision with enemy layers while dashing.")]
    public bool ignoreEnemyCollisionDuringDash = true;

    [Header("Debug")]
    public bool showDebug = true;

    public override void Apply(GameObject player)
    {
        var effect = player.GetComponent<FireEaterEffect>();
        if (!effect) effect = player.AddComponent<FireEaterEffect>();

        effect.damageScaling = Mathf.Max(0f, damageScaling);
        effect.hitRadius = Mathf.Max(0.05f, hitRadius);
        effect.enemyLayers = enemyLayers;
        effect.ignoreEnemyCollisionDuringDash = ignoreEnemyCollisionDuringDash;
        effect.showDebug = showDebug;

        effect.enabled = true;
    }
}