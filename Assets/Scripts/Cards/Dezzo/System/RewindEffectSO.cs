// Assets/Scripts/Cards/Dezzo/RewindEffectSO.cs
using UnityEngine;

[CreateAssetMenu(
    fileName = "Dezzo_Rewind",
    menuName = "Game/Cards/Effects/Dezzo/Rewind",
    order = 51)]
public class RewindEffectSO : CardEffectSO
{
    [Header("Revive Settings")]
    [Tooltip("Seconds the revive animation/lock lasts.")]
    public float reviveDuration = 2.0f;

    [Range(0.05f, 1f)]
    [Tooltip("HP percent restored after revive (0.5 = 50%).")]
    public float healPercent = 0.5f;

    [Header("Behavior During Revive")]
    [Tooltip("Disable DezzoShark behavior during revive.")]
    public bool freezeSharks = true;

    [Tooltip("Disable player movement/controls during revive.")]
    public bool lockControls = true;

    [Tooltip("Temporary invulnerability during revive.")]
    public bool invulnerableWhileReviving = true;

    [Tooltip("Optional layer set while reviving (e.g., 'IgnoreEnemies'). Leave empty to skip.")]
    public string invulnerableLayerName = "";

    public override void Apply(GameObject player)
    {
        var r = player.GetComponent<RewindRuntime>();
        if (!r) r = player.AddComponent<RewindRuntime>();

        r.reviveDuration = Mathf.Max(0.2f, reviveDuration);
        r.healPercent = Mathf.Clamp01(healPercent);
        r.freezeSharks = freezeSharks;
        r.lockControls = lockControls;
        r.invulnerableWhileReviving = invulnerableWhileReviving;
        r.invulnerableLayerName = invulnerableLayerName;
    }
}
