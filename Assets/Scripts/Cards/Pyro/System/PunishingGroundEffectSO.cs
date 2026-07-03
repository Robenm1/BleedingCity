using UnityEngine;

[CreateAssetMenu(fileName = "PunishingGroundEffect", menuName = "Game/Cards/Effects/Pyro/Punishing Ground")]
public class PunishingGroundEffectSO : CardEffectSO
{
    [Header("Hell Bomb Charges")]
    [Tooltip("How many bombs Pyro can plant before Hell Bomb goes on cooldown.")]
    public int bombsBeforeCooldown = 2;

    public override void Apply(GameObject player)
    {
        var effect = player.GetComponent<PunishingGroundEffect>();
        if (!effect) effect = player.AddComponent<PunishingGroundEffect>();

        effect.bombsBeforeCooldown = Mathf.Max(1, bombsBeforeCooldown);

        effect.enabled = true;
    }
}