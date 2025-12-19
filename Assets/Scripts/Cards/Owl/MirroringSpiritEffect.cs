using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerStats))]
public class MirroringSpiritEffect : MonoBehaviour
{
    [Header("Runtime (from SO)")]
    public float mirrorSpeedMultiplier = 1f;
    public bool invertX = true;
    public bool invertY = true;

    private OwlCloneAbility _cloneAbility;
    private PlayerStats _stats;
    private GameObject _lastClone;

    private void Awake()
    {
        _stats = GetComponent<PlayerStats>();
        _cloneAbility = GetComponent<OwlCloneAbility>();
        if (!_cloneAbility)
            Debug.LogWarning("[MirroringSpiritEffect] OwlCloneAbility not found on player.");
    }

    private void Update()
    {
        if (!_cloneAbility) return;

        var clone = _cloneAbility.GetActiveClone(); // needs small accessor (see section 3)

        // If clone changed, (re)wire the driver
        if (clone != _lastClone)
        {
            // cleanup old
            if (_lastClone)
            {
                var oldDrv = _lastClone.GetComponent<MirroredCloneDriver>();
                if (oldDrv) Destroy(oldDrv);
            }

            _lastClone = clone;

            // add new
            if (_lastClone)
            {
                var drv = _lastClone.GetComponent<MirroredCloneDriver>();
                if (!drv) drv = _lastClone.AddComponent<MirroredCloneDriver>();

                drv.player = this.transform;
                drv.playerStats = _stats;
                drv.mirrorSpeedMultiplier = mirrorSpeedMultiplier;
                drv.invertX = invertX;
                drv.invertY = invertY;

                // Optional: keep clone kinematic; driver moves by transform.
            }
        }
    }
}
