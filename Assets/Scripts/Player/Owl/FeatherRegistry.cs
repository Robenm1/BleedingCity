// Assets/Scripts/Player/SnowOwl/FeatherRegistry.cs
using System.Collections.Generic;
using UnityEngine;

public static class FeatherRegistry
{
    // ownerId -> feathers
    private static readonly Dictionary<int, List<SnowOwlFeather>> _byOwner = new();

    public static void Register(int ownerId, SnowOwlFeather f)
    {
        if (!_byOwner.TryGetValue(ownerId, out var list))
        {
            list = new List<SnowOwlFeather>(32);
            _byOwner[ownerId] = list;
        }
        if (f && !list.Contains(f)) list.Add(f);
    }

    public static void Unregister(int ownerId, SnowOwlFeather f)
    {
        if (ownerId == 0 || f == null) return;
        if (_byOwner.TryGetValue(ownerId, out var list))
        {
            list.Remove(f);
            if (list.Count == 0) _byOwner.Remove(ownerId);
        }
    }

    public static List<SnowOwlFeather> GetAll(int ownerId)
    {
        if (_byOwner.TryGetValue(ownerId, out var list)) return list;
        return null;
    }
}
