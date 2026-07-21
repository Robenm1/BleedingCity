using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class RockElementStatus : MonoBehaviour
{
    [Header("Runtime")]
    [SerializeField] private int hitCount;
    [SerializeField] private bool isStunned;

    private Rigidbody2D _rb;
    private Coroutine _stunRoutine;

    private readonly List<MonoBehaviour> _disabledBehaviours = new();

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    public void RegisterRockHit(
        int hitsRequiredToStun,
        float stunDuration,
        bool resetHitsAfterStun,
        bool showDebug
    )
    {
        hitsRequiredToStun = Mathf.Max(1, hitsRequiredToStun);
        stunDuration = Mathf.Max(0.01f, stunDuration);

        hitCount++;

        if (showDebug)
            Debug.Log($"[RockElementStatus] {name} Rock hits: {hitCount}/{hitsRequiredToStun}");

        if (hitCount < hitsRequiredToStun)
            return;

        ApplyStun(stunDuration, showDebug);

        if (resetHitsAfterStun)
            hitCount = 0;
    }

    private void ApplyStun(float duration, bool showDebug)
    {
        if (_stunRoutine != null)
            StopCoroutine(_stunRoutine);

        _stunRoutine = StartCoroutine(StunRoutine(duration, showDebug));
    }

    private IEnumerator StunRoutine(float duration, bool showDebug)
    {
        isStunned = true;

        if (showDebug)
            Debug.Log($"[RockElementStatus] {name} STUN START for {duration:F1}s.");

        StopRigidbody();
        DisableEnemyControlScripts();

        float timer = duration;

        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            StopRigidbody();
            yield return null;
        }

        EnableEnemyControlScripts();

        StopRigidbody();

        isStunned = false;
        _stunRoutine = null;

        if (showDebug)
            Debug.Log($"[RockElementStatus] {name} STUN END.");
    }

    private void StopRigidbody()
    {
        if (_rb == null)
            _rb = GetComponent<Rigidbody2D>();

        if (_rb == null)
            return;

        _rb.linearVelocity = Vector2.zero;
        _rb.angularVelocity = 0f;
    }

    private void DisableEnemyControlScripts()
    {
        _disabledBehaviours.Clear();

        MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();

        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];

            if (behaviour == null)
                continue;

            if (behaviour == this)
                continue;

            if (!behaviour.enabled)
                continue;

            if (!ShouldDisableForStun(behaviour))
                continue;

            behaviour.enabled = false;
            _disabledBehaviours.Add(behaviour);
        }
    }

    private void EnableEnemyControlScripts()
    {
        for (int i = 0; i < _disabledBehaviours.Count; i++)
        {
            MonoBehaviour behaviour = _disabledBehaviours[i];

            if (behaviour != null)
                behaviour.enabled = true;
        }

        _disabledBehaviours.Clear();
    }

    private bool ShouldDisableForStun(MonoBehaviour behaviour)
    {
        string scriptName = behaviour.GetType().Name.ToLowerInvariant();

        // Never disable important health/status systems.
        if (scriptName.Contains("health")) return false;
        if (scriptName.Contains("status")) return false;
        if (scriptName.Contains("element")) return false;
        if (scriptName.Contains("mark")) return false;
        if (scriptName.Contains("damagepopup")) return false;

        // Disable common enemy movement / AI / attack scripts.
        if (scriptName.Contains("movement")) return true;
        if (scriptName.Contains("move")) return true;
        if (scriptName.Contains("ai")) return true;
        if (scriptName.Contains("chase")) return true;
        if (scriptName.Contains("follow")) return true;
        if (scriptName.Contains("attack")) return true;
        if (scriptName.Contains("shooter")) return true;
        if (scriptName.Contains("melee")) return true;
        if (scriptName.Contains("enemy")) return true;

        return false;
    }
}