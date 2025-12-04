// Assets/Scripts/Util/SimpleLifetime.cs
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Destroys (or disables) the GameObject after a countdown.
/// Call ResetLifetime() to restart, SetLifetime() to change during runtime,
/// or KillNow() to end immediately.
/// </summary>
public class SimpleLifetime : MonoBehaviour
{
    [Header("Timing")]
    [Tooltip("Seconds until expiry.")]
    public float lifetime = 8f;

    [Tooltip("Use unscaled time (ignores Time.timeScale).")]
    public bool useUnscaledTime = false;

    [Header("On Expire")]
    [Tooltip("If true, Destroy(this.gameObject) on expiry. If false, just SetActive(false).")]
    public bool destroyOnExpire = true;

    [Tooltip("Optional event fired exactly once when the timer expires.")]
    public UnityEvent onExpired;

    private float _timeLeft;
    private bool _running;

    private void OnEnable()
    {
        _timeLeft = Mathf.Max(0f, lifetime);
        _running = true;
    }

    private void Update()
    {
        if (!_running) return;

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        _timeLeft -= dt;
        if (_timeLeft <= 0f)
        {
            _running = false;
            TryExpire();
        }
    }

    /// <summary>Restart the countdown using the current 'lifetime' value.</summary>
    public void ResetLifetime()
    {
        _timeLeft = Mathf.Max(0f, lifetime);
        _running = true;
        if (!gameObject.activeSelf) gameObject.SetActive(true);
    }

    /// <summary>Change the remaining time (and optionally start running if it was stopped).</summary>
    public void SetLifetime(float seconds, bool restart = true)
    {
        lifetime = Mathf.Max(0f, seconds);
        if (restart) ResetLifetime();
        else _timeLeft = Mathf.Max(0f, seconds);
    }

    /// <summary>Immediately trigger expiry behavior.</summary>
    public void KillNow()
    {
        _running = false;
        _timeLeft = 0f;
        TryExpire();
    }

    private void TryExpire()
    {
        // Fire event once
        try { onExpired?.Invoke(); } catch { /* ignore user event exceptions */ }

        if (destroyOnExpire)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
