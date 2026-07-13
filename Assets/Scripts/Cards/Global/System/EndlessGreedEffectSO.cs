using UnityEngine;

[CreateAssetMenu(fileName = "EndlessGreedEffect", menuName = "Game/Cards/Effects/Global/Endless Greed")]
public class EndlessGreedEffectSO : CardEffectSO
{
    [Header("Endless Greed")]
    [Tooltip("Flat damage gained per coin collected. 1 = +1 damage.")]
    public float flatDamagePerStack = 1f;

    [Tooltip("Move speed gained per coin collected as percent of original move speed. 1 = +1% move speed.")]
    public float moveSpeedPercentPerStack = 1f;

    [Tooltip("If true, stacks have no limit.")]
    public bool infiniteStacks = true;

    [Tooltip("Only used if Infinite Stacks is false.")]
    public int maxStacks = 999;

    [Header("Reset")]
    [Tooltip("Lose all Endless Greed stacks when the player takes damage.")]
    public bool resetOnDamageTaken = true;

    [Header("Debug")]
    public bool showDebug = true;

    public override void Apply(GameObject player)
    {
        var effect = player.GetComponent<EndlessGreedEffect>();
        if (!effect) effect = player.AddComponent<EndlessGreedEffect>();

        effect.flatDamagePerStack = Mathf.Max(0f, flatDamagePerStack);
        effect.moveSpeedPercentPerStack = Mathf.Max(0f, moveSpeedPercentPerStack);

        effect.infiniteStacks = infiniteStacks;
        effect.maxStacks = Mathf.Max(1, maxStacks);

        effect.resetOnDamageTaken = resetOnDamageTaken;
        effect.showDebug = showDebug;

        effect.enabled = true;
    }
}