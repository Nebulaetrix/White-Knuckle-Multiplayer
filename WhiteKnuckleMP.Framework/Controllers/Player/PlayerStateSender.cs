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
    
    // References
    private ENT_Player _localPlayer = null!;

    public void Initialize(ushort netId)
    {
        _netID = netId;
        _localPlayer = ENT_Player.GetPlayer();
    }

    private void Update()
    {
        _sendTimer += Time.deltaTime;
        if (_sendTimer < _sendInterval) return;
        _sendTimer = 0f;

        var leftItem = GetItemName(0);
        var rightItem = GetItemName(1);

        var playerSyncMessage = new PlayerSyncMessage(_netID, transform.position, transform.rotation, leftItem, rightItem);
        
        using (var packet = new NetworkPacket(MessageIds.PlayerSync, MessageSendMode.Unreliable))
        {
            playerSyncMessage.WriteTo(packet);

            // send over network
            NetworkManager.Instance.Client.Send(packet.RawMessage);
        }
    }

    private string GetItemName(int handIndex)
    {
        // TODO: Implement this back
        return "None";
    }
}