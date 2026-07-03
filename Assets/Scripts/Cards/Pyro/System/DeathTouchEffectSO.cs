using UnityEngine;

[CreateAssetMenu(fileName = "DeathTouchEffect", menuName = "Game/Cards/Effects/Pyro/Death Touch")]
public class DeathTouchEffectSO : CardEffectSO
{
    [Header("Death Touch")]
    [Tooltip("Total damage multiplier applied before converting the hit into DoT. 1.10 = +10% damage.")]
    public float damageMultiplier = 1.10f;

    [Tooltip("How long the converted damage lasts.")]
    public float dotDuration = 3f;

    [Tooltip("How often the DoT applies damage.")]
    public float tickInterval = 0.25f;

    public override void Apply(GameObject player)
    {
        var effect = player.GetComponent<DeathTouchEffect>();
        if (!effect) effect = player.AddComponent<DeathTouchEffect>();

        effect.damageMultiplier = Mathf.Max(0f, damageMultiplier);
        effect.dotDuration = Mathf.Max(0.05f, dotDuration);
        effect.tickInterval = Mathf.Max(0.05f, tickInterval);

        effect.enabled = true;
    }
}