using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PyroAbility2))]
public class PunishingGroundEffect : MonoBehaviour
{
    [Header("Hell Bomb Charges")]
    [Tooltip("How many bombs Pyro can plant before Hell Bomb goes on cooldown.")]
    public int bombsBeforeCooldown = 2;

    private PyroAbility2 _ability2;
    private int _originalBombsBeforeCooldown;
    private bool _registered;

    private void Awake()
    {
        _ability2 = GetComponent<PyroAbility2>();
    }

    private void OnEnable()
    {
        if (_registered) return;

        if (!_ability2)
            _ability2 = GetComponent<PyroAbility2>();

        if (!_ability2)
        {
            Debug.LogWarning("[PunishingGroundEffect] PyroAbility2 was not found on player.");
            return;
        }

        _originalBombsBeforeCooldown = _ability2.GetBombsBeforeCooldown();
        _ability2.SetBombsBeforeCooldown(Mathf.Max(1, bombsBeforeCooldown));

        _registered = true;
    }

    private void OnDisable()
    {
        if (!_registered || !_ability2) return;

        _ability2.SetBombsBeforeCooldown(_originalBombsBeforeCooldown);

        _registered = false;
    }
}