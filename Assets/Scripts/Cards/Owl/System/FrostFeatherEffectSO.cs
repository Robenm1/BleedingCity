using UnityEngine;

[CreateAssetMenu(fileName = "FrostFeatherEffect", menuName = "Game/Cards/Effects/Owl/Frost Feather")]
public class FrostFeatherEffectSO : CardEffectSO
{
    [Header("Chain Settings")]
    [Tooltip("Maximum targets total, including the first enemy hit. 3 = hits max 3 enemies total.")]
    public int maxBounces = 3;

    [Tooltip("Damage multiplier per chain hit relative to PlayerStats.GetDamage().")]
    public float chainDamageMultiplier = 1.0f;

    [Header("Straight Bounce")]
    public float visualSpeed = 24f;
    public float bounceDelay = 0.03f;
    public float bounceSearchRadius = 6f;
    public float lineWidth = 2.25f;

    [Range(-1f, 1f)]
    public float minForwardDot = 0.25f;

    public LayerMask enemyLayers;

    [Header("Final Stick Position")]
    public float stickBehindDistance = 0.45f;
    public float stickSideOffset = 0.15f;

    [Header("Visual")]
    public float visualScale = 1f;
    public bool hideRealFeatherDuringJump = true;
    public bool hideRealLineRendererDuringJump = true;

    public override void Apply(GameObject player)
    {
        if (!player)
            return;

        FrostFeatherEffect eff = player.GetComponent<FrostFeatherEffect>();

        if (!eff)
            eff = player.AddComponent<FrostFeatherEffect>();

        eff.maxBounces = Mathf.Clamp(maxBounces, 1, 3);
        eff.chainDamageMultiplier = Mathf.Max(0f, chainDamageMultiplier);

        eff.visualSpeed = Mathf.Max(0.1f, visualSpeed);
        eff.bounceDelay = Mathf.Max(0f, bounceDelay);
        eff.bounceSearchRadius = Mathf.Max(0.1f, bounceSearchRadius);
        eff.lineWidth = Mathf.Max(0.1f, lineWidth);
        eff.minForwardDot = Mathf.Clamp(minForwardDot, -1f, 1f);
        eff.enemyLayers = enemyLayers;

        eff.stickBehindDistance = Mathf.Max(0f, stickBehindDistance);
        eff.stickSideOffset = Mathf.Max(0f, stickSideOffset);

        eff.visualScale = Mathf.Max(0.01f, visualScale);
        eff.hideRealFeatherDuringJump = hideRealFeatherDuringJump;
        eff.hideRealLineRendererDuringJump = hideRealLineRendererDuringJump;

        eff.EnableEffect();
    }
}