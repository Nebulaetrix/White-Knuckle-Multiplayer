using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Riptide;
using Riptide.Utils;
using White_Knuckle_Multiplayer.Networking.Routing;
using White_Knuckle_Multiplayer.Networking.Transports.Steam;

namespace White_Knuckle_Multiplayer.Networking
{

    public class NetworkServer : MonoBehaviour
    {
        public static NetworkServer Instance { get; private set; }
        public Server Server { get; private set; }
        public SteamServer SteamServer { get; private set; }
        public bool IsActive => Server != null && Server.IsRunning;



        private void Awake()
        {
            // Keeping only one Instance
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }    
            Instance = this;
            // SteamServer = new SteamServer();
            
            // Setting up Riptide Logger
            RiptideLogger.Initialize(LogManager.Server.Debug, LogManager.Server.Info, LogManager.Server.Warn, LogManager.Server.Error, false);
        }

        private void FixedUpdate()
        {
            if (Server != null && Server.IsRunning)
            {
                Server.Update();
            }
        }

        private void Start()
        {
            if (!SteamManager.initialized)
            {
                LogManager.Server.Error("Steam is not initialized");
                return;
            }
            
            SteamServer = new SteamServer();
            Server = new Server(SteamServer);
        }

        public void StartServer(ushort port = 7777, ushort maxClientCount = 10, string transport = "udp")
        {
            if (Server != null && Server.IsRunning) {
                LogManager.Server.Warn("Server is already running");
                return;
            }

            if (transport == "steam")
            {
                Server = new Server(SteamServer);
            }
            else
            {
                Server = new Server();
            }

            Server.MessageReceived += OnServerMessageReceived;
            Server.ClientConnected += OnClientConnected;
            Server.ClientDisconnected += OnClientDisconnected;
            Server.Start(port, maxClientCount, messageHandlerGroupId: (byte)GroupID.Server, useMessageHandlers: false);

            LogManager.Server.Info($"Server Started on port {port}");
        }

        public void StartSteamServer()
        {
            StartServer(7777, 10, "steam");
        }

        public void StopServer()
        {
            if (Server != null && Server.IsRunning) {
                Server.Stop();
                LogManager.Server.Info("Server stopped");
            }
        }

        private void OnApplicationQuit() => StopServer();

        private void OnClientConnected(object sender, ServerConnectedEventArgs e)
        {
            LogManager.Server.Info($"Client {e.Client.Id} connected");
        }

        private void OnClientDisconnected(object sender, ServerDisconnectedEventArgs e)
        {
            LogManager.Server.Info($"Client {e.Client.Id} disconnected");
            MessageSender.SendDespawn(e.Client.Id);
        }
        
        private void OnServerMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            var messageID = e.MessageId;
            var fromClientID = e.FromConnection.Id;
            var msg = e.Message;
            
            MessageRouter.Route(messageID, (byte)GroupID.Server, fromClientID, msg);
        }

        private void OnDisable()
        {
            Server.MessageReceived -= OnServerMessageReceived;
            Server.ClientConnected -= OnClientConnected;
            Server.ClientDisconnected -= OnClientDisconnected;
        }
    }
}
