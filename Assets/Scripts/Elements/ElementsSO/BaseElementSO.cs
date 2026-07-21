using UnityEngine;

public abstract class BaseElementSO : ScriptableObject
{
    [Header("Element Identity")]
    [Tooltip("Display name of the element.")]
    public string elementName = "New Element";

    [Tooltip("Used for VFX, UI, outlines, damage text, particles, etc.")]
    public Color elementColor = Color.white;

    public virtual void OnElementApplied(GameObject owner) { }

    public virtual void OnElementRemoved(GameObject owner) { }

    /// <summary>
    /// Called when this element holder deals direct damage.
    /// Example: Fire adds burn, Water scales part of damage with max HP.
    /// </summary>
    public virtual float ModifyDirectDamage(GameObject attacker, GameObject target, float damage)
    {
        return damage;
    }

    /// <summary>
    /// Called when this element holder deals DoT damage.
    /// </summary>
    public virtual float ModifyDotDamage(GameObject attacker, GameObject target, float totalDotDamage)
    {
        return totalDotDamage;
    }

    /// <summary>
    /// Called when this element holder receives direct damage.
    /// Example: Water reduces incoming damage when HP is low.
    /// </summary>
    public virtual float ModifyIncomingDirectDamage(GameObject holder, GameObject attacker, float damage)
    {
        return damage;
    }
}