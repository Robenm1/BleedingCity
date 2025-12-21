using UnityEngine;

[DisallowMultipleComponent]
public class WinterBombOnClone : MonoBehaviour
{
    [HideInInspector] public WinterBombEffect owner;

    [HideInInspector] public LayerMask enemyLayers;
    [HideInInspector] public float explosionRadius = 3.5f;
    [HideInInspector] public float explosionDamage = 35f;

    [HideInInspector] public float slowFactor = 0.6f;
    [HideInInspector] public float slowDuration = 4f;

    [HideInInspector] public float vulnerabilityMultiplier = 1.25f;
    [HideInInspector] public float vulnerabilityDuration = 4f;

    [HideInInspector] public Sprite frostMarkSprite;
    [HideInInspector] public float markYOffset = 0.9f;
    [HideInInspector] public float markScale = 0.7f;

    private bool _exploded = false;

    private void OnDestroy()
    {
        if (_exploded) return; // safety vs. multiple destroys
        _exploded = true;

        ExplodeAndFrost();
    }

    private void ExplodeAndFrost()
    {
        Vector2 center = transform.position;
        var hits = Physics2D.OverlapCircleAll(center, explosionRadius, enemyLayers);
        if (hits == null || hits.Length == 0) return;

        for (int i = 0; i < hits.Length; i++)
        {
            var col = hits[i];
            if (!col) continue;

            var eh = col.GetComponent<EnemyHealth>();
            if (eh)
            {
                // Flat explosion damage
                eh.TakeDamage(explosionDamage);

                // Apply vulnerability (extra damage taken) if EnemyHealth supports it
                eh.ApplyVulnerability(vulnerabilityMultiplier, vulnerabilityDuration);
            }

            // Apply slow by temporarily reducing EnemyFollow.moveSpeed
            var follow = col.GetComponent<EnemyFollow>();
            if (follow)
            {
                var slow = col.GetComponent<FrostedStatus>();
                if (!slow) slow = col.gameObject.AddComponent<FrostedStatus>();

                slow.ApplySlow(follow, slowFactor, slowDuration);
            }

            // Show frost mark above enemy
            if (frostMarkSprite != null)
            {
                var mark = col.GetComponent<FrostMarkIndicator>();
                if (!mark) mark = col.gameObject.AddComponent<FrostMarkIndicator>();

                mark.SetSprite(frostMarkSprite);
                mark.yOffset = markYOffset;
                mark.scale = markScale;
                mark.ShowFor(slowDuration);
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 0.9f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
#endif
}
