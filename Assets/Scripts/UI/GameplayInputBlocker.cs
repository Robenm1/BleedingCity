using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

[DisallowMultipleComponent]
public class BuffPanelInputBlocker : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Your PlayerInput component. Usually on the Player.")]
    public PlayerInput playerInput;

    [Tooltip("Name of the gameplay action map. Usually Player or Gameplay.")]
    public string gameplayActionMapName = "Player";

    [Tooltip("Optional UI action map name. Leave empty if you do not use one.")]
    public string uiActionMapName = "";

    [Header("Options")]
    [Tooltip("Re-enable gameplay only after left click is released, so choosing a buff does not trigger Ability1.")]
    public bool waitForMouseRelease = true;

    private InputActionMap _gameplayMap;
    private bool _wasGameplayEnabled;

    private void Awake()
    {
        if (playerInput == null)
            playerInput = FindObjectOfType<PlayerInput>();

        CacheGameplayMap();
    }

    private void OnEnable()
    {
        BlockGameplayInput();
    }

    private void OnDisable()
    {
        BuffPanelInputBlockerRunner.Run(ReenableGameplaySafely());
    }

    private void CacheGameplayMap()
    {
        if (playerInput == null || playerInput.actions == null) return;

        _gameplayMap = playerInput.actions.FindActionMap(gameplayActionMapName, false);
    }

    private void BlockGameplayInput()
    {
        if (_gameplayMap == null)
            CacheGameplayMap();

        if (_gameplayMap == null)
        {
            Debug.LogWarning($"[BuffPanelInputBlocker] Could not find gameplay action map: {gameplayActionMapName}");
            return;
        }

        _wasGameplayEnabled = _gameplayMap.enabled;

        if (_gameplayMap.enabled)
            _gameplayMap.Disable();

        if (!string.IsNullOrEmpty(uiActionMapName) && playerInput != null && playerInput.actions != null)
        {
            var uiMap = playerInput.actions.FindActionMap(uiActionMapName, false);
            if (uiMap != null && !uiMap.enabled)
                uiMap.Enable();
        }
    }

    private IEnumerator ReenableGameplaySafely()
    {
        if (waitForMouseRelease)
        {
            while (Mouse.current != null && Mouse.current.leftButton.isPressed)
                yield return null;
        }

        // Extra frame safety so the same click cannot trigger Ability1.
        yield return null;

        if (_gameplayMap != null && _wasGameplayEnabled)
            _gameplayMap.Enable();
    }
}

public class BuffPanelInputBlockerRunner : MonoBehaviour
{
    private static BuffPanelInputBlockerRunner _instance;

    public static void Run(IEnumerator routine)
    {
        if (_instance == null)
        {
            GameObject obj = new GameObject("BuffPanelInputBlockerRunner");
            DontDestroyOnLoad(obj);
            _instance = obj.AddComponent<BuffPanelInputBlockerRunner>();
        }

        _instance.StartCoroutine(routine);
    }
}