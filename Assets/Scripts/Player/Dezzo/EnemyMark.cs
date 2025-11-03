using UnityEngine;
using System.Collections;

public class EnemyMark : MonoBehaviour
{
    [Tooltip("Is this enemy currently marked for priority targeting?")]
    public bool isMarked = false;

    private Coroutine markRoutine;

    public void SetMarked(float duration)
    {
        if (markRoutine != null) StopCoroutine(markRoutine);
        markRoutine = StartCoroutine(MarkFor(duration));
    }

    private IEnumerator MarkFor(float duration)
    {
        isMarked = true;
        float t = duration;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            yield return null;
        }
        isMarked = false;
        markRoutine = null;
    }
}
