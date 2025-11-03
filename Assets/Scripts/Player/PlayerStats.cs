using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Core Stats")]
    [Tooltip("Base movement speed (units/sec).")]
    public float moveSpeed = 5f;

    [Tooltip("Base attack/auto ability damage per bullet.")]
    public float baseDamage = 10f;

    [Tooltip("Seconds between auto-attacks/shots. 0.25 = 4 shots/sec.")]
    public float attackDelay = 0.25f;

    [Header("Combat")]
    [Tooltip("How far the player can detect and shoot enemies.")]
    public float attackRange = 6f;

    [Tooltip("Base projectile speed (units/sec).")]
    public float projectileSpeed = 12f;

    [Tooltip("Radius for picking up drops like XP, heals, etc.")]
    public float pickupRange = 2f;

    [Tooltip("Player max health.")]
    public float maxHealth = 100f;

    [Tooltip("Current health during runtime.")]
    public float currentHealth = 100f;

    [Header("Mitigation")]
    [Tooltip("Flat damage reduction applied first.")]
    public float armor = 0f;

    [Tooltip("Percent damage reduction. 0.2 = -20% incoming damage.")]
    [Range(0f, 1f)]
    public float damageReductionPercent = 0f;

    [Header("Ability / Utility")]
    [Tooltip("Global cooldown multiplier. 1 = normal, 0.8 = 20% faster abilities.")]
    public float cooldownMultiplier = 1f;

    [Tooltip("Extra movement multiplier from buffs/debuffs. 1 = normal.")]
    public float speedMultiplier = 1f;

    [Header("Dash")]
    [Tooltip("Dash speed (burst).")]
    public float dashSpeed = 12f;

    [Tooltip("How long dash lasts (seconds).")]
    public float dashDuration = 0.15f;

    [Tooltip("Dash cooldown (seconds).")]
    public float dashCooldown = 2f;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    // ===== Getters =====
    public float GetMoveSpeed() => moveSpeed * speedMultiplier;
    public float GetDamage() => baseDamage;
    public float GetPickupRange() => pickupRange;
    public float GetCooldownMultiplier() => cooldownMultiplier;
    public float GetDashSpeed() => dashSpeed;
    public float GetDashDuration() => dashDuration;
    public float GetDashCooldown() => dashCooldown;
    public float GetAttackDelay() => attackDelay;
    public float GetAttackRange() => attackRange;
    public float GetProjectileSpeed() => projectileSpeed;

    // ===== Health / Damage Handling =====
    public void TakeDamage(float rawAmount)
    {
        float afterFlat = Mathf.Max(rawAmount - armor, 0f);
        float final = afterFlat * (1f - damageReductionPercent);

        currentHealth -= final;
        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            Die();
        }
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }

    private void Die()
    {
        Debug.Log("[PlayerStats] Player died.");
    }
}
