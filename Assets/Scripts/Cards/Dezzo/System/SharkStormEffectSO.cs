using UnityEngine;

[CreateAssetMenu(fileName = "SharkStormEffect", menuName = "Game/Cards/Effects/Dezzo/Shark Storm")]
public class SharkStormEffectSO : CardEffectSO
{
    [Header("Starting Setup")]
    public int startSharks = 1;
    [Range(0.05f, 2f)] public float damageScaleOnStart = 0.7f;

    [Header("Progression")]
    public int killsPerNewShark = 5;
    public int maxSharks = 0; // 0 = unlimited

    public override void Apply(GameObject player)
    {
        // Attach (or reuse) the runtime effect component and feed the config
        var eff = player.GetComponent<SharkStormEffect>();
        if (!eff) eff = player.AddComponent<SharkStormEffect>();

        eff.startSharks = startSharks;
        eff.damageScaleOnStart = damageScaleOnStart;
        eff.killsPerNewShark = killsPerNewShark;
        eff.maxSharks = maxSharks;

        // If the effect component is already active, it will use these values.
        // Otherwise its Start() will run with these configured values.
    }
}
