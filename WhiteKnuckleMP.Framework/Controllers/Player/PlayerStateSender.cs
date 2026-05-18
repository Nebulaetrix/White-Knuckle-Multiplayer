using UnityEngine;
using WhiteKnuckleMP.Framework.Managers;
using WhiteKnuckleMP.Networking;

namespace WhiteKnuckleMP.Framework.Controllers.Player;

public class PlayerStateSender : MonoBehaviour
{
    private ushort _netID;
    private float _sendInterval = 1f / 60f; // Time between sending player state updates
    private float _sendTimer; // Timer to track time since last update
    
    // References
    private ENT_Player _localPlayer;

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

        using (var packet = new NetworkPacket((ushort)MessageId.PlayerSync))
        {
            packet.Write(_netID)
                .Write(transform.position)
                .Write(transform.rotation);
            // TODO: Add hand sync & Item Sync back

            NetworkManager.Instance.Client.Send(packet.RawMessage);
        }
    }

    private string GetItemName(int handIndex)
    {
        // TODO: Implement this back
        return "None";
    }
}