using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerStats))]
public class SandRepulseAbility : MonoBehaviour
{
    [Header("Input (Optional)")]
    [Tooltip("If assigned, pressing this action triggers the ability. Otherwise, call Activate() from your code.")]
    public InputActionReference abilityAction;

    [Header("Effect")]
    [Tooltip("Radius around Dezzo that gets pushed/marked.")]
    public float radius = 3f;

    [Tooltip("How far enemies are pushed back.")]
    public float knockbackDistance = 2.5f;

    [Tooltip("How long the push lasts (seconds).")]
    public float knockbackDuration = 0.15f;

    [Tooltip("How long enemies are marked for sharks to prioritize.")]
    public float markDuration = 3f;

    [Tooltip("Which layers count as enemies.")]
    public LayerMask enemyLayers;

    [Header("Cooldown")]
    [Tooltip("Base cooldown before cooldownMultiplier.")]
    public float baseCooldown = 3f;

    [Header("FX (optional)")]
    public GameObject castVfxPrefab;

    [Header("UI / Debug")]
    [Tooltip("If ON, shows a brief circle pulse for the push radius.")]
    public bool showCircle = true;

    private PlayerStats stats;
    private float cooldownTimer = 0f;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
    }

    private void OnEnable()
    {
        if (abilityAction != null && abilityAction.action != null)
            abilityAction.action.performed += OnAbilityPerformed;
    }

    private void OnDisable()
    {
        if (abilityAction != null && abilityAction.action != null)
            abilityAction.action.performed -= OnAbilityPerformed;
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer < 0f) cooldownTimer = 0f;
        }
    }

    private void OnAbilityPerformed(InputAction.CallbackContext ctx)
    {
        Activate();
    }

    /// <summary>Call this from your PlayerControls when Ability1 is pressed if you don't use InputActionReference.</summary>
    public void Activate()
    {
        if (cooldownTimer > 0f) return;

        // cooldown respects PlayerStats cooldownMultiplier (e.g., 0.8 = faster CDs)
        float cd = baseCooldown * Mathf.Max(0.05f, stats.GetCooldownMultiplier());
        cooldownTimer = cd;

        Vector2 center = transform.position;
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, enemyLayers);

        // optional VFX
        if (castVfxPrefab != null)
        {
            Instantiate(castVfxPrefab, transform.position, Quaternion.identity);
        }

        for (int i = 0; i < hits.Length; i++)
        {
            var col = hits[i];
            if (col == null) continue;

            // Only act on enemies that can take damage / be targeted
            EnemyHealth eh = col.GetComponent<EnemyHealth>();
            if (eh == null) continue;

            // Push away
            Vector2 toEnemy = ((Vector2)col.transform.position - center);
            Vector2 dir = toEnemy.sqrMagnitude > 0.0001f ? toEnemy.normalized : Random.insideUnitCircle.normalized;
            var kb = col.GetComponent<KnockbackReceiver>();
            if (kb == null) kb = col.gameObject.AddComponent<KnockbackReceiver>();
            kb.ApplyKnockback(dir, knockbackDistance, knockbackDuration);

            // Mark for sharks
            var mark = col.GetComponent<EnemyMark>();
            if (mark == null) mark = col.gameObject.AddComponent<EnemyMark>();
            mark.SetMarked(markDuration);
        }

        // Show pulse circle if enabled
        if (showCircle)
            GetComponent<RangeCircles>()?.ShowRepulsePulse(radius);
    }

    private void OnDrawGizmosSelected()
    {
        // helpful editor viz
        Gizmos.color = new Color(0.9f, 0.8f, 0.3f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
