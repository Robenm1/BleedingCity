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

    public virtual float ModifyDirectDamage(GameObject attacker, GameObject target, float damage)
    {
        return damage;
    }

    public virtual float ModifyDotDamage(GameObject attacker, GameObject target, float totalDotDamage)
    {
        return totalDotDamage;
    }

    public virtual float ModifyIncomingDirectDamage(GameObject holder, GameObject attacker, float damage)
    {
        return damage;
    }

    public virtual float ModifyHealingReceived(GameObject holder, GameObject healer, float healing)
    {
        return healing;
    }
}