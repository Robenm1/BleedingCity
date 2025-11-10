using UnityEngine;
using System;

public static class PlayerLocator
{
    public static Transform Current { get; private set; }

    public static event Action<Transform> OnPlayerChanged;

    public static void Set(Transform t)
    {
        Current = t;
        OnPlayerChanged?.Invoke(Current);
    }
}
