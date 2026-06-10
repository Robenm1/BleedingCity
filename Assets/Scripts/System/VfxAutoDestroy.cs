using UnityEngine;

/// <summary>Destroys the GameObject after a fixed delay. Use on one-shot VFX prefabs so they clean themselves up after the animation finishes.</summary>
public class VfxAutoDestroy : MonoBehaviour
{
    [Tooltip("Time in seconds before the GameObject destroys itself.")]
    public float lifetime = 1f;

    private void OnEnable()
    {
        Destroy(gameObject, lifetime);
    }
}
