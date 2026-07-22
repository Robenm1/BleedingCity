using UnityEngine;

[DisallowMultipleComponent]
public class ElementHolder : MonoBehaviour
{
    [Header("Element")]
    [Tooltip("Assign one element ScriptableObject here.")]
    public BaseElementSO element;

    [Header("Runtime")]
    [Tooltip("Apply the element automatically when this object starts.")]
    public bool applyOnStart = true;

    [Tooltip("Remove the element passive when this object is disabled.")]
    public bool removeOnDisable = true;

    [Header("Debug")]
    public bool showDebug = true;

    private BaseElementSO _appliedElement;

    private void Start()
    {
        if (applyOnStart)
            ApplyElement();
    }

    private void OnDisable()
    {
        if (removeOnDisable)
            RemoveCurrentElement();
    }

    public void ApplyElement()
    {
        if (_appliedElement == element)
            return;

        RemoveCurrentElement();

        _appliedElement = element;

        if (_appliedElement != null)
        {
            _appliedElement.OnElementApplied(gameObject);

            if (showDebug)
                Debug.Log($"[ElementHolder] {name} gained element: {_appliedElement.elementName}");
        }
        else
        {
            if (showDebug)
                Debug.Log($"[ElementHolder] {name} has no element.");
        }
    }

    public void SetElement(BaseElementSO newElement)
    {
        if (element == newElement && _appliedElement == newElement)
            return;

        element = newElement;
        ApplyElement();
    }

    public void RemoveCurrentElement()
    {
        if (_appliedElement == null)
            return;

        if (showDebug)
            Debug.Log($"[ElementHolder] {name} removed element: {_appliedElement.elementName}");

        _appliedElement.OnElementRemoved(gameObject);
        _appliedElement = null;
    }

    public bool HasElement()
    {
        return element != null;
    }

    public BaseElementSO GetElement()
    {
        return element;
    }

    public string GetElementName()
    {
        return element != null ? element.elementName : "None";
    }

    public Color GetElementColor()
    {
        return element != null ? element.elementColor : Color.white;
    }

    public float ModifyDirectDamage(GameObject target, float damage)
    {
        if (element == null)
            return damage;

        return element.ModifyDirectDamage(gameObject, target, damage);
    }

    public float ModifyDotDamage(GameObject target, float totalDotDamage)
    {
        if (element == null)
            return totalDotDamage;

        return element.ModifyDotDamage(gameObject, target, totalDotDamage);
    }

    public float ModifyIncomingDirectDamage(GameObject attacker, float damage)
    {
        if (element == null)
            return damage;

        return element.ModifyIncomingDirectDamage(gameObject, attacker, damage);
    }

    public float ModifyHealingReceived(GameObject healer, float healing)
    {
        if (element == null)
            return healing;

        return element.ModifyHealingReceived(gameObject, healer, healing);
    }
}