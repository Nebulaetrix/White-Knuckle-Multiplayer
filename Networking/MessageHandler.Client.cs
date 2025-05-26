using Riptide;
using White_Knuckle_Multiplayer.Networking.Controllers;
using White_Knuckle_Multiplayer.Networking.Messages;
using White_Knuckle_Multiplayer.Networking.Routing;

namespace White_Knuckle_Multiplayer.Networking;

// CLIENT‑SIDE HANDLERS (groupId = 1)

public partial class MessageHandler
{
    // Handles Spawning the player on the local game
    // Gets the data, and spawn player with their NetworkID so they can be manipulated easily
    [WKMessageHandler((ushort)MessageID.SpawnPlayer, (byte)GroupID.Client)]
    private static void HandleSpawnPlayer_Client(Riptide.Message msg)
    {
        SpawnPlayerData data = msg.GetSerializable<SpawnPlayerData>();
        LogManager.Client.Info($"SpawnPlayer for ID {data.NetID}");
        Instance.SpawnPlayer_Internal(data.NetID);
    }

    // Handles Despawning players
    // Handles the Message for despawning a client, this happens when the other client disconnects,
    // Server notifies all other clients that this client disconnected
    [WKMessageHandler((ushort)MessageID.DespawnPlayer, (byte)GroupID.Client)]
    private static void HandleDespawnPlayer_Client(Riptide.Message msg)
    {
        DespawnPlayerData data = msg.GetSerializable<DespawnPlayerData>();
        LogManager.Client.Info($"DespawnPlayer for ID {data.NetID}");
        Instance.DespawnPlayer(data.NetID);
    }

    // Handles Player Data Synchronization
    // Gets the data for a client identified with NetworkID
    // Gets the networked clone and manipulates it
    [WKMessageHandler((ushort)MessageID.PlayerDataSync, (byte)GroupID.Client)]
    private static void HandlePlayerDataSync_Client(Riptide.Message msg)
    {
        PlayerData data = msg.GetSerializable<PlayerData>();
        
        // Get the network clone
        if (Instance._players.TryGetValue(data.NetID, out var go))
        {
            PlayerNetworkController playerNetworkController = go.GetComponent<PlayerNetworkController>();
            
            // Separating game logic from network logic
            // Passes data recieved from the message to the components to do synchronization
            playerNetworkController.UpdateHands(
                data.HandLeftPosition, data.HandRightPosition,
                data.HandLeftState, data.HandRightState, data.HandLeftColor, data.HandRightColor
            );
            playerNetworkController.UpdateHandItems(data.LeftHandItemPrefabName, data.RightHandItemPrefabName);
            playerNetworkController.UpdatePositionRotation(data.Position, data.Rotation);
        }
    }

    // Handles SceneChange
    // already done by Nebby
    [WKMessageHandler((ushort)MessageID.SceneChange, (byte)GroupID.Client)]
    private static void HandleSceneChange_Client(Riptide.Message msg)
    {
        SceneChangeData data = msg.GetSerializable<SceneChangeData>();
        LogManager.Client.Info($"Loading scene {data.SceneName}");
        UnityEngine.SceneManagement.SceneManager.LoadScene(data.SceneName);
    }
}