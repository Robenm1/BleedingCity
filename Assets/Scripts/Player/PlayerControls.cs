using UnityEngine;
using UnityEngine.InputSystem;
using System;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerControls : MonoBehaviour
{
    [Header("Input Actions (assign in Inspector)")]
    public InputActionReference Move;
    public InputActionReference Ability1;
    public InputActionReference Ability2;
    public InputActionReference Active1;
    public InputActionReference Active2;
    public InputActionReference Interact;
    public InputActionReference Dash;

    [Tooltip("Press this to open/trigger the level up menu / spend points.")]
    public InputActionReference LevelUp; // NEW

    private PlayerMovement movement;

    // Events other systems can listen to (abilities, interact, etc.)
    public event Action OnAbility1;
    public event Action OnAbility2;
    public event Action OnActive1;
    public event Action OnActive2;
    public event Action OnInteract;
    public event Action OnDash;
    public event Action OnLevelUp; // NEW

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
    }

    private void OnEnable()
    {
        // Enable actions when object becomes active
        Move.action.Enable();
        Ability1.action.Enable();
        Ability2.action.Enable();
        Active1.action.Enable();
        Active2.action.Enable();
        Interact.action.Enable();
        Dash.action.Enable();
        LevelUp.action.Enable(); // NEW

        // Hook button callbacks
        Ability1.action.performed += HandleAbility1;
        Ability2.action.performed += HandleAbility2;
        Active1.action.performed += HandleActive1;
        Active2.action.performed += HandleActive2;
        Interact.action.performed += HandleInteract;
        Dash.action.performed += HandleDash;
        LevelUp.action.performed += HandleLevelUp; // NEW
    }

    private void OnDisable()
    {
        // Unhook callbacks
        Ability1.action.performed -= HandleAbility1;
        Ability2.action.performed -= HandleAbility2;
        Active1.action.performed -= HandleActive1;
        Active2.action.performed -= HandleActive2;
        Interact.action.performed -= HandleInteract;
        Dash.action.performed -= HandleDash;
        LevelUp.action.performed -= HandleLevelUp; // NEW

        // Disable actions
        Move.action.Disable();
        Ability1.action.Disable();
        Ability2.action.Disable();
        Active1.action.Disable();
        Active2.action.Disable();
        Interact.action.Disable();
        Dash.action.Disable();
        LevelUp.action.Disable(); // NEW
    }

    private void Update()
    {
        // Read movement every frame and pass it to PlayerMovement
        Vector2 moveVec = Move.action.ReadValue<Vector2>();
        movement.SetMoveInput(moveVec);
    }

    // ===== Input Callbacks =====

    private void HandleAbility1(InputAction.CallbackContext ctx)
    {
        OnAbility1?.Invoke();
        // e.g. primary fire / auto ability override trigger
    }

    private void HandleAbility2(InputAction.CallbackContext ctx)
    {
        OnAbility2?.Invoke();
        // e.g. alt fire / secondary ability
    }

    private void HandleActive1(InputAction.CallbackContext ctx)
    {
        OnActive1?.Invoke();
        // e.g. active item 1 / consumable / card active
    }

    private void HandleActive2(InputAction.CallbackContext ctx)
    {
        OnActive2?.Invoke();
        // e.g. active item 2 / consumable / card active
    }

    private void HandleInteract(InputAction.CallbackContext ctx)
    {
        OnInteract?.Invoke();
        // e.g. open shop, pick up loot, enter building
    }

    private void HandleDash(InputAction.CallbackContext ctx)
    {
        OnDash?.Invoke();
        movement.TryDash();
    }

    private void HandleLevelUp(InputAction.CallbackContext ctx)
    {
        OnLevelUp?.Invoke();
        // Your level up system can listen to this and open the "choose 1 of 3 buffs" UI
        // and spend points from the wallet.
    }
}
