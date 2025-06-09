using System.Linq;
using Riptide;
using White_Knuckle_Multiplayer.Managers;
using White_Knuckle_Multiplayer.Networking.Messages;
using White_Knuckle_Multiplayer.Networking.Routing;
using White_Knuckle_Multiplayer.Utils;

namespace White_Knuckle_Multiplayer.Networking;

// SERVER‑SIDE HANDLERS

public partial class MessageHandler
{
    // Handles JoinRequest,
    // Spawns player for itself, relays a message to all connected clients to also spawn the new player
    [WKMessageHandler((ushort)MessageID.JoinRequest, (byte)GroupID.Server)]
    private static void HandleJoinRequest_Server(ushort clientId, Riptide.Message msg)
    {
        JoinRequestData data = msg.GetSerializable<JoinRequestData>();
        var modListReceived = data.ModList;
        var localModList = ModListHelper.GetLoadedModsList();
        var comparison = ModListHelper.CompareModLists(localModList, modListReceived);
        
        LogManager.Server.Info(
            $"JoinRequest from {clientId}: {data.Username}"
        );
        
        if (comparison.MalformedEntries.Any())
            LogManager.Server.Warn($"Malformed ModList entries: {string.Join("\n", comparison.MalformedEntries)}");

        if (!comparison.AllMatch)
        {
            if (comparison.MissingOnA.Any())
            {
                LogManager.Server.Warn($"Client has extra mods: {string.Join(", ", comparison.MissingOnA)}");
            }

            if (comparison.MissingOnB.Any())
            {
                LogManager.Server.Warn($"Client is missing mods: {string.Join(", ", comparison.MissingOnB)}");
            }
            
            foreach (var kv in comparison.VersionMismatches)
            {
                LogManager.Server.Warn(
                    $"Version mismatch for {kv.Key}: host={kv.Value.VersionA}; client={kv.Value.VersionB}"    
                );
            }
        }
        else
        {
            LogManager.Server.Info($"ModLists match!\n{string.Join("\n", modListReceived)}");
        }
        
        var states = ModCompability.CheckModList(modListReceived, false);

        LogManager.Info("Mod States:");
        foreach (var kv in states)
        {
            LogManager.Info($"{kv.Key} -> {kv.Value}");
        }
        

        // Broadcast spawn to everyone
        //Riptide.Message spawnMsg = Riptide.Message.Create(MessageSendMode.Reliable, (ushort)MessageID.SpawnPlayer);
        //spawnMsg.AddSerializable(new SpawnPlayerData(clientId));
        //NetworkServer.Instance.Server.SendToAll(spawnMsg);

        // Above replaced by States
        // Send only player state update, which will handle spawning if ready
        if (PlayerStateManager.Instance != null)
        {
            PlayerStateManager.Instance.UpdatePlayerState(clientId, PlayerStateManager.PlayerState.InGame, data.Username);
        }
        
        
        // Always tell the new Client about the host client, don't send this to host himself
        if (clientId != 1)
        {
            LogManager.Server.Info("Telling new client about host");
            Riptide.Message hostMessage = Riptide.Message.Create(MessageSendMode.Reliable, (ushort)MessageID.SpawnPlayer);
            hostMessage.AddSerializable(new SpawnPlayerData((ushort)1));
            NetworkServer.Instance.Server.Send(hostMessage, clientId);
        }
        

        // Tell new client about existing players
        foreach (ushort existingID in Instance._players.Keys)
        {
            LogManager.Server.Info($"Sending SpawnPlayer for ID {existingID}; Not Sending to {clientId}");
            Riptide.Message m = Riptide.Message.Create(MessageSendMode.Reliable, (ushort)MessageID.SpawnPlayer);
            m.AddSerializable(new SpawnPlayerData(existingID));
            NetworkServer.Instance.Server.Send(m, clientId);
        }
    }

    // Handles PlayerData synchronization
    // this one acts as a relay, because the server doesn't need to do anything else with this
    [WKMessageHandler((ushort)MessageID.PlayerDataSync, (byte)GroupID.Server)]
    private static void HandlePlayerDataSync_Server(ushort fromClientId, Riptide.Message msg)
    {
        // Relay to all except sender
        NetworkServer.Instance.Server.SendToAll(msg, fromClientId);
    }

    [WKMessageHandler((ushort)MessageID.PlayerStateUpdate, (byte)GroupID.Server)]
    private static void HandlePlayerStateUpdate_Server(ushort fromClientId, Riptide.Message msg)
    {
        PlayerStateUpdateData data = msg.GetSerializable<PlayerStateUpdateData>();
        LogManager.Client.Info($"Player {fromClientId} state update: {data.State}");
        
        // Update server's player state tracking
        if (PlayerStateManager.Instance != null)
        {
            PlayerStateManager.Instance.UpdatePlayerState(data.NetID, data.State, data.Username);
        }
        
        // Relay to all other clients
        NetworkServer.Instance.Server.SendToAll(msg, fromClientId);
        
        // If player just became ready, send spawn message
        if (data.State == PlayerStateManager.PlayerState.InGame)
        {
            LogManager.Server.Info($"Player {fromClientId} is now ready - sending spawn message");
            Riptide.Message spawnMsg = Riptide.Message.Create(MessageSendMode.Reliable, (ushort)MessageID.SpawnPlayer);
            spawnMsg.AddSerializable(new SpawnPlayerData(fromClientId));
            NetworkServer.Instance.Server.Send(spawnMsg, fromClientId);
        }
    }

    // Handles SceneChange
    // acts as a relay to other clients
    // TODO: Replace with actual level synchronization
    [WKMessageHandler((ushort)MessageID.SceneChange, (byte)GroupID.Server)]
    private static void HandleSceneChange_Server(ushort fromClientId, Riptide.Message msg)
    {
        // Relay scene‐change
        NetworkServer.Instance.Server.SendToAll(msg);
    }
}