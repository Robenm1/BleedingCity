// Assets/Scripts/Util/OnDestroyCallback.cs
using UnityEngine;

/// <summary>
/// Attach at runtime, call Init(() => { ... }) to run code exactly once when this GameObject is destroyed.
/// </summary>
public class OnDestroyCallback : MonoBehaviour
{
    private System.Action _callback;

    /// <summary>Set the callback to be invoked on OnDestroy.</summary>
    public OnDestroyCallback Init(System.Action callback)
    {
        _callback = callback;
        return this;
    }

    private void OnDestroy()
    {
        // Try/catch so destruction never throws
        try { _callback?.Invoke(); } catch { }
        _callback = null;
    }
}
