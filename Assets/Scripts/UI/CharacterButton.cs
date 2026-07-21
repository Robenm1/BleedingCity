using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CharacterButton : MonoBehaviour
{
    [Header("Data")]
    public CharacterData character;

    [Header("Wiring")]
    public CharacterSelectUIManager uiManager;
    public Image buttonIconImage;

    [Header("Locked Visuals")]
    [Tooltip("Tint applied to the icon when locked.")]
    public Color lockedIconTint = new Color(0.4f, 0.4f, 0.4f, 1f);

    [Tooltip("Tint applied to the icon when unlocked/normal.")]
    public Color normalIconTint = Color.white;

    [Tooltip("CanvasGroup alpha when locked. 1 keeps brightness from iconTint only.")]
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

        _btn.onClick.AddListener(() =>
        {
            if (uiManager && character)
                uiManager.SelectCharacter(character);
        });

        if (!buttonIconImage)
            buttonIconImage = GetComponent<Image>();

        RefreshButtonIcon();
        ApplyLockedVisuals(false);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!buttonIconImage)
            buttonIconImage = GetComponent<Image>();

        RefreshButtonIcon();
        ApplyLockedVisuals(_isLocked);
    }
#endif

    public void SetLocked(bool locked)
    {
        _isLocked = locked;

        ApplyLockedVisuals(locked);

        if (_btn)
            _btn.interactable = true;

        if (_cg)
        {
            _cg.blocksRaycasts = true;
            _cg.alpha = locked ? lockedAlpha : normalAlpha;
        }
    }

    private void RefreshButtonIcon()
    {
        if (buttonIconImage && character)
            buttonIconImage.sprite = character.buttonIcon;
    }

    private void ApplyLockedVisuals(bool locked)
    {
        if (buttonIconImage)
            buttonIconImage.color = locked ? lockedIconTint : normalIconTint;
    }
}