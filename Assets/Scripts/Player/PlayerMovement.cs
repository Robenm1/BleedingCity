using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerStats))]
public class PlayerMovement : MonoBehaviour
{
    // Fired the instant a dash actually starts (hook your UI to this)
    public event Action OnDashStarted;

    [Header("Movement")]
    [Tooltip("Multiplier applied on top of PlayerStats.moveSpeed (for testing/tuning).")]
    [SerializeField] private float moveMultiplier = 1f;

    [Tooltip("If true, input is normalized so diagonals aren't faster.")]
    [SerializeField] private bool normalizeInput = true;

    [Header("Dash")]
    [Tooltip("Minimum input magnitude required to dash (prevents 'zero-direction' dashes).")]
    [SerializeField, Range(0f, 0.5f)] private float minDashInput = 0.1f;

    [Tooltip("If true, you can dash again while holding a direction at the exact cooldown end; otherwise require re-press via PlayerControls.")]
    [SerializeField] private bool allowInstantRedash = true;

    private Rigidbody2D rb;
    private PlayerStats stats;
    private OwlCloneAbility cloneAbility; // NEW: reference to clone ability

    // input/state
    private Vector2 moveInputRaw;
    private Vector2 moveInput;          // possibly normalized
    private Vector2 lastNonZeroMove;    // used for dash direction when not moving

    // dash runtime
    private bool isDashing;
    private float dashTimer;
    private float dashCooldownTimer;
    private Vector2 dashDir;

    // Out of the Ordinary runtime
    private bool extraDashEnabled;
    private float extraDashWindowDuration = 0.8f;
    private bool extraDashWindowActive;
    private bool extraDashAvailable;
    private float extraDashWindowTimer;

    private PyroAbility1 pyroAbility;

    // cache per-frame
    private float baseMoveSpeed
    {
        get
        {
            float speed = (stats != null ? stats.GetMoveSpeed() : 5f) * moveMultiplier;

            // Apply Pyro Ability 1 speed buff if active
            if (pyroAbility != null)
            {
                speed *= pyroAbility.GetSpeedMultiplier();
            }

            return speed;
        }
    }

    private float dashSpeed => (stats != null ? stats.GetDashSpeed() : 12f);
    private float dashDuration => (stats != null ? stats.GetDashDuration() : 0.15f);
    private float dashCooldown => (stats != null ? stats.GetDashCooldown() : 2f);

    private void Awake()
    {
        pyroAbility = GetComponent<PyroAbility1>();
        rb = GetComponent<Rigidbody2D>();
        stats = GetComponent<PlayerStats>();
        cloneAbility = GetComponent<OwlCloneAbility>(); // NEW: find clone ability

        // Recommended Rigidbody2D settings for top-down:
        rb.gravityScale = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.freezeRotation = true;
        // BodyType should be Dynamic in the Inspector (enemies Kinematic if you don't want to push them).
    }

    private void Update()
    {
        // Dash timers
        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;

            if (dashCooldownTimer < 0f)
                dashCooldownTimer = 0f;
        }

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
            {
                isDashing = false;
                dashTimer = 0f;
                // remain on cooldown until dashCooldownTimer reaches 0
            }
        }

        // Out of the Ordinary blue window timer
        if (extraDashWindowActive)
        {
            extraDashWindowTimer -= Time.deltaTime;

            if (extraDashWindowTimer <= 0f)
            {
                extraDashWindowTimer = 0f;
                EndExtraDashWindow(startCooldown: true);
            }
        }

        // Remember last non-zero move (for dash direction)
        if (moveInput.sqrMagnitude > 0.0001f)
            lastNonZeroMove = moveInput;
    }

    private void FixedUpdate()
    {
        if (isDashing)
        {
            // Fixed dash movement (ignores regular input)
            Vector2 next = rb.position + dashDir * dashSpeed * Time.fixedDeltaTime;
            rb.MovePosition(next);
            return;
        }

        // Regular movement
        Vector2 velocity = moveInput * baseMoveSpeed;
        Vector2 target = rb.position + velocity * Time.fixedDeltaTime;
        rb.MovePosition(target);
    }

    /// <summary>
    /// Called by PlayerControls every frame with the Move action value.
    /// </summary>
    public void SetMoveInput(Vector2 input)
    {
        moveInputRaw = input;
        moveInput = normalizeInput ? Vector2.ClampMagnitude(input, 1f) : input;
    }

    /// <summary>
    /// Called by PlayerControls when Dash input is performed.
    /// This checks cooldown/timers and starts a dash if possible.
    /// NEW: If clone exists, swaps with clone instead of dashing.
    /// </summary>
    public void TryDash()
    {
        // NEW: Check if clone ability wants to handle this (swap instead of dash)
        if (cloneAbility != null && cloneAbility.TrySwapWithClone())
        {
            return; // Clone ability handled it (swap occurred)
        }

        // Normal dash logic
        if (isDashing) return;

        // If the blue window is not active, normal cooldown blocks dash.
        if (!extraDashWindowActive)
        {
            if (dashCooldownTimer > 0f) return;
        }

        // If the blue window is active but the extra dash was already consumed, block.
        if (extraDashWindowActive && !extraDashAvailable)
            return;

        // Determine dash direction:
        Vector2 desiredDir = moveInput.sqrMagnitude >= (minDashInput * minDashInput)
            ? moveInput.normalized
            : (lastNonZeroMove.sqrMagnitude > 0.0001f ? lastNonZeroMove.normalized : Vector2.up); // default up if never moved

        // If still no meaningful direction, cancel
        if (desiredDir.sqrMagnitude < 0.0001f) return;

        // Is this dash the extra dash from Out of the Ordinary?
        bool consumedExtraDash = extraDashWindowActive && extraDashAvailable;

        if (consumedExtraDash)
        {
            extraDashAvailable = false;
            EndExtraDashWindow(startCooldown: false);
        }

        // Start dash
        isDashing = true;
        dashDir = desiredDir;
        dashTimer = Mathf.Max(0.01f, dashDuration);

        if (consumedExtraDash)
        {
            // After the extra dash, start normal cooldown.
            dashCooldownTimer = Mathf.Max(dashCooldown, dashTimer);
        }
        else if (extraDashEnabled)
        {
            // First dash with Out of the Ordinary selected:
            // open the blue second-dash window instead of starting cooldown immediately.
            BeginExtraDashWindow();
        }
        else
        {
            // Normal dash behavior.
            dashCooldownTimer = Mathf.Max(dashCooldown, dashTimer);
        }

        OnDashStarted?.Invoke();
    }

    // ===== Out of the Ordinary API =====

    public void EnableExtraDashWindow(float windowDuration)
    {
        extraDashEnabled = true;
        extraDashWindowDuration = Mathf.Max(0.05f, windowDuration);
    }

    public void DisableExtraDashWindow()
    {
        extraDashEnabled = false;

        if (extraDashWindowActive || extraDashAvailable)
            EndExtraDashWindow(startCooldown: true);
    }

    private void BeginExtraDashWindow()
    {
        extraDashAvailable = true;
        extraDashWindowActive = true;
        extraDashWindowTimer = Mathf.Max(0.05f, extraDashWindowDuration);
    }

    private void EndExtraDashWindow(bool startCooldown)
    {
        if (!extraDashWindowActive && !extraDashAvailable) return;

        extraDashWindowActive = false;
        extraDashAvailable = false;
        extraDashWindowTimer = 0f;

        if (startCooldown)
            dashCooldownTimer = Mathf.Max(dashCooldown, dashTimer);
    }

    public bool IsExtraDashWindowActive()
    {
        return extraDashWindowActive;
    }

    public float GetExtraDashWindowNormalized()
    {
        if (!extraDashWindowActive) return 0f;
        return Mathf.Clamp01(extraDashWindowTimer / Mathf.Max(0.0001f, extraDashWindowDuration));
    }

    public float GetExtraDashWindowRemaining()
    {
        return Mathf.Max(0f, extraDashWindowTimer);
    }

    // ===== Optional helpers for UI/logic =====

    /// <summary> Returns true while the dash burst is active. </summary>
    public bool IsDashing() => isDashing;

    /// <summary> Normalized remaining dash cooldown (1 = just started cooldown, 0 = ready). </summary>
    public float GetDashCooldownNormalized()
    {
        if (dashCooldown <= 0f) return 0f;
        return Mathf.Clamp01(dashCooldownTimer / dashCooldown);
    }

    /// <summary> How many seconds remain on dash cooldown. </summary>
    public float GetDashCooldownRemaining() => Mathf.Max(0f, dashCooldownTimer);
}