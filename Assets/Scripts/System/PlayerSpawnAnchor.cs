using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawnAnchor : MonoBehaviour
{
    public Transform playerParent;
    public bool clearCarrierAfterSpawn = true;

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

        var player = Instantiate(data.characterPrefab, transform.position, transform.rotation, playerParent);
        player.name = data.name + " (Player)";

        // Make sure it’s tagged correctly (optional but recommended)
        // Ensure the tag "Player" exists in Tags & Layers settings.
        player.tag = "Player";

        // >>> IMPORTANT: announce the spawned player
        PlayerLocator.Set(player.transform);

        // If your player has a deck applier with method ReapplyFrom(List<CardData>), this will call it.
        var deckCopy = new List<CardData>(carrier.selectedCards);
        player.SendMessage("ReapplyFrom", deckCopy, SendMessageOptions.DontRequireReceiver);

        if (clearCarrierAfterSpawn) carrier.Clear();
    }
}
