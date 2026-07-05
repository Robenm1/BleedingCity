using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class AbyssalDollObject : EnemyHealth
{
    private enum WaveDamageMode
    {
        FlatDamage,
        EnemyMaxHealthPercent
    }

    [Header("Runtime Source")]
    [Tooltip("The player stats used to scale the doll wave damage.")]
    public PlayerStats playerStats;

    [Header("Lifetime")]
    [Tooltip("How long the doll stays on the ground.")]
    public float duration = 5f;

    [Tooltip("Maximum number of waves the doll can release before disappearing.")]
    public int maxWaves = 5;

    [Header("Wave")]
    [Tooltip("Radius of each wave.")]
    public float waveRadius = 4f;

    [Tooltip("Damage scaling. 1 = 100% of incoming source damage.")]
    public float damageScaling = 1f;

    [Tooltip("Enemy layers damaged by the wave.")]
    public LayerMask enemyLayers;

    [Header("Slow")]
    [Tooltip("How long enemies are slowed by the wave.")]
    public float slowDuration = 2f;

    [Tooltip("Enemy speed multiplier while slowed. 0.5 = 50% speed.")]
    public float slowMultiplier = 0.5f;

    [Header("Visuals")]
    [Tooltip("Parent object that gets scaled. Recommended: WaveHolder.")]
    public Transform waveVisualRoot;

    [Tooltip("SpriteRenderer used to show the wave. Usually on WaveVisual child.")]
    public SpriteRenderer waveSpriteRenderer;

    [Tooltip("Animator on the WaveVisual child.")]
    public Animator waveAnimator;

    [Tooltip("Exact animation state name used for the wave animation.")]
    public string waveAnimationStateName = "Wave";

    [Tooltip("How long the wave visual stays visible.")]
    public float waveVisualDuration = 0.7f;

    [Tooltip("Speed of the wave animation. 1 = normal, 0.5 = half speed.")]
    public float waveAnimationSpeed = 0.6f;

    [Tooltip("Original diameter of the wave visual in Unity world units before scaling. Start with 1.")]
    public float waveBaseDiameter = 1f;

    [Tooltip("Extra visual scale correction. Increase if visual is smaller than damage radius.")]
    public float waveVisualScaleMultiplier = 1f;

    [Header("Debug")]
    public bool showDebug = true;

    private float _timer;
    private int _wavesUsed;
    private Coroutine _waveVisualRoutine;

#if UNITY_EDITOR
    private void Reset()
    {
        AutoFindWaveRefs();
        ForceHideWaveVisualEditorSafe();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            AutoFindWaveRefs();
            ForceHideWaveVisualEditorSafe();
        }
    }

    private void AutoFindWaveRefs()
    {
        if (waveAnimator == null)
            waveAnimator = GetComponentInChildren<Animator>(true);

        if (waveSpriteRenderer == null && waveAnimator != null)
            waveSpriteRenderer = waveAnimator.GetComponent<SpriteRenderer>();

        if (waveVisualRoot == null && waveAnimator != null)
            waveVisualRoot = waveAnimator.transform.parent != null
                ? waveAnimator.transform.parent
                : waveAnimator.transform;
    }

    private void ForceHideWaveVisualEditorSafe()
    {
        if (waveAnimator != null)
            waveAnimator.enabled = false;

        if (waveSpriteRenderer != null)
            waveSpriteRenderer.enabled = false;

        if (waveVisualRoot != null)
            waveVisualRoot.gameObject.SetActive(false);
    }
