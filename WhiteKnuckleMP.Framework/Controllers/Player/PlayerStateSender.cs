using System.Collections.Generic;
using System.Linq;
using Riptide;
using UnityEngine;
using WhiteKnuckleMP.Framework.Managers;
using WhiteKnuckleMP.Networking;
using WhiteKnuckleMP.Networking.Messages;

namespace WhiteKnuckleMP.Framework.Controllers.Player;

public class PlayerStateSender : MonoBehaviour
{
    private ushort _netID;
    private float _sendInterval = 1f / 60f; // Time between sending player state updates
    private float _sendTimer; // Timer to track time since last update

    private GameObject _leftHand = null!;
    private GameObject _rightHand = null!;
    
    // References
    private ENT_Player _localPlayer = null!;

    public void Initialize(ushort netId, GameObject leftHand, GameObject rightHand)
    {
        _netID = netId;
        _localPlayer = ENT_Player.GetPlayer();
        _leftHand = leftHand;
        _rightHand = rightHand;
    }

    private void Update()
    {
        _sendTimer += Time.deltaTime;
        if (_sendTimer < _sendInterval) return;
        _sendTimer = 0f;

        var leftHand = GetHand(0);
        var leftItem = leftHand.Keys.First()!;
        var leftHandPosition = leftHand.Values.First()!;
        
        var rightHand = GetHand(1);
        var rightItem = rightHand.Keys.First()!;
        var rightHandPosition = rightHand.Values.First();

        var playerSyncMessage = new PlayerSyncMessage(_netID, transform.position, transform.rotation, leftItem, rightItem, leftHandPosition, rightHandPosition);
        
        using (var packet = new NetworkPacket(MessageIds.PlayerSync, MessageSendMode.Unreliable))
        {
            playerSyncMessage.WriteTo(packet);

            // send over network
            NetworkManager.Instance.Client.Send(packet.RawMessage);
        }
    }

    /// <summary>
    /// Retrieves the item name and hand position for a given hand index.
    /// </summary>
    /// <param name="handIndex">The index of the hand (0 for left, 1 for right).</param>
    /// <returns>A dictionary containing the item name as the key and the hand position as the value.</returns>
    private Dictionary<string, Vector3> GetHand(int handIndex)
    {
        var hand = _localPlayer.hands[handIndex];
        var handItem = hand.inventoryHand.currentItem;
        var item = handItem != null ? handItem.itemName : "None";
        var pos = hand.GetHoldWorldPosition();

        if (!hand.IsFree() && item != "None")
        {
            var offset = hand.inventoryHand.handModel.targetOffset;
            
            pos -= hand.handModel.transform.rotation * offset;
        }
        
        return new Dictionary<string, Vector3>()
        {
            [item] = pos
        };
    }
}