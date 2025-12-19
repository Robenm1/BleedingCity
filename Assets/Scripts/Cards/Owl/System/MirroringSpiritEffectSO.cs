using UnityEngine;

[CreateAssetMenu(
    fileName = "MirroringSpiritEffect",
    menuName = "Game/Cards/Effects/Owl/Mirroring Spirit"
)]
public class MirroringSpiritEffectSO : CardEffectSO
{
    [Header("Mirror Settings")]
    [Tooltip("Scale clone movement vs. player's movement speed.")]
    public float mirrorSpeedMultiplier = 1f;

    [Tooltip("Invert X axis? (left/right)")]
    public bool invertX = true;

    [Tooltip("Invert Y axis? (up/down)")]
    public bool invertY = true;

    public override void Apply(GameObject player)
    {
        var eff = player.GetComponent<MirroringSpiritEffect>();
        if (!eff) eff = player.AddComponent<MirroringSpiritEffect>();

        eff.mirrorSpeedMultiplier = Mathf.Max(0f, mirrorSpeedMultiplier);
        eff.invertX = invertX;
        eff.invertY = invertY;
        eff.enabled = true;
    }

    // NOTE: No Remove() override here because CardEffectSO doesn't define it.
    // If you later add removal support, you can implement a matching method name/signature.
}