#endif

    protected override void Awake()
    {
        // Fake EnemyHealth receiver only.
        // No real HP behavior, no normal enemy visuals.
        alwaysShowHPBar = false;

        // Important:
        // Keep this fake HP high enough so normal attacks do not kill the doll.
        // Max-HP attacks like Will-o'-the-wisp should use HitByEnemyMaxHealthPercent()
        // instead of calculating damage from the doll's fake maxHP.
        maxHP = 999999f;

        base.Awake();

        currentHP = maxHP;

        ForceHideWaveVisual();
    }

    private void OnEnable()
    {
        _timer = Mathf.Max(0.1f, duration);
        _wavesUsed = 0;
        currentHP = maxHP;

        ForceHideWaveVisual();
    }

    private void Update()
    {
        _timer -= Time.deltaTime;

        if (_timer <= 0f)
            DestroyDoll();
    }

    /// <summary>
    /// Normal player attacks already call TakeDamage on EnemyHealth.
    /// For the doll, incoming flat damage becomes the wave damage source.
    /// </summary>
    public override void TakeDamage(float dmg)
    {
        if (dmg <= 0f) return;

        ReleaseWave(WaveDamageMode.FlatDamage, dmg);

        // Never let the doll actually lose HP.
        currentHP = maxHP;
    }

    public override void TakeSummonDamage(float dmg)
    {
        // Summons do not trigger the doll.
        // If later you want summons to trigger it too, use:
        // TakeDamage(dmg);
    }

    /// <summary>
    /// Use this for attacks that normally deal percent of enemy max HP.
    /// Example: Will-o'-the-wisp 0.05f = 5% of each enemy's own maxHP.
    /// </summary>
    public void HitByEnemyMaxHealthPercent(float percent)
    {
        if (percent <= 0f) return;

        ReleaseWave(WaveDamageMode.EnemyMaxHealthPercent, percent);

        currentHP = maxHP;
    }

    /// <summary>
    /// Optional direct flat-damage call if a script wants to hit the doll manually.
    /// </summary>
    public void HitByPlayer(float incomingDamage)
    {
        if (incomingDamage <= 0f) return;

        ReleaseWave(WaveDamageMode.FlatDamage, incomingDamage);

        currentHP = maxHP;
    }

    private void ReleaseWave(WaveDamageMode damageMode, float damageValue)
    {
        if (_wavesUsed >= Mathf.Max(1, maxWaves)) return;

        _wavesUsed++;

        PlayWaveVisual();

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            Mathf.Max(0.1f, waveRadius),
            enemyLayers
        );

        foreach (var hit in hits)
        {
            if (hit == null) continue;

            // Do not hit this doll or other dolls.
            if (hit.GetComponent<AbyssalDollObject>() != null) continue;
            if (hit.GetComponentInParent<AbyssalDollObject>() != null) continue;

            var enemy = hit.GetComponent<EnemyHealth>();
            if (enemy == null) enemy = hit.GetComponentInParent<EnemyHealth>();
            if (enemy == null) continue;

            float damage = CalculateWaveDamage(enemy, damageMode, damageValue);
            if (damage <= 0f) continue;

            enemy.TakeDamage(damage);

            var slow = enemy.GetComponent<DemonicSlowEffect>();
            if (!slow) slow = enemy.gameObject.AddComponent<DemonicSlowEffect>();

            slow.ApplySlow(slowMultiplier, slowDuration);
        }

        if (showDebug)
        {
            Debug.Log(
                $"[AbyssalDollObject] Wave {_wavesUsed}/{maxWaves}, " +
                $"mode: {damageMode}, source value: {damageValue:F3}"
            );
        }

        if (_wavesUsed >= Mathf.Max(1, maxWaves))
            DestroyDoll();
    }

    private float CalculateWaveDamage(EnemyHealth enemy, WaveDamageMode damageMode, float damageValue)
    {
        float scale = Mathf.Max(0f, damageScaling);

        switch (damageMode)
        {
            case WaveDamageMode.FlatDamage:
                return Mathf.Max(0f, damageValue) * scale;

            case WaveDamageMode.EnemyMaxHealthPercent:
                if (enemy == null) return 0f;
                return enemy.maxHP * Mathf.Max(0f, damageValue) * scale;

            default:
                return 0f;
        }
    }

    private void PlayWaveVisual()
    {
        if (waveVisualRoot == null) return;

        if (_waveVisualRoutine != null)
            StopCoroutine(_waveVisualRoutine);

        _waveVisualRoutine = StartCoroutine(PlayWaveVisualRoutine());
    }

    private IEnumerator PlayWaveVisualRoutine()
    {
        // Hide first so no unscaled/small frame can appear.
        if (waveSpriteRenderer != null)
            waveSpriteRenderer.enabled = false;

        if (waveAnimator != null)
            waveAnimator.enabled = false;

        ScaleWaveVisualToRadius();

        if (waveVisualRoot != null)
            waveVisualRoot.gameObject.SetActive(true);

        if (waveSpriteRenderer != null)
            waveSpriteRenderer.enabled = true;

        if (waveAnimator != null)
        {
            waveAnimator.enabled = true;
            waveAnimator.speed = Mathf.Max(0.05f, waveAnimationSpeed);

            // Force replay from the first frame every hit.
            waveAnimator.Play(waveAnimationStateName, 0, 0f);
            waveAnimator.Update(0f);
        }

        yield return new WaitForSeconds(Mathf.Max(0.05f, waveVisualDuration));

        ForceHideWaveVisual();
    }

    private void ForceHideWaveVisual()
    {
        if (_waveVisualRoutine != null)
        {
            StopCoroutine(_waveVisualRoutine);
            _waveVisualRoutine = null;
        }

        if (waveAnimator != null)
            waveAnimator.enabled = false;

        if (waveSpriteRenderer != null)
            waveSpriteRenderer.enabled = false;

        if (waveVisualRoot != null)
            waveVisualRoot.gameObject.SetActive(false);
    }

    private void ScaleWaveVisualToRadius()
    {
        if (waveVisualRoot == null) return;

        float targetDiameter = Mathf.Max(0.1f, waveRadius) * 2f;
        float baseDiameter = Mathf.Max(0.01f, waveBaseDiameter);

        float scale = targetDiameter / baseDiameter;
        scale *= Mathf.Max(0.01f, waveVisualScaleMultiplier);

        waveVisualRoot.localScale = Vector3.one * scale;
    }

    public void HideWaveVisual()
    {
        ForceHideWaveVisual();
    }

    private void DestroyDoll()
    {
        ForceHideWaveVisual();

        if (hpUIRoot != null)
            Destroy(hpUIRoot.gameObject);

        Destroy(gameObject);
    }

    protected override void Die()
    {
        // No XP, no coins, no normal enemy death behavior.
        DestroyDoll();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.45f, 0.05f, 0.75f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.1f, waveRadius));
    }
#endif
}