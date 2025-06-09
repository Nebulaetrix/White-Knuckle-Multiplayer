using System;
using UnityEngine;
using Riptide;
using Riptide.Utils;
using White_Knuckle_Multiplayer.Managers;
using White_Knuckle_Multiplayer.Networking.Messages;
using White_Knuckle_Multiplayer.Networking.Routing;
using White_Knuckle_Multiplayer.Networking.Transports.Steam;
using White_Knuckle_Multiplayer.Utils;

namespace White_Knuckle_Multiplayer.Networking
{

    public class NetworkClient : MonoBehaviour
    {
        public static NetworkClient Instance { get; private set; }
        public Client Client { get; private set; }
        public SteamClient SteamClient { get; private set; }

        [SerializeField] public string connectionAddress = "localhost";
        [SerializeField] public ushort connectionPort = 7777;

        private void Awake()
        {
            // Keeping only one Instance
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Setting up Riptide Logger
            RiptideLogger.Initialize(LogManager.Client.Debug, LogManager.Client.Info, LogManager.Client.Warn, LogManager.Client.Error, false);
        }

        private void Start()
        {
            SteamClient = new SteamClient();
            Client = new Client(SteamClient);
        }
        
        private void Update()
        {
            if (Client != null)
            {
                Client.Update();
            }
        }

        private void OnApplicationQuit()
        {
            if (Client != null)
            { 
                Client.Disconnect();
            }
        }

        public void StartClient(string ip = "localhost", ushort port = 7777, string transport = "udp", bool isHost = false)
        {
            if (Client != null && Client.IsConnected)
            {
                LogManager.Client.Info("Client already connected");
                return;
            }

            if (transport.ToLower() == "steam")
            {
                // IMPLEMENTED HORAAAAYYYY
                if (isHost)
                {
                    Client = new Client(new SteamClient(NetworkServer.Instance.SteamServer));
                }
                else
                {
                    // NetworkServer.Instance.StartSteamServer();
                    var steamClient = new SteamClient(NetworkServer.Instance.SteamServer);
                    
                    Client = new Client(steamClient);
                }
            }
            else
            {
                // Local LAN hosting, or fallback
                Client = new Client();
            }

            Client.MessageReceived += OnClientMessageReceived;
            Client.Connected += OnConnected;
            Client.Disconnected += OnDisconnected;
            Client.ConnectionFailed += OnConnectionFailed;
            string connectString;
            if (transport == "steam")
                connectString = $"{ip}";
            else
                connectString = $"{ip}:{port}";
            
            Client.Connect(connectString, maxConnectionAttempts: 5, messageHandlerGroupId: (byte)GroupID.Client, useMessageHandlers: false);
        }

        public void Disconnect()
        {
            if (Client != null && Client.IsConnected)
            {
                Client.Disconnect();
            }
        }
        
        private void OnConnected(object sender, EventArgs e)
        {
            LogManager.Client.Info("Client Connected");
            string username;
            try
            {
                username = Steamworks.SteamFriends.GetPersonaName();
            }
            catch
            {
                username = $"Player_{Client.Id}";
            }

            // Update player state
            if (PlayerStateManager.Instance != null)
            {
                PlayerStateManager.Instance.StartAsClient(true);
            }
            //var modList = ModListHelper.GetLoadedModsList();
            //MessageSender.SendJoinRequest(new JoinRequestData(username, MyPluginInfo.PLUGIN_VERSION, modList));
        }

        private void OnConnectionFailed(object sender, EventArgs e)
        {
            LogManager.Client.Error($"Client Failed to Connect");
        }

        private void OnDisconnected(object sender, EventArgs e)
        {
            LogManager.Client.Info("Client Disconnected");

            // Update PlayerState
            if (PlayerStateManager.Instance != null)
            {
                PlayerStateManager.Instance.HandleDisconnect();
            }
            
            
            // Clean up spawned players
            foreach (ushort netID in MessageHandler.Instance._players.Keys)
            {
                MessageHandler.Instance.DespawnPlayer(netID);
            }
        }

        private void OnClientMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            var messageID = e.MessageId;
            var fromClientID = e.FromConnection.Id;
            var msg = e.Message;
            
            MessageRouter.Route(messageID, (byte)GroupID.Client, fromClientID, msg);
        }

        private void OnDisable()
        {
            Client.MessageReceived -= OnClientMessageReceived;
            Client.Connected -= OnConnected;
            Client.Disconnected -= OnDisconnected;
            Client.ConnectionFailed -= OnConnectionFailed;
        }
    }
}
