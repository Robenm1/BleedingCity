using UnityEngine;

public class XPCoin : MonoBehaviour
{
    [Header("XP Coin Settings")]
    [Tooltip("How much XP this coin gives toward the next level.")]
    public int xpValue = 5;

    [Tooltip("How long this coin exists before it despawns (seconds). 0 or negative = never despawn.")]
    public float lifetime = 10f;

    private float lifeTimer;

    private void Awake()
    {
        lifeTimer = lifetime;
    }

    private void Update()
    {
        if (lifetime > 0f)
        {
            lifeTimer -= Time.deltaTime;
            if (lifeTimer <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        PlayerXP playerXP = other.GetComponent<PlayerXP>();
        if (playerXP != null)
            playerXP.AddCoinPickup(xpValue);

        Destroy(gameObject);
    }
}
