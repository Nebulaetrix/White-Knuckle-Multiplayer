using Riptide;
using White_Knuckle_Multiplayer.Networking.Messages;

namespace White_Knuckle_Multiplayer.Networking.Routing;

    /// <summary>
    /// Static class for sending Messages over the network
    /// </summary>
    public static class MessageSender
    {
        // Sending Request on join, server authorizes and keeps track of this
        public static void SendJoinRequest(JoinRequestData data)
        {
            Riptide.Message msg = Riptide.Message.Create(MessageSendMode.Reliable, (ushort)MessageID.JoinRequest);
            
            msg.AddSerializable(data);

            NetworkClient.Instance.Client.Send(msg);
        }

        // Sending Player Object Data
        public static void SendPlayerData(PlayerData data)
        {
            Riptide.Message msg = Riptide.Message.Create(MessageSendMode.Unreliable, (ushort)MessageID.PlayerDataSync);
            
            msg.AddSerializable(data);

            if (NetworkClient.Instance?.Client != null && NetworkClient.Instance.Client.IsConnected)
            {
                NetworkClient.Instance.Client.Send(msg);
                return;
            }

            if (NetworkServer.Instance?.Server != null && NetworkServer.Instance.Server.IsRunning)
            {
                NetworkServer.Instance.Server.SendToAll(msg, data.NetID);
                return;
            }

            LogManager.Net.Error("Cannot send PlayerDataSync: no client or server available.");
        }

        // Sending Scene Change
        // TODO: Replace this with actual level synchronization
        public static void SendLevelData(string[] sceneName)
        {
            /*var data = new SceneChangeData(sceneName);
            var msg = Riptide.Message.Create(MessageSendMode.Reliable, (ushort)MessageID.SceneChange);
            msg.AddSerializable(data);
            // Host is both server & client
            if (NetworkServer.Instance.IsActive)
                NetworkServer.Instance.Server.SendToAll(msg);
            else
                NetworkClient.Instance.Client.Send(msg);*/
        }

        // Sending DespawnPlayer to all connected clients
        public static void SendDespawn(ushort netID)
        {
            var data = new DespawnPlayerData(netID);
            var msg = Riptide.Message.Create(MessageSendMode.Reliable, (ushort)MessageID.DespawnPlayer);
            msg.AddSerializable(data);
            if (NetworkServer.Instance.IsActive)
                NetworkServer.Instance.Server.SendToAll(msg, netID);
        }
    }
