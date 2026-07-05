using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[DisallowMultipleComponent]
public class DemonicSlowEffect : MonoBehaviour
{
    [Header("Slow")]
    [Tooltip("Speed multiplier while slowed. 0.5 = 50% speed.")]
    public float slowMultiplier = 0.5f;

    [Tooltip("How long the slow lasts.")]
    public float slowDuration = 2f;

    [Tooltip("Common field/property names used for enemy speed.")]
    public string[] speedMemberNames =
    {
        "moveSpeed",
        "speed",
        "movementSpeed",
        "chaseSpeed"
    };

    private float _timer;
    private bool _applied;

    private readonly List<SpeedMember> _modifiedMembers = new List<SpeedMember>();

    private struct SpeedMember
    {
        public Component component;
        public FieldInfo field;
        public PropertyInfo property;
        public float originalValue;
    }

    private void Update()
    {
        if (!_applied) return;

        _timer -= Time.deltaTime;

        if (_timer <= 0f)
        {
            RestoreSpeeds();
            Destroy(this);
        }
    }

    public void ApplySlow(float multiplier, float duration)
    {
        slowMultiplier = Mathf.Clamp(multiplier, 0.01f, 1f);
        slowDuration = Mathf.Max(0f, duration);
        _timer = Mathf.Max(_timer, slowDuration);

        if (_applied) return;

        ApplyToSpeedMembers();
    }

    private void ApplyToSpeedMembers()
    {
        _modifiedMembers.Clear();

        Component[] components = GetComponents<Component>();

        foreach (var component in components)
        {
            if (component == null) continue;

            var type = component.GetType();

            foreach (string memberName in speedMemberNames)
            {
                var field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null && field.FieldType == typeof(float))
                {
                    float original = (float)field.GetValue(component);
                    field.SetValue(component, original * slowMultiplier);

                    _modifiedMembers.Add(new SpeedMember
                    {
                        component = component,
                        field = field,
                        property = null,
                        originalValue = original
                    });

                    continue;
                }

                var property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (property != null && property.PropertyType == typeof(float) && property.CanRead && property.CanWrite)
                {
                    float original = (float)property.GetValue(component);
                    property.SetValue(component, original * slowMultiplier);

                    _modifiedMembers.Add(new SpeedMember
                    {
                        component = component,
                        field = null,
                        property = property,
                        originalValue = original
                    });
                }
            }
        }

        _applied = true;
    }

    private void RestoreSpeeds()
    {
        for (int i = 0; i < _modifiedMembers.Count; i++)
        {
            var member = _modifiedMembers[i];

            if (member.component == null) continue;

            if (member.field != null)
            {
                member.field.SetValue(member.component, member.originalValue);
            }
            else if (member.property != null && member.property.CanWrite)
            {
                member.property.SetValue(member.component, member.originalValue);
            }
        }

        _modifiedMembers.Clear();
        _applied = false;
    }

    private void OnDisable()
    {
        if (_applied)
            RestoreSpeeds();
    }
}