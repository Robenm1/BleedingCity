using UnityEngine;

[DisallowMultipleComponent]
public class FeatherOwnerLink : MonoBehaviour
{
    public Transform owner;            // player or clone owner
    public PlayerStats ownerStats;     // used to scale damage, range, etc.

    public void StampOwner(Transform t, PlayerStats stats)
    {
        owner = t;
        ownerStats = stats;
    }
}
