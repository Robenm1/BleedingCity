using UnityEngine;

[DisallowMultipleComponent]
public class FrostFeatherEffect : MonoBehaviour
{
    [Header("Chain Settings")]
    [Tooltip("Maximum targets total, including the first enemy hit. 3 = hits max 3 enemies total.")]
    public int maxBounces = 3;

    [Tooltip("Damage multiplier applied to each chain hit relative to PlayerStats.GetDamage().")]
    public float chainDamageMultiplier = 1.0f;

    [Header("Straight Bounce")]
    [Tooltip("How fast the feather visual moves between enemies.")]
    public float visualSpeed = 24f;

    [Tooltip("Small delay between bounces.")]
    public float bounceDelay = 0.03f;

    [Tooltip("Maximum distance from one enemy to the next. Prevents across-map jumps.")]
    public float bounceSearchRadius = 6f;

    [Tooltip("The feather only jumps forward inside this corridor.")]
    public float lineWidth = 2.25f;

    [Tooltip("Higher = stricter forward direction. Good values: 0.25 to 0.45.")]
    [Range(-1f, 1f)]
    public float minForwardDot = 0.25f;

    [Tooltip("Enemy layers used by the ricochet search.")]
    public LayerMask enemyLayers;

    [Header("Final Stick Position")]
    [Tooltip("How far behind the last enemy the real feather sticks.")]
    public float stickBehindDistance = 0.45f;

    [Tooltip("Small side offset so the feather does not hide perfectly under the enemy.")]
    public float stickSideOffset = 0.15f;

    [Header("Visual")]
    [Tooltip("Keep this at 1 for normal feather size.")]
    public float visualScale = 1f;

    [Tooltip("Hide the real feather renderers while the visual copy jumps.")]
    public bool hideRealFeatherDuringJump = true;

    [Tooltip("Hide the original LineRenderer too while bouncing.")]
    public bool hideRealLineRendererDuringJump = true;

    [Header("Performance")]
    [Tooltip("How often the effect checks for newly spawned feathers.")]
    public float scanInterval = 0.12f;

    private bool _isActive;
    private float _scanTimer;

    public void EnableEffect()
    {
        _isActive = true;
        _scanTimer = 0f;
    }

    public void DisableEffect()
    {
        _isActive = false;
    }

    private void OnDisable()
    {
        DisableEffect();
    }

    private void Update()
    {
        if (!_isActive)
            return;

        _scanTimer -= Time.deltaTime;

        if (_scanTimer > 0f)
            return;

        _scanTimer = Mathf.Max(0.02f, scanInterval);
        PatchNewFeathers();
    }

    private void PatchNewFeathers()
    {
        SnowOwlFeather[] feathers = FindObjectsOfType<SnowOwlFeather>();

        for (int i = 0; i < feathers.Length; i++)
        {
            SnowOwlFeather feather = feathers[i];

            if (feather == null)
                continue;

            if (!feather.gameObject.scene.IsValid())
                continue;

            FeatherChainOnHit chain = feather.GetComponent<FeatherChainOnHit>();

            if (chain == null)
                chain = feather.gameObject.AddComponent<FeatherChainOnHit>();

            chain.owner = gameObject;

            chain.maxBounces = Mathf.Clamp(maxBounces, 1, 3);
            chain.damageMultiplier = Mathf.Max(0f, chainDamageMultiplier);

            chain.bounceDelay = Mathf.Max(0f, bounceDelay);
            chain.bounceSearchRadius = Mathf.Max(0.1f, bounceSearchRadius);
            chain.enemyLayers = enemyLayers;

            chain.lineWidth = Mathf.Max(0.1f, lineWidth);
            chain.minForwardDot = Mathf.Clamp(minForwardDot, -1f, 1f);

            chain.visualSpeed = Mathf.Max(0.1f, visualSpeed);
            chain.visualScale = Mathf.Max(0.01f, visualScale);

            chain.stickBehindDistance = Mathf.Max(0f, stickBehindDistance);
            chain.stickSideOffset = Mathf.Max(0f, stickSideOffset);

            chain.hideRealFeatherDuringJump = hideRealFeatherDuringJump;
            chain.hideRealLineRendererDuringJump = hideRealLineRendererDuringJump;
        }
    }
}