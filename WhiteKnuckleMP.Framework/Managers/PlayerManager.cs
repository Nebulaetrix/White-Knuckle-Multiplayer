using System;
using System.Collections.Generic;
using Riptide;
using UnityEngine;
using WhiteKnuckleMP.Framework.Controllers;
using WhiteKnuckleMP.Framework.Controllers.Player;
using WhiteKnuckleMP.Networking;
using WhiteKnuckleMP.Networking.Messages;
using WhiteKnuckleMP.Utils;

namespace WhiteKnuckleMP.Framework.Managers;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; } = null!;
    public Dictionary<ushort, PlayerController> ActivePlayers = new();

    private void Awake()
    {
        Instance = this;

        StateManager.OnStateChanged += HandleStateChanged;
    }

    private void OnDestroy()
    {
        StateManager.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(StateManager.State oldState, StateManager.State newState)
    {
        if (newState == StateManager.State.Disconnected)
        {
            foreach (var player in ActivePlayers.Values)
            {
                if (player != null) Destroy(player.gameObject);
            }
            ActivePlayers.Clear();
            LogManager.Framework.Info("Cleared all networked player due to disconnection.");
        }
    }

    #region Message Handlers
    
    [MessageHandler((ushort)MessageId.PlayerSync)]
    public static void HandlePlayerSync(Riptide.Message message)
    {
        using var packet = new NetworkPacket(message);
        
        // Read data from packet
        PlayerSyncMessage data = PlayerSyncMessage.FromPacket(packet);
            
        ushort netId = data.NetId;
        Vector3 pos = data.Position;
        Quaternion rot = data.Rotation;
        string leftItem = data.LeftItemName;
        string rightItem = data.RightItemName;

        if (Instance.ActivePlayers.TryGetValue(netId, out var player))
        {
            if (!player.IsLocal && player.Receiver != null)
            {
                player.Receiver.ApplyNetworkData(pos, rot, leftItem, rightItem);
            }
        }
    }
    
    #endregion
}