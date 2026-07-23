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

    [System.Serializable]
    public class ActiveCardRuntimeData
    {
        [Header("Runtime Effect")]
        public MonoBehaviour effect;

        [Header("UI Data")]
        public Sprite cardIcon;
        public float cooldown = 5f;
    }

    [Header("Runtime Active Cards")]
    [Tooltip("Active1 = E button UI slot.")]
    [SerializeField] private ActiveCardRuntimeData active1 = new ActiveCardRuntimeData();

    [Tooltip("Active2 = Q button UI slot.")]
    [SerializeField] private ActiveCardRuntimeData active2 = new ActiveCardRuntimeData();

    [Header("HUD")]
    [Tooltip("If empty, the router will search in children.")]
    [SerializeField] private ActiveCardHUD activeCardHUD;

    [Header("Input")]
    public bool pollInputDirectly = true;

    [Header("Cooldown")]
    [Tooltip("Normal active cards start HUD cooldown immediately. Manual cards like Abyssal Doll control it themselves.")]
    public bool startHudCooldownOnActivate = true;

    [Header("Debug")]
    public bool showDebug = true;

    private PlayerControls _controls;

    private void Awake()
    {
        _controls = GetComponent<PlayerControls>();

        if (activeCardHUD == null)
            activeCardHUD = GetComponentInChildren<ActiveCardHUD>();
    }

    private void Start()
    {
        RefreshHUD();
    }

    private void Update()
    {
        if (!pollInputDirectly)
            return;

        if (!_controls)
            _controls = GetComponent<PlayerControls>();

        if (!_controls)
            return;

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

    public bool RegisterFirstFree(MonoBehaviour effect)
    {
        return RegisterFirstFree(effect, null, 5f);
    }

    public bool RegisterFirstFree(MonoBehaviour effect, Sprite cardIcon, float cooldown)
    {
        if (!IsValidActiveEffect(effect))
            return false;

        if (active1.effect == null)
            return RegisterToSlot(effect, ActiveSlot.Active1, cardIcon, cooldown);

        if (active2.effect == null)
            return RegisterToSlot(effect, ActiveSlot.Active2, cardIcon, cooldown);

        if (showDebug)
            Debug.LogWarning($"[ActiveCardInputRouter] Both active slots are full. Could not register {effect.GetType().Name}.");

        return false;
    }

    public bool RegisterToSlot(MonoBehaviour effect, ActiveSlot slot)
    {
        return RegisterToSlot(effect, slot, null, 5f);
    }

    public bool RegisterToSlot(MonoBehaviour effect, ActiveSlot slot, Sprite cardIcon, float cooldown)
    {
        if (!IsValidActiveEffect(effect))
            return false;

        ActiveCardRuntimeData data = GetSlotData(slot);

        data.effect = effect;
        data.cardIcon = cardIcon;
        data.cooldown = Mathf.Max(0.01f, cooldown);

        CacheHUD();

        if (effect is IActiveCardHUDReceiver receiver)
            receiver.SetActiveCardHUD(activeCardHUD, slot);

        UpdateHUDSlot(slot);

        if (showDebug)
        {
            Debug.Log(
                $"[ActiveCardInputRouter] Registered {effect.GetType().Name} to {slot}. " +
                $"Icon: {(cardIcon != null ? cardIcon.name : "None")}, Cooldown: {data.cooldown:F1}"
            );
        }

        return true;
    }

    public void ClearSlot(ActiveSlot slot)
    {
        ActiveCardRuntimeData data = GetSlotData(slot);

        data.effect = null;
        data.cardIcon = null;
        data.cooldown = 0f;

        CacheHUD();

        if (activeCardHUD != null)
        {
            if (slot == ActiveSlot.Active1)
                activeCardHUD.ClearActiveCard1();
            else
                activeCardHUD.ClearActiveCard2();
        }
    }

    public void ClearAll()
    {
        ClearSlot(ActiveSlot.Active1);
        ClearSlot(ActiveSlot.Active2);
    }

    public void RefreshHUD()
    {
        UpdateHUDSlot(ActiveSlot.Active1);
        UpdateHUDSlot(ActiveSlot.Active2);
    }

    private void ActivateSlot1()
    {
        ActivateEffect(active1, ActiveSlot.Active1);
    }

    private void ActivateSlot2()
    {
        ActivateEffect(active2, ActiveSlot.Active2);
    }

    private void ActivateEffect(ActiveCardRuntimeData data, ActiveSlot slot)
    {
        string slotName = slot == ActiveSlot.Active1 ? "Active1 / E" : "Active2 / Q";

        if (showDebug)
            Debug.Log($"[ActiveCardInputRouter] {slotName} pressed.");

        if (data == null || data.effect == null)
        {
            if (showDebug)
                Debug.Log($"[ActiveCardInputRouter] {slotName} has no active card assigned.");

            return;
        }

        if (data.effect is not IActiveCardEffect activeEffect)
        {
            Debug.LogWarning($"[ActiveCardInputRouter] {data.effect.GetType().Name} does not implement IActiveCardEffect.");
            return;
        }

        activeEffect.Activate();

        bool manualCooldown =
            data.effect is IManualActiveCardCooldown manual &&
            manual.UsesManualCooldown;

        // Important:
        // Abyssal Doll is manual, so the router must NOT start/reset HUD cooldown.
        if (startHudCooldownOnActivate && !manualCooldown)
            StartHUDCooldown(slot, data.cooldown);

        if (showDebug)
            Debug.Log($"[ActiveCardInputRouter] Activated {data.effect.GetType().Name} from {slotName}.");
    }

    public void StartCooldownForEffect(MonoBehaviour effect, float cooldown)
    {
        if (effect == null)
            return;

        if (active1 != null && active1.effect == effect)
        {
            StartHUDCooldown(ActiveSlot.Active1, cooldown);
            return;
        }

        if (active2 != null && active2.effect == effect)
        {
            StartHUDCooldown(ActiveSlot.Active2, cooldown);
            return;
        }
    }

    public void SetBusyForEffect(MonoBehaviour effect, bool busy)
    {
        if (effect == null)
            return;

        CacheHUD();

        if (activeCardHUD == null)
            return;

        if (active1 != null && active1.effect == effect)
        {
            activeCardHUD.SetActiveCard1Busy(busy);
            return;
        }

        if (active2 != null && active2.effect == effect)
        {
            activeCardHUD.SetActiveCard2Busy(busy);
            return;
        }
    }

    private void UpdateHUDSlot(ActiveSlot slot)
    {
        CacheHUD();

        if (activeCardHUD == null)
            return;

        ActiveCardRuntimeData data = GetSlotData(slot);

        if (data == null || data.effect == null || data.cardIcon == null)
        {
            if (slot == ActiveSlot.Active1)
                activeCardHUD.ClearActiveCard1();
            else
                activeCardHUD.ClearActiveCard2();

            return;
        }

        if (slot == ActiveSlot.Active1)
            activeCardHUD.AssignActiveCard1(data.cardIcon, data.cooldown);
        else
            activeCardHUD.AssignActiveCard2(data.cardIcon, data.cooldown);
    }

    private void StartHUDCooldown(ActiveSlot slot, float cooldown)
    {
        CacheHUD();

        if (activeCardHUD == null)
            return;

        if (slot == ActiveSlot.Active1)
            activeCardHUD.StartCooldownActive1(cooldown);
        else
            activeCardHUD.StartCooldownActive2(cooldown);
    }

    private void CacheHUD()
    {
        if (activeCardHUD == null)
            activeCardHUD = GetComponentInChildren<ActiveCardHUD>();
    }

    private ActiveCardRuntimeData GetSlotData(ActiveSlot slot)
    {
        return slot == ActiveSlot.Active1 ? active1 : active2;
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