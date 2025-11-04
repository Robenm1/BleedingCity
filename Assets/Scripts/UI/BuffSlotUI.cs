using UnityEngine;
using TMPro;

public class BuffSlotUI : MonoBehaviour
{
    [Tooltip("Root container for this slot (hide when empty).")]
    public GameObject root;

    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descText;

    // If later you want icons, add: public Image icon;

    public void Set(BuffData data)
    {
        if (data == null)
        {
            if (root) root.SetActive(false);
            else gameObject.SetActive(false);
            return;
        }

        if (root) root.SetActive(true);
        else gameObject.SetActive(true);

        if (nameText) nameText.text = data.buffName;
        if (descText) descText.text = data.description;
        // if (icon) icon.sprite = data.icon;
    }
}
