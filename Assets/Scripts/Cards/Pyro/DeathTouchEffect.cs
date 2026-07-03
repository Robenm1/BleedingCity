using UnityEngine;

[DisallowMultipleComponent]
public class DeathTouchEffect : MonoBehaviour
{
    [Header("Death Touch")]
    [Tooltip("Total damage multiplier applied before converting the hit into DoT. 1.10 = +10% damage.")]
    public float damageMultiplier = 1.10f;

    [Tooltip("How long the converted damage lasts.")]
    public float dotDuration = 3f;

    [Tooltip("How often the DoT applies damage.")]
    public float tickInterval = 0.25f;

    private bool _registered;

    private static int _activeCount;
    private static float _damageMultiplier = 1.10f;
    private static float _dotDuration = 3f;
    private static float _tickInterval = 0.25f;

    public static bool IsActive => _activeCount > 0;

    private void OnEnable()
    {
        if (_registered) return;

        _damageMultiplier = Mathf.Max(0f, damageMultiplier);
        _dotDuration = Mathf.Max(0.05f, dotDuration);
        _tickInterval = Mathf.Max(0.05f, tickInterval);

        _activeCount++;
        _registered = true;
    }

    private void OnDisable()
    {
        if (!_registered) return;

        _activeCount = Mathf.Max(0, _activeCount - 1);
        _registered = false;
    }

    public static bool TryConvertToDot(EnemyHealth enemy, float dmg)
    {
        if (!IsActive) return false;
        if (enemy == null) return false;
        if (dmg <= 0f) return true;

        var dot = enemy.GetComponent<DeathTouchDot>();
        if (!dot) dot = enemy.gameObject.AddComponent<DeathTouchDot>();

        float totalDamage = dmg * Mathf.Max(0f, _damageMultiplier);
        dot.AddDamage(totalDamage, _dotDuration, _tickInterval);

        return true;
    }
}