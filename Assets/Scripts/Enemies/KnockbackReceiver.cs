using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Transform))]
public class KnockbackReceiver : MonoBehaviour
{
    [Tooltip("Reduce incoming knockback (0 = none, 1 = immune).")]
    [Range(0f, 1f)] public float resistance = 0f;

    private Coroutine kbRoutine;

    public void ApplyKnockback(Vector2 dir, float distance, float duration)
    {
        if (kbRoutine != null) StopCoroutine(kbRoutine);
        kbRoutine = StartCoroutine(DoKnockback(dir.normalized, distance * (1f - resistance), duration));
    }

    private IEnumerator DoKnockback(Vector2 dir, float dist, float dur)
    {
        if (dist <= 0f || dur <= 0f) yield break;

        var follow = GetComponent<EnemyFollow>();
        bool reenableFollow = false;
        if (follow != null && follow.enabled)
        {
            follow.enabled = false;
            reenableFollow = true;
        }

        var rb = GetComponent<Rigidbody2D>();
        Vector2 start = transform.position;
        Vector2 end = start + dir * dist;

        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / dur);
            Vector2 pos = Vector2.Lerp(start, end, k);
            if (rb != null)
                rb.MovePosition(pos);
            else
                transform.position = pos;
            yield return null;
        }

        if (reenableFollow && follow != null) follow.enabled = true;
        kbRoutine = null;
    }
}
