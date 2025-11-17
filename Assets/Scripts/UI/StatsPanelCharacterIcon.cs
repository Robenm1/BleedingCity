// StatsPanelCharacterIcon.cs
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

public class StatsPanelCharacterIcon : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image characterIconImage;

    [Header("Preferences")]
    [SerializeField] private bool useButtonIcon = true;
    [SerializeField] private bool verboseLogs = false;

    private void OnEnable() => Refresh();

    public void Refresh()
    {
        if (!characterIconImage)
        {
            if (verboseLogs) Debug.LogWarning("[StatsPanelCharacterIcon] No Image assigned.");
            return;
        }

        var data = ResolveCharacter();
        if (!data)
        {
            characterIconImage.enabled = false;
            if (verboseLogs) Debug.LogWarning("[StatsPanelCharacterIcon] No CharacterData found.");
            return;
        }

        Sprite s = null;
        if (useButtonIcon && data.buttonIcon) s = data.buttonIcon;
        if (!s && data.selectedIcon) s = data.selectedIcon;

        if (s)
        {
            characterIconImage.enabled = true;
            characterIconImage.sprite = s;
            characterIconImage.preserveAspect = true;
        }
        else
        {
            characterIconImage.enabled = false;
        }
    }

    private CharacterData ResolveCharacter()
    {
        // 1) Preferred: global selection set by character select flow
        if (SelectedContext.Current) return SelectedContext.Current;

        // 2) Fallback: ask the spawned player (if present)
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player)
        {
            var id = player.GetComponent<PlayerIdentity>();
            if (id && id.characterData) return id.characterData;
        }

        // 3) Last resort: try to find any singleton named SelectionCarrier.* with a CharacterData field/property
        var t = FindTypeByName("SelectionCarrier");
        if (t != null)
        {
            object inst = GetStaticMember(t, "Instance") ?? GetStaticMember(t, "instance") ?? GetStaticMember(t, "Singleton");
            if (inst != null)
            {
                string[] names = { "SelectedCharacter", "SelectedCharacterData", "character", "characterData", "current", "currentSelection" };
                foreach (var n in names)
                {
                    var cd = GetMemberValue<CharacterData>(inst, n);
                    if (cd) return cd;
                }
            }
        }

        return null;
    }

    private static System.Type FindTypeByName(string shortName)
    {
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            var types = asm.GetTypes();
            for (int i = 0; i < types.Length; i++)
                if (types[i].Name == shortName) return types[i];
        }
        return null;
    }

    private static object GetStaticMember(System.Type t, string name)
    {
        const BindingFlags F = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        var p = t.GetProperty(name, F);
        if (p != null) return p.GetValue(null, null);
        var f = t.GetField(name, F);
        if (f != null) return f.GetValue(null);
        return null;
    }

    private static T GetMemberValue<T>(object obj, string name) where T : class
    {
        if (obj == null) return null;
        const BindingFlags F = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        var t = obj.GetType();

        var p = t.GetProperty(name, F);
        if (p != null && typeof(T).IsAssignableFrom(p.PropertyType))
            return p.GetValue(obj, null) as T;

        var f = t.GetField(name, F);
        if (f != null && typeof(T).IsAssignableFrom(f.FieldType))
            return f.GetValue(obj) as T;

        return null;
    }
}
