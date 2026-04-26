using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class MirroredCloneDriver : MonoBehaviour
{
    [Header("Sources")]
    public Transform player;
    public PlayerStats playerStats;

    [Header("Input")]
    [Tooltip("If null, tries to read from a PlayerInput on the player: action 'Move' (Vector2).")]
    public InputActionReference moveAction;

    [Header("Tuning")]
    public float mirrorSpeedMultiplier = 1f;
    public bool invertX = true;
    public bool invertY = true;

    private Vector2 _lastMove;
    private Rigidbody2D _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (_rb)
        {
            _rb.bodyType = RigidbodyType2D.Kinematic; // clone stays kinematic
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
        }
    }

    private Vector2 ReadMove()
    {
        if (moveAction && moveAction.action != null)
            return moveAction.action.ReadValue<Vector2>();

        // Fallback: try read from player's PlayerInput ("Move" action)
        if (player)
        {
            var pi = player.GetComponent<PlayerInput>();
            if (pi != null)
            {
                var act = pi.actions.FindAction("Move");
                if (act != null) return act.ReadValue<Vector2>();
            }
        }
        return Vector2.zero;
    }

    private void Update()
    {
        if (!player || !playerStats) return;

        var mv = ReadMove();
        _lastMove = mv;

        // Mirror axes
        if (invertX) mv.x = -mv.x;
        if (invertY) mv.y = -mv.y;

        // Move speed from player stats
        float spd = playerStats.GetMoveSpeed() * Mathf.Max(0f, mirrorSpeedMultiplier);

        Vector3 delta = (Vector3)(mv.normalized * spd * Time.deltaTime);
        transform.position += delta;
    }
}
