using System;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Reversibly multiplies a specific cooldown (field or property) on a specific ability component by name.
/// No changes to the ability script are required. Uses reflection safely.
/// </summary>
[DisallowMultipleComponent]
public class AbilityCooldownMultiplierApplier : MonoBehaviour
{
    [Serializable]
    private class Entry
    {
        public string componentName;
        public string memberName;
        public float multiplier = 1f;

        // cached
        [NonSerialized] public Component target;
        [NonSerialized] public MemberInfo member;
        [NonSerialized] public float originalValue;
        [NonSerialized] public bool applied;
    }

    [Tooltip("Active applied entries.")]
    [SerializeField] private System.Collections.Generic.List<Entry> _entries = new();

    /// <summary>
    /// Call from a CardEffectSO to apply (or update) a multiplier on a given component/member.
    /// </summary>
    public void ApplyFor(string targetComponentName, string cooldownMemberName, float multiplier)
    {
        if (string.IsNullOrWhiteSpace(targetComponentName) || string.IsNullOrWhiteSpace(cooldownMemberName))
        {
            Debug.LogWarning("[AbilityCooldownMultiplierApplier] Invalid component/member names.");
            return;
        }

        // If an entry for the same pair exists, first revert it then update.
        var existing = _entries.Find(e => e.componentName == targetComponentName && e.memberName == cooldownMemberName);
        if (existing != null)
        {
            RevertEntry(existing);
            existing.multiplier = multiplier;
            ApplyEntry(existing);
            return;
        }

        var entry = new Entry
        {
            componentName = targetComponentName,
            memberName = cooldownMemberName,
            multiplier = multiplier
        };

        _entries.Add(entry);
        ApplyEntry(entry);
    }

    private void OnDisable()
    {
        // Revert all on disable (scene change / run end).
        foreach (var e in _entries)
            RevertEntry(e);
    }

    private void ApplyEntry(Entry e)
    {
        if (e.applied) return;

        // Find target component by name (on this GameObject or children, if needed).
        e.target = FindComponentByName(e.componentName);
        if (!e.target)
        {
            Debug.LogWarning($"[AbilityCooldownMultiplierApplier] Component '{e.componentName}' not found on '{name}'.");
            return;
        }

        // Find member (field or property).
        e.member = FindMember(e.target.GetType(), e.memberName);
        if (e.member == null)
        {
            Debug.LogWarning($"[AbilityCooldownMultiplierApplier] Member '{e.memberName}' not found on '{e.componentName}'.");
            return;
        }

        // Read original value (float) and write multiplied value.
        if (!TryGetFloat(e.target, e.member, out float current))
        {
            Debug.LogWarning($"[AbilityCooldownMultiplierApplier] Member '{e.memberName}' is not a readable float.");
            return;
        }

        e.originalValue = current;

        float newValue = current * e.multiplier;
        if (!TrySetFloat(e.target, e.member, newValue))
        {
            Debug.LogWarning($"[AbilityCooldownMultiplierApplier] Member '{e.memberName}' is not a writable float.");
            return;
        }

        e.applied = true;
    }

    private void RevertEntry(Entry e)
    {
        if (!e.applied) return;
        if (e.target == null || e.member == null) { e.applied = false; return; }

        // Restore original value.
        TrySetFloat(e.target, e.member, e.originalValue);
        e.applied = false;
    }

    private Component FindComponentByName(string componentName)
    {
        // Try on self
        var comps = GetComponents<MonoBehaviour>();
        foreach (var c in comps)
            if (c != null && c.GetType().Name == componentName)
                return c;

        // Optional: look in children if your ability sits on a child object
        var childComps = GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var c in childComps)
            if (c != null && c.GetType().Name == componentName)
                return c;

        return null;
    }

    private static MemberInfo FindMember(Type type, string memberName)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        // Field?
        var f = type.GetField(memberName, flags);
        if (f != null) return f;

        // Property?
        var p = type.GetProperty(memberName, flags);
        if (p != null) return p;

        return null;
    }

    private static bool TryGetFloat(Component target, MemberInfo member, out float value)
    {
        value = 0f;
        switch (member)
        {
            case FieldInfo fi:
                if (fi.FieldType == typeof(float))
                {
                    value = (float)fi.GetValue(target);
                    return true;
                }
                break;
            case PropertyInfo pi:
                if (pi.PropertyType == typeof(float) && pi.CanRead)
                {
                    value = (float)pi.GetValue(target, null);
                    return true;
                }
                break;
        }
        return false;
    }

    private static bool TrySetFloat(Component target, MemberInfo member, float value)
    {
        switch (member)
        {
            case FieldInfo fi:
                if (fi.FieldType == typeof(float))
                {
                    fi.SetValue(target, value);
                    return true;
                }
                break;
            case PropertyInfo pi:
                if (pi.PropertyType == typeof(float) && pi.CanWrite)
                {
                    pi.SetValue(target, value, null);
                    return true;
                }
                break;
        }
        return false;
    }
}
