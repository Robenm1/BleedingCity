// Assets/Scripts/Cards/Dezzo/UltimateHungerEffectSO.cs
using UnityEngine;

[CreateAssetMenu(
    fileName = "Dezzo_UltimateHunger",
    menuName = "Game/Cards/Effects/Dezzo/Ultimate Hunger",
    order = 50)]
public class UltimateHungerEffectSO : CardEffectSO
{
    [Header("Hit → Mark")]
    public int hitsToMark = 3;
    public float hitCounterDecayDelay = 4f;
    public float biteMarkDuration = 6f;

    [Header("Frenzy vs Bite-Marked target")]
    public float frenzyMoveSpeedMul = 1.25f; // sharks move faster toward marked
    public float frenzyAttackDelayMul = 0.80f; // bite sooner
    public float frenzyDamageMul = 1.15f; // hit harder

    [Header("Execute / Finisher")]
    [Range(0.01f, 0.5f)] public float executeThreshold = 0.10f;

    [Header("Visuals")]
    public Sprite biteMarkSprite;              // red sigil placed ON enemy’s body
    public Color biteMarkTint = new Color(1f, 0.15f, 0.15f, 1f);
    public Vector2 biteMarkOffset = new Vector2(0f, 0.1f);
    public int sortingOrder = 300;

    public override void Apply(GameObject player)
    {
        var runtime = player.GetComponent<UltimateHungerRuntime>();
        if (!runtime) runtime = player.AddComponent<UltimateHungerRuntime>();

        runtime.hitsToMark = Mathf.Max(1, hitsToMark);
        runtime.hitCounterDecayDelay = Mathf.Max(0.1f, hitCounterDecayDelay);
        runtime.biteMarkDuration = Mathf.Max(0.25f, biteMarkDuration);

        runtime.frenzyMoveSpeedMul = Mathf.Max(0.01f, frenzyMoveSpeedMul);
        runtime.frenzyAttackDelayMul = Mathf.Max(0.01f, frenzyAttackDelayMul);
        runtime.frenzyDamageMul = Mathf.Max(0.01f, frenzyDamageMul);

        runtime.executeThreshold = Mathf.Clamp01(executeThreshold);

        runtime.biteMarkSprite = biteMarkSprite;
        runtime.biteMarkTint = biteMarkTint;
        runtime.biteMarkOffset = biteMarkOffset;
        runtime.sortingOrder = sortingOrder;
    }
}
