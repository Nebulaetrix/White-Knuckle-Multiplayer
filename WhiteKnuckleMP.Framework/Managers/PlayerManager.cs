using System.Collections.Generic;
using Riptide;
using UnityEngine;
using WhiteKnuckleMP.Framework.Controllers;
using WhiteKnuckleMP.Framework.Controllers.Player;
using WhiteKnuckleMP.Networking;

namespace WhiteKnuckleMP.Framework.Managers;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }
    public Dictionary<ushort, PlayerController> ActivePlayers = new();

    [MessageHandler((ushort)MessageId.PlayerSync)]
    public static void HandlePlayerSync(Riptide.Message message)
    {
        using (var packet = new NetworkPacket(message))
        {
            ushort netId = packet.ReadUShort();
            Vector3 pos = packet.ReadVector3();
            Quaternion rot = packet.ReadQuaternion();

            if (Instance.ActivePlayers.TryGetValue(netId, out var player))
            {
                if (!player.IsLocal != player.Receiver != null)
                {
                    player.Receiver.ApplyNetworkData(pos, rot, "None", "None");
                }
            }
        }
    }
}