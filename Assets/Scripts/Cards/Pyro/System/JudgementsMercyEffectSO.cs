using UnityEngine;

[CreateAssetMenu(fileName = "JudgementsMercyEffect", menuName = "Game/Cards/Effects/Pyro/Judgement's Mercy")]
public class JudgementsMercyEffectSO : CardEffectSO
{
    [Header("Judgement Heal")]
    [Tooltip("Percent of Pyro's current HP restored when killing an enemy marked with Judgement. 0.01 = 1%.")]
    public float healCurrentHpPercent = 0.01f;

    [Tooltip("If true, Pyro cannot heal above max HP.")]
    public bool clampToMaxHp = true;

    [Header("Debug")]
    public bool showDebug = true;

    public override void Apply(GameObject player)
    {
        var effect = player.GetComponent<JudgementsMercyEffect>();
        if (!effect) effect = player.AddComponent<JudgementsMercyEffect>();

        effect.healCurrentHpPercent = Mathf.Max(0f, healCurrentHpPercent);
        effect.clampToMaxHp = clampToMaxHp;
        effect.showDebug = showDebug;

        effect.enabled = true;
    }
}