// Assets/Scripts/Enemies/EnemyBiteMark.cs
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyBiteMark : MonoBehaviour, IMarkDisplay
{
    [Header("State")]
    public bool isActive;
    public float remaining;

    [Header("Frenzy vs this enemy")]
    public float moveMul = 1f;
    public float attackDelayMul = 1f;
    public float damageMul = 1f;

    [Header("Execute")]
    public float executeThreshold = 0.10f;

    [Header("Visual")]
    public SpriteRenderer sigilRenderer;
    public int sortingOrder = 300;

    private EnemyHealth _hp;

    // ---- IMarkDisplay ----
    /// <inheritdoc/>
    public bool IsMarkVisible => isActive && sigilRenderer != null && sigilRenderer.enabled;

    /// <inheritdoc/>
    public SpriteRenderer MarkSpriteRenderer => sigilRenderer;

    private void Awake()
    {
        _hp = GetComponent<EnemyHealth>();
        // Ensure the centralised layout controller is present on this enemy.
        MarkDisplayController.EnsureOn(gameObject);
        EnsureSigil();
        HideSigil();
    }

    private void Update()
    {
        if (!isActive) return;

        remaining -= Time.deltaTime;
        if (remaining <= 0f) { Deactivate(); return; }

        // Position and scale are handled by MarkDisplayController each LateUpdate.
    }

    /// <summary>Activates the bite mark with the given parameters.</summary>
    public void Apply(
        float duration,
        float frenzyMoveMul, float frenzyAtkDelayMul, float frenzyDmgMul,
        float execThreshold,
        Sprite sigilSprite, Color tint, int sortingOrder,
        float sigilScale)
    {
        this.remaining = Mathf.Max(0.1f, duration);
        this.moveMul = Mathf.Max(0.01f, frenzyMoveMul);
        this.attackDelayMul = Mathf.Max(0.01f, frenzyAtkDelayMul);
        this.damageMul = Mathf.Max(0.01f, frenzyDmgMul);
        this.executeThreshold = Mathf.Clamp01(execThreshold);
        this.sortingOrder = sortingOrder;

        EnsureSigil();
        if (sigilRenderer)
        {
            sigilRenderer.sprite = sigilSprite;
            sigilRenderer.color = tint;
            sigilRenderer.sortingOrder = sortingOrder;
            sigilRenderer.enabled = true;
            // Position and scale are set by MarkDisplayController each LateUpdate.
        }

        isActive = true;
    }

    /// <summary>Deactivates the bite mark and hides its sigil.</summary>
    public void Deactivate()
    {
        isActive = false;
        remaining = 0f;
        HideSigil();
    }

    /// <summary>Returns true if the enemy should be executed at its current HP.</summary>
    public bool ShouldExecute(float currentHP, float maxHP)
    {
        if (!isActive || maxHP <= 0f) return false;
        return (currentHP / maxHP) <= executeThreshold;
    }

    private void EnsureSigil()
    {
        if (sigilRenderer != null) return;
        var go = new GameObject("BiteMarkSigil");
        go.transform.SetParent(transform, worldPositionStays: true);
        sigilRenderer = go.AddComponent<SpriteRenderer>();
        sigilRenderer.sortingOrder = sortingOrder;
        sigilRenderer.enabled = false;
    }

    private void HideSigil()
    {
        if (sigilRenderer) sigilRenderer.enabled = false;
    }
}
