using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public interface IDeckSelectionSource
{
    IReadOnlyList<CardData> GetSelectedCards();
}

public class CharacterSelectFlow : MonoBehaviour
{
    [Header("Character Spawn (in SELECT scene)")]
    public Transform spawnPoint;
    public bool spawnOnSelect = false;

    [Tooltip("Hide any Canvas under the previewed player so gameplay HUD doesn't appear in the select scene.")]
    public bool disableChildCanvasesOnPreview = true;

    [Header("Start Validation")]
    public int minCardsRequired = 3;
    public TMP_Text warningText;
    public Button startButton;

    [Header("Selected Data (source)")]
    [Tooltip("Your CharacterSelectUIManager (implements IDeckSelectionSource).")]
    public CharacterSelectUIManager deckSelectionSource;

    [Header("Scene Flow")]
    [Tooltip("Gameplay scene name (as in Build Settings).")]
    public string gameplaySceneName = "";

    // runtime
    private CharacterData _currentCharacter;
    private GameObject _previewPlayer;
    private IDeckSelectionSource _source;

    void Awake()
    {
        // Auto-find if not assigned
        if (deckSelectionSource == null)
            deckSelectionSource = FindObjectOfType<CharacterSelectUIManager>();

        _source = deckSelectionSource as IDeckSelectionSource;

        if (warningText) warningText.gameObject.SetActive(false);
        ValidateCanStart();
    }

    // ─────────────────────────────────────────────────────────────
    // UI HOOKS
    // ─────────────────────────────────────────────────────────────
    public void OnSelectCharacter(CharacterData data)
    {
        _currentCharacter = data;

        if (spawnOnSelect)
            SpawnPreviewPlayer();

        ValidateCanStart();
    }

    public void OnPressStartGame()
    {
        var deck = GetCurrentDeck();
        int need = Mathf.Max(0, minCardsRequired);

        if (_currentCharacter == null)
        {
            ShowWarning("Please select a character.");
            return;
        }
        if (deck.Count < need)
        {
            ShowWarning($"You must pick at least {need} cards.");
            return;
        }

        // Persist selection
        if (SelectionCarrier.Instance == null)
            new GameObject("SelectionCarrier").AddComponent<SelectionCarrier>();
        SelectionCarrier.Instance.SetSelection(_currentCharacter, deck);

        // Load gameplay
        if (!string.IsNullOrEmpty(gameplaySceneName))
        {
            SceneManager.LoadScene(gameplaySceneName);
        }
        else
        {
            HideWarning();
        }
    }

    /// <summary>Call this when the deck changes (cards added/removed).</summary>
    public void OnDeckChanged()
    {
        ValidateCanStart();
    }

    // ─────────────────────────────────────────────────────────────
    // VALIDATION
    // ─────────────────────────────────────────────────────────────
    public void ValidateCanStart()
    {
        bool haveChar = _currentCharacter != null;
        int count = GetCurrentDeck().Count;
        bool ok = haveChar && count >= Mathf.Max(0, minCardsRequired);

        if (startButton) startButton.interactable = ok;

        if (!ok)
            ShowWarning($"You must pick at least {minCardsRequired} cards.");
        else
            HideWarning();
    }

    private IReadOnlyList<CardData> GetCurrentDeck()
    {
        if (_source != null)
        {
            var d = _source.GetSelectedCards();
            if (d != null) return d;
        }
        return System.Array.Empty<CardData>();
    }

    private void ShowWarning(string msg)
    {
        if (!warningText) return;
        warningText.text = msg;
        if (!warningText.gameObject.activeSelf) warningText.gameObject.SetActive(true);
    }

    private void HideWarning()
    {
        if (!warningText) return;
        warningText.gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────
    // PREVIEW (select scene)
    // ─────────────────────────────────────────────────────────────
    private void SpawnPreviewPlayer()
    {
        if (_currentCharacter == null || _currentCharacter.characterPrefab == null)
        {
            Debug.LogWarning("[CharacterSelectFlow] No character or prefab to preview.");
            return;
        }

        if (_previewPlayer) Destroy(_previewPlayer);

        var pos = spawnPoint ? spawnPoint.position : Vector3.zero;
        var rot = spawnPoint ? spawnPoint.rotation : Quaternion.identity;
        _previewPlayer = Instantiate(_currentCharacter.characterPrefab, pos, rot);
        _previewPlayer.name = $"{_currentCharacter.name} (Preview)";

        if (disableChildCanvasesOnPreview)
        {
            foreach (var cv in _previewPlayer.GetComponentsInChildren<Canvas>(true))
                cv.gameObject.SetActive(false);
        }
    }
}
