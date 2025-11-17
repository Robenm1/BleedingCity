using UnityEngine;

[CreateAssetMenu(fileName = "HealingDuneEffect", menuName = "Game/Cards/Effects/Dezzo/Healing Dune")]
public class HealingDuneEffectSO : CardEffectSO
{
    public float healPerSecond = 6f;
    public bool useUnscaledTime = false;

    public override void Apply(GameObject player)
    {
        var eff = player.GetComponent<HealingDuneEffect>();
        if (!eff) eff = player.AddComponent<HealingDuneEffect>();

        eff.healPerSecond = healPerSecond;
        eff.useUnscaledTime = useUnscaledTime;
    }
}
