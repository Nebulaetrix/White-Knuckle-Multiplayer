using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Riptide;
using Riptide.Utils;

namespace White_Knuckle_Multiplayer.Networking
{

    public class NetworkServer : MonoBehaviour
    {
        public static NetworkServer Instance { get; private set; }
        public Server Server { get; private set; }
        public bool isActive => Server != null && Server.IsRunning;



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
            RiptideLogger.Initialize(LogManager.Server.Debug, LogManager.Server.Info, LogManager.Server.Warn, LogManager.Server.Error, false);
        }

        private void FixedUpdate()
        {
            if (Server != null)
            {
                Server.Update();
            }
        }

        public void StartServer(ushort port = 7777, ushort maxClientCount = 10)
        {
            if (Server != null) {
                LogManager.Server.Warn("Server is already running");
                return;
            }

            Server = new Server();
            Server.ClientConnected += OnClientConnected;
            Server.ClientDisconnected += OnClientDisconnected;
            Server.Start(port, maxClientCount, messageHandlerGroupId: (byte)GroupID.Server, useMessageHandlers: true);

            LogManager.Server.Info($"Server Started on port {port}");
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
    }
}
