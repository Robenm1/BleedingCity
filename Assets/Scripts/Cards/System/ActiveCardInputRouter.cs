using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerControls))]
public class ActiveCardInputRouter : MonoBehaviour
{
    public enum ActiveSlot
    {
        Active1,
        Active2
    }

    [Header("Runtime Active Cards")]
    [Tooltip("The active card assigned to Active1.")]
    [SerializeField] private MonoBehaviour active1Effect;

    [Tooltip("The active card assigned to Active2.")]
    [SerializeField] private MonoBehaviour active2Effect;

    [Header("Input")]
    [Tooltip("If true, the router reads Active1/Active2 directly from PlayerControls every frame.")]
    public bool pollInputDirectly = true;

    [Header("Debug")]
    public bool showDebug = true;

    private PlayerControls _controls;

    private void Awake()
    {
        _controls = GetComponent<PlayerControls>();
    }

    private void Update()
    {
        if (!pollInputDirectly) return;

        if (!_controls)
            _controls = GetComponent<PlayerControls>();

        if (!_controls) return;

        if (_controls.Active1 != null &&
            _controls.Active1.action != null &&
            _controls.Active1.action.WasPressedThisFrame())
        {
            ActivateSlot1();
        }

        if (_controls.Active2 != null &&
            _controls.Active2.action != null &&
            _controls.Active2.action.WasPressedThisFrame())
        {
            ActivateSlot2();
        }
    }

    // ── Public API ─────────────────────────────────────────────────────────

    public bool RegisterFirstFree(MonoBehaviour effect)
    {
        if (!IsValidActiveEffect(effect)) return false;

        if (active1Effect == null)
            return RegisterToSlot(effect, ActiveSlot.Active1);

        if (active2Effect == null)
            return RegisterToSlot(effect, ActiveSlot.Active2);

        if (showDebug)
            Debug.LogWarning($"[ActiveCardInputRouter] Both active slots are full. Could not register {effect.GetType().Name}.");

        return false;
    }

    public bool RegisterToSlot(MonoBehaviour effect, ActiveSlot slot)
    {
        if (!IsValidActiveEffect(effect)) return false;

        if (slot == ActiveSlot.Active1)
        {
            active1Effect = effect;

            if (showDebug)
                Debug.Log($"[ActiveCardInputRouter] Registered {effect.GetType().Name} to Active1.");

            return true;
        }

        active2Effect = effect;

        if (showDebug)
            Debug.Log($"[ActiveCardInputRouter] Registered {effect.GetType().Name} to Active2.");

        return true;
    }

    public void ClearSlot(ActiveSlot slot)
    {
        if (slot == ActiveSlot.Active1)
            active1Effect = null;
        else
            active2Effect = null;
    }

    public void ClearAll()
    {
        active1Effect = null;
        active2Effect = null;
    }

    // ── Activation ─────────────────────────────────────────────────────────

    private void ActivateSlot1()
    {
        ActivateEffect(active1Effect, "Active1");
    }

    private void ActivateSlot2()
    {
        ActivateEffect(active2Effect, "Active2");
    }

    private void ActivateEffect(MonoBehaviour effect, string slotName)
    {
        if (showDebug)
            Debug.Log($"[ActiveCardInputRouter] {slotName} pressed.");

        if (effect == null)
        {
            if (showDebug)
                Debug.Log($"[ActiveCardInputRouter] {slotName} has no active card assigned.");

            return;
        }

        if (effect is IActiveCardEffect activeEffect)
        {
            activeEffect.Activate();

            if (showDebug)
                Debug.Log($"[ActiveCardInputRouter] Activated {effect.GetType().Name} from {slotName}.");

            return;
        }

        Debug.LogWarning($"[ActiveCardInputRouter] {effect.GetType().Name} does not implement IActiveCardEffect.");
    }

    private bool IsValidActiveEffect(MonoBehaviour effect)
    {
        if (effect == null)
        {
            if (showDebug)
                Debug.LogWarning("[ActiveCardInputRouter] Tried to register a null active effect.");

            return false;
        }

        if (effect is IActiveCardEffect)
            return true;

        Debug.LogWarning($"[ActiveCardInputRouter] {effect.GetType().Name} does not implement IActiveCardEffect.");
        return false;
    }
}