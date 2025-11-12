using UnityEngine;

public abstract class CardEffectSO : ScriptableObject
{
    /// <summary>Apply this effect to the spawned player object.</summary>
    public abstract void Apply(GameObject player);
}
