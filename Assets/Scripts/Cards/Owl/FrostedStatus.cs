using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class FrostedStatus : MonoBehaviour
{
    private Coroutine _co;

    public void ApplySlow(EnemyFollow targetFollow, float slowFactor, float duration)
    {
        if (!targetFollow) return;
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(SlowRoutine(targetFollow, Mathf.Clamp(slowFactor, 0.1f, 1f), Mathf.Max(0f, duration)));
    }

    private IEnumerator SlowRoutine(EnemyFollow f, float factor, float dur)
    {
        float original = f.moveSpeed;
        f.moveSpeed = original * factor;

        float t = dur;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            yield return null;
        }

        // If still around, restore
        if (f) f.moveSpeed = original;
        _co = null;
    }
}
