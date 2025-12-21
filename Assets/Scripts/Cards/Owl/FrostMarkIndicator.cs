using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class FrostMarkIndicator : MonoBehaviour
{
    public float yOffset = 0.9f;
    public float scale = 0.7f;

    private SpriteRenderer _sr;
    private Transform _tf;
    private Coroutine _co;

    private void Ensure()
    {
        if (_tf == null)
        {
            var go = new GameObject("FrostMark");
            go.transform.SetParent(transform, false);
            _tf = go.transform;
            _tf.localPosition = new Vector3(0f, yOffset, 0f);
        }
        if (_sr == null)
        {
            _sr = _tf.GetComponent<SpriteRenderer>();
            if (_sr == null) _sr = _tf.gameObject.AddComponent<SpriteRenderer>();
            _sr.sortingOrder = 200; // above sprites
            _sr.color = Color.white;
        }
    }

    public void SetSprite(Sprite s)
    {
        Ensure();
        _sr.sprite = s;
        _tf.localScale = new Vector3(scale, scale, 1f);
        _tf.localPosition = new Vector3(0f, yOffset, 0f);
    }

    public void ShowFor(float duration)
    {
        Ensure();
        if (_co != null) StopCoroutine(_co);
        _sr.enabled = true;
        _co = StartCoroutine(HideAfter(duration));
    }

    private IEnumerator HideAfter(float t)
    {
        float time = Mathf.Max(0f, t);
        while (time > 0f)
        {
            time -= Time.deltaTime;
            yield return null;
        }
        if (_sr) _sr.enabled = false;
        _co = null;
    }
}
