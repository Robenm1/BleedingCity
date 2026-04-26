using UnityEngine;

/// <summary>
/// Placed on the ground by PyroAbility2.
/// Charges over time, turning from yellow to red.
/// Only explodes on enemy contact once fully charged (red).
/// </summary>
public class HellBomb : MonoBehaviour
{
    [Header("Charge")]
    [Tooltip("Seconds to go from yellow (safe) to red (armed).")]
    public float chargeTime = 2f;

    [Header("Explosion")]
    [Tooltip("Radius in which enemies get marked when the bomb explodes.")]
    public float explosionRadius = 4f;

    [Tooltip("How long the Hell's Justice mark lasts on each enemy.")]
    public float markDuration = 5f;

    [Tooltip("Damage multiplier applied to summon hits on marked enemies.")]
    public float markDamageMultiplier = 1.5f;

    [Header("Mark Icon")]
    [Tooltip("Sprite displayed above marked enemies. Assign HellsJusticeUI here.")]
    public Sprite markIconSprite;

    [Header("Layers")]
    public LayerMask enemyLayers;

    // ── State ──────────────────────────────────────────────────────────────
    private float _chargeTimer = 0f;
    private bool _armed = false;
    private bool _exploded = false;

    private SpriteRenderer _sr;

    private static readonly Color ColorYellow = new Color(1f, 0.92f, 0.02f, 1f);
    private static readonly Color ColorRed    = new Color(1f, 0.12f, 0.02f, 1f);

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (_sr != null)
            _sr.color = ColorYellow;
    }

    private void Update()
    {
        if (_armed || _exploded) return;

        _chargeTimer += Time.deltaTime;
        float t = Mathf.Clamp01(_chargeTimer / chargeTime);

        if (_sr != null)
            _sr.color = Color.Lerp(ColorYellow, ColorRed, t);

        if (t >= 1f)
            _armed = true;
    }

    // ── Trigger ────────────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_armed || _exploded) return;
        if (((1 << other.gameObject.layer) & enemyLayers) == 0) return;

        Explode();
    }

    // ── Internals ──────────────────────────────────────────────────────────

    private void Explode()
    {
        _exploded = true;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, enemyLayers);

        foreach (var hit in hits)
        {
            if (hit == null) continue;

            var mark = hit.GetComponent<HellsJusticeMark>();
            if (mark == null)
                mark = hit.gameObject.AddComponent<HellsJusticeMark>();

            mark.summonDamageMultiplier = markDamageMultiplier;
            mark.iconSprite             = markIconSprite;
            mark.Apply(markDuration);
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = _armed
            ? new Color(1f, 0.1f, 0f, 0.35f)
            : new Color(1f, 0.9f, 0f, 0.35f);

        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
