using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawnAnchor : MonoBehaviour
{
    public Transform playerParent;
    public bool clearCarrierAfterSpawn = true;

    [Header("Camera")]
    [Tooltip("If empty, uses Camera.main.")]
    public CameraFollowTarget cameraFollow;

    [Tooltip("If true, camera follows the spawned player immediately.")]
    public bool assignCameraTargetOnSpawn = true;

    void Start()
    {
        var carrier = SelectionCarrier.Instance;
        if (carrier == null || carrier.selectedCharacter == null)
        {
            Debug.LogWarning("[PlayerSpawnAnchor] No selection to spawn.");
            return;
        }

        var data = carrier.selectedCharacter;
        if (!data.characterPrefab)
        {
            Debug.LogError("[PlayerSpawnAnchor] Character prefab is missing.");
            return;
        }

        SelectedContext.Current = data;

        var player = Instantiate(data.characterPrefab, transform.position, transform.rotation, playerParent);
        player.name = data.name + " (Player)";
        player.tag = "Player";

        PlayerLocator.Set(player.transform);

        AssignCameraTarget(player.transform);

        var identity = player.GetComponent<PlayerIdentity>();
        if (identity == null)
            identity = player.AddComponent<PlayerIdentity>();

        identity.characterData = data;

        var deckCopy = new List<CardData>(carrier.selectedCards);
        player.SendMessage("ReapplyFrom", deckCopy, SendMessageOptions.DontRequireReceiver);

        if (clearCarrierAfterSpawn)
            carrier.Clear();
    }

    private void AssignCameraTarget(Transform player)
    {
        if (!assignCameraTargetOnSpawn || player == null)
            return;

        if (cameraFollow == null && Camera.main != null)
            cameraFollow = Camera.main.GetComponent<CameraFollowTarget>();

        if (cameraFollow == null)
        {
            Debug.LogWarning("[PlayerSpawnAnchor] No CameraFollowTarget found on Main Camera.");
            return;
        }

        cameraFollow.SetTarget(player);
    }
}