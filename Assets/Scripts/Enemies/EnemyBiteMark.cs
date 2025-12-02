// Assets/Scripts/Cards/Dezzo/EnemyBiteMark.cs
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyBiteMark : MonoBehaviour
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
    public Vector2 offset;
    public int sortingOrder = 300;
    [Tooltip("Uniform scale applied to the sigil sprite.")]
    public float sigilScale = 1f;

    private EnemyHealth _hp;

    private void Awake()
    {
        _hp = GetComponent<EnemyHealth>();
        EnsureSigil();
        HideSigil();
    }

    private void Update()
    {
        if (!isActive) return;

        remaining -= Time.deltaTime;
        if (remaining <= 0f) { Deactivate(); return; }

        if (sigilRenderer)
        {
            sigilRenderer.transform.position = (Vector2)transform.position + offset;
            sigilRenderer.transform.localScale = Vector3.one * Mathf.Max(0.05f, sigilScale);
        }
    }

    public void Apply(
        float duration,
        float frenzyMoveMul, float frenzyAtkDelayMul, float frenzyDmgMul,
        float execThreshold,
        Sprite sigilSprite, Color tint, Vector2 offset, int sortingOrder,
        float sigilScale)
    {
        this.remaining = Mathf.Max(0.1f, duration);
        this.moveMul = Mathf.Max(0.01f, frenzyMoveMul);
        this.attackDelayMul = Mathf.Max(0.01f, frenzyAtkDelayMul);
        this.damageMul = Mathf.Max(0.01f, frenzyDmgMul);
        this.executeThreshold = Mathf.Clamp01(execThreshold);
        this.offset = offset;
        this.sortingOrder = sortingOrder;
        this.sigilScale = Mathf.Max(0.05f, sigilScale);

        EnsureSigil();
        if (sigilRenderer)
        {
            sigilRenderer.sprite = sigilSprite;
            sigilRenderer.color = tint;
            sigilRenderer.sortingOrder = sortingOrder;
            sigilRenderer.transform.position = (Vector2)transform.position + offset;
            sigilRenderer.transform.localScale = Vector3.one * this.sigilScale;
            sigilRenderer.enabled = true;
        }

        isActive = true;
    }

    public void Deactivate()
    {
        isActive = false;
        remaining = 0f;
        HideSigil();
    }

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
