using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Full-screen darkness overlay with a circular flashlight hole that follows the cursor.
/// Attach to a full-screen RawImage that sits above all other UI siblings.
/// </summary>
[RequireComponent(typeof(RawImage))]
public class FlashlightCursor : MonoBehaviour
{
    [Header("Darkness")]
    [Range(0f, 1f)]
    [Tooltip("Alpha of the dark overlay (0 = invisible, 1 = pitch black).")]
    public float darkAlpha = 0.82f;

    [Header("Flashlight")]
    [Range(0f, 0.5f)]
    [Tooltip("Radius of the lit circle in screen UV space (0–1).")]
    public float lightRadius = 0.18f;

    [Range(0f, 0.3f)]
    [Tooltip("How soft/gradual the edge of the light circle is.")]
    public float edgeSoftness = 0.07f;

    private Material _mat;

    private static readonly int s_MousePos    = Shader.PropertyToID("_MousePos");
    private static readonly int s_AspectRatio = Shader.PropertyToID("_AspectRatio");
    private static readonly int s_Radius      = Shader.PropertyToID("_Radius");
    private static readonly int s_Softness    = Shader.PropertyToID("_Softness");
    private static readonly int s_Color       = Shader.PropertyToID("_Color");

    private void Awake()
    {
        var shader = Shader.Find("Custom/FlashlightOverlay");
        if (!shader)
        {
            Debug.LogError("[FlashlightCursor] Shader 'Custom/FlashlightOverlay' not found. Make sure it is inside the Assets folder.");
            enabled = false;
            return;
        }

        _mat = new Material(shader) { name = "FlashlightOverlayInstance" };

        var img = GetComponent<RawImage>();
        img.material     = _mat;
        img.color        = Color.white;
        img.raycastTarget = false; // never block UI clicks

        ApplyStaticSettings();
    }

    private void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 mousePos = mouse.position.ReadValue();
        float u = mousePos.x / Screen.width;
        float v = mousePos.y / Screen.height;

        _mat.SetVector(s_MousePos,    new Vector4(u, v, 0f, 0f));
        _mat.SetFloat(s_AspectRatio, (float)Screen.width / Screen.height);
    }

    /// <summary>Pushes inspector-tweakable values to the material.</summary>
    private void ApplyStaticSettings()
    {
        _mat.SetColor(s_Color,    new Color(0f, 0f, 0f, darkAlpha));
        _mat.SetFloat(s_Radius,   lightRadius);
        _mat.SetFloat(s_Softness, edgeSoftness);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_mat) ApplyStaticSettings();
    }
#endif
}
