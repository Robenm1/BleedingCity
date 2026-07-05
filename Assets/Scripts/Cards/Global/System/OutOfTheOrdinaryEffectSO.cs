using UnityEngine;

[CreateAssetMenu(fileName = "OutOfTheOrdinaryEffect", menuName = "Game/Cards/Effects/Global/Out of the Ordinary")]
public class OutOfTheOrdinaryEffectSO : CardEffectSO
{
    [Header("Out of the Ordinary")]
    [Tooltip("How long after the first dash the player can dash again before cooldown begins.")]
    public float extraDashWindowDuration = 0.8f;

    [Header("Debug")]
    public bool showDebug = true;

    public override void Apply(GameObject player)
    {
        var effect = player.GetComponent<OutOfTheOrdinaryEffect>();
        if (!effect) effect = player.AddComponent<OutOfTheOrdinaryEffect>();

        effect.extraDashWindowDuration = Mathf.Max(0.05f, extraDashWindowDuration);
        effect.showDebug = showDebug;

        effect.enabled = true;
    }
}