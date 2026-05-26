using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Renders a name as individual paper-cut style letter tiles — each with a random
/// vivid background, contrasting letter colour, and a slight random rotation.
/// Attach to a GameObject that also has a HorizontalLayoutGroup.
/// </summary>
[RequireComponent(typeof(HorizontalLayoutGroup))]
public class PaperCutNameDisplay : MonoBehaviour
{
    [Header("Tile")]
    [Tooltip("Width and height of each letter tile in pixels.")]
    public float tileSize = 54f;

    [Tooltip("Gap between tiles.")]
    public float spacing = 5f;

    [Tooltip("Maximum random rotation applied to each tile (degrees).")]
    [Range(0f, 25f)]
    public float maxRotation = 12f;

    [Header("Font")]
    [Tooltip("TMP font asset used for the letters. Defaults to TMP's built-in if null.")]
    public TMP_FontAsset letterFont;

    [Range(10f, 72f)]
    public float fontSize = 32f;

    // ── Colour palette ────────────────────────────────────────────────────────
    private static readonly Color[] BgColors =
    {
        new Color(0.88f, 0.12f, 0.12f), // red
        new Color(0.94f, 0.48f, 0.08f), // orange
        new Color(0.96f, 0.88f, 0.08f), // yellow
        new Color(0.12f, 0.40f, 0.84f), // blue
        new Color(0.08f, 0.62f, 0.28f), // green
        new Color(0.52f, 0.12f, 0.74f), // purple
        new Color(0.08f, 0.62f, 0.68f), // teal
        new Color(0.90f, 0.90f, 0.90f), // off-white
        new Color(0.10f, 0.10f, 0.10f), // near-black
        new Color(0.68f, 0.32f, 0.08f), // brown
    };

    // Contrasting text colour matched 1-to-1 with BgColors
    private static readonly Color[] TextColors =
    {
        Color.white,                       // on red
        Color.white,                       // on orange
        new Color(0.08f, 0.08f, 0.08f),   // on yellow  → dark
        Color.white,                       // on blue
        Color.white,                       // on green
        Color.white,                       // on purple
        Color.white,                       // on teal
        new Color(0.08f, 0.08f, 0.08f),   // on off-white → dark
        Color.white,                       // on near-black
        Color.white,                       // on brown
    };

    private readonly List<GameObject> _tiles = new();

    private void Awake()
    {
        var hlg = GetComponent<HorizontalLayoutGroup>();
        hlg.childAlignment        = TextAnchor.MiddleCenter;
        hlg.childControlWidth     = false;
        hlg.childControlHeight    = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.spacing               = spacing;
        hlg.padding               = new RectOffset(8, 8, 0, 0);
    }

    /// <summary>Rebuilds the letter tiles for the given name. Pass null or empty to clear.</summary>
    public void SetName(string name)
    {
        foreach (var t in _tiles) Destroy(t);
        _tiles.Clear();

        if (string.IsNullOrWhiteSpace(name)) return;

        foreach (char c in name)
        {
            if (c == ' ')
                SpawnSpacer();
            else
                SpawnTile(c.ToString().ToUpper());
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void SpawnTile(string letter)
    {
        int idx = Random.Range(0, BgColors.Length);

        // Root tile GO — carries the background Image and rotation
        var tileGO = new GameObject($"Tile_{letter}", typeof(RectTransform));
        tileGO.transform.SetParent(transform, false);

        var tileRT = tileGO.GetComponent<RectTransform>();
        tileRT.sizeDelta = new Vector2(tileSize, tileSize);

        // Random tilt — the slight overlaps add to the ransom-note charm
        tileGO.transform.localRotation =
            Quaternion.Euler(0f, 0f, Random.Range(-maxRotation, maxRotation));

        // Background
        var bg = tileGO.AddComponent<Image>();
        bg.color         = BgColors[idx];
        bg.raycastTarget = false;

        // Letter text child
        var textGO = new GameObject("Letter",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(tileGO.transform, false);

        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        var tmp = textGO.GetComponent<TextMeshProUGUI>();
        tmp.text          = letter;
        tmp.fontSize      = fontSize;
        tmp.fontStyle     = FontStyles.Bold;
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.color         = TextColors[idx];
        tmp.raycastTarget = false;
        if (letterFont) tmp.font = letterFont;

        _tiles.Add(tileGO);
    }

    private void SpawnSpacer()
    {
        var spacerGO = new GameObject("Space", typeof(RectTransform));
        spacerGO.transform.SetParent(transform, false);
        spacerGO.GetComponent<RectTransform>().sizeDelta = new Vector2(tileSize * 0.4f, tileSize);
        _tiles.Add(spacerGO);
    }
}
