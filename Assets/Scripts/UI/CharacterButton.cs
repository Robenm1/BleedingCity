using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CharacterButton : MonoBehaviour
{
    [Header("Data")]
    public CharacterData character;

    [Header("Wiring")]
    public CharacterSelectUIManager uiManager;   // assign in Inspector
    public Image buttonIconImage;                // icon on the button (optional)

    [Header("Locked Visuals")]
    [Tooltip("Tint applied to the icon when locked.")]
    public Color lockedIconTint = new Color(0.4f, 0.4f, 0.4f, 1f); // darker
    [Tooltip("Tint applied to the icon when unlocked/normal.")]
    public Color normalIconTint = Color.white;
    [Tooltip("CanvasGroup alpha when locked (0-1). 1 keeps brightness from iconTint only.")]
    [Range(0f, 1f)] public float lockedAlpha = 1f;
    [Range(0f, 1f)] public float normalAlpha = 1f;

    private Button _btn;
    private CanvasGroup _cg;
    private bool _isLocked;

    private void Awake()
    {
        _btn = GetComponent<Button>();
        _cg = GetComponent<CanvasGroup>();
        if (!_cg) _cg = gameObject.AddComponent<CanvasGroup>();

        // keep clickable ALWAYS; manager decides if switch is allowed
        _btn.onClick.AddListener(() =>
        {
            if (uiManager && character)
                uiManager.SelectCharacter(character); // will shake if locked to another character
        });

        if (!buttonIconImage) buttonIconImage = GetComponent<Image>();
        if (buttonIconImage && character)
            buttonIconImage.sprite = character.buttonIcon;

        // default visuals
        ApplyLockedVisuals(false);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!buttonIconImage) buttonIconImage = GetComponent<Image>();
        if (buttonIconImage && character)
            buttonIconImage.sprite = character.buttonIcon;

        // keep the preview consistent in editor
        ApplyLockedVisuals(_isLocked);
    }
#endif

    /// <summary>
    /// Called by the UI Manager. Keeps button clickable but darkens when locked.
    /// </summary>
    public void SetLocked(bool locked)
    {
        _isLocked = locked;
        ApplyLockedVisuals(locked);
        // IMPORTANT: keep clickable so we can trigger the shake UX in the manager
        if (_btn) _btn.interactable = true;
        if (_cg)
        {
            _cg.blocksRaycasts = true;           // allow clicks to go through
            _cg.alpha = locked ? lockedAlpha : normalAlpha;
        }
    }

    private void ApplyLockedVisuals(bool locked)
    {
        if (buttonIconImage)
            buttonIconImage.color = locked ? lockedIconTint : normalIconTint;
    }
}
