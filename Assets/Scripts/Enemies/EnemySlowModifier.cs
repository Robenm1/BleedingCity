using UnityEngine;
using System.Collections.Generic;

public class EnemySlowModifier : MonoBehaviour
{
    private EnemyFollow follow;
    private float baseMoveSpeed;
    private bool capturedBase = false;

    // track active slows (endTime, factor)
    private readonly List<(float endTime, float factor)> slows = new();

    private void Awake()
    {
        follow = GetComponent<EnemyFollow>();
        if (follow == null)
            Debug.LogWarning("[EnemySlowModifier] EnemyFollow not found on enemy.");
    }

    public void Apply(float factor, float duration)
    {
        if (follow == null) return;
        if (!capturedBase)
        {
            baseMoveSpeed = follow.moveSpeed;
            capturedBase = true;
        }

        float endTime = Time.time + Mathf.Max(0.01f, duration);
        slows.Add((endTime, Mathf.Clamp(factor, 0.05f, 1f)));
        Recompute();
    }

    public void CleanupExpired()
    {
        if (slows.Count == 0 || follow == null) return;
        bool changed = false;
        for (int i = slows.Count - 1; i >= 0; i--)
        {
            if (Time.time >= slows[i].endTime)
            {
                slows.RemoveAt(i);
                changed = true;
            }
        }
        if (changed) Recompute();
    }

    private void Update()
    {
        if (slows.Count == 0) return;
        CleanupExpired();
    }

    private void Recompute()
    {
        if (follow == null) return;

        if (slows.Count == 0)
        {
            // restore base
            if (capturedBase) follow.moveSpeed = baseMoveSpeed;
            return;
        }

        // pick strongest slow (smallest factor)
        float minFactor = 1f;
        for (int i = 0; i < slows.Count; i++)
            if (slows[i].factor < minFactor) minFactor = slows[i].factor;

        follow.moveSpeed = (capturedBase ? baseMoveSpeed : follow.moveSpeed) * minFactor;
    }

    private void OnDisable()
    {
        // restore on disable
        if (follow != null && capturedBase)
            follow.moveSpeed = baseMoveSpeed;
    }
}
