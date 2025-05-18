using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Riptide;
using Riptide.Utils;
using Riptide.Transports;

namespace White_Knuckle_Multiplayer.Networking
{

    public class NetworkClient : MonoBehaviour
    {
        public static NetworkClient Instance { get; private set; }
        public Client Client { get; private set; }

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

        public void StartClient(string ip = "localhost", ushort port = 7777)
        {
            if (Client != null && Client.IsConnected)
            {
                LogManager.Client.Info("Client already connected");
                return;
            }

            Client = new Client();
            Client.Connected += OnConnected;
            Client.Disconnected += OnDisconnected;
            Client.ConnectionFailed += OnConnectionFailed;
            Client.Connect($"{ip}:{port}", maxConnectionAttempts: 5, messageHandlerGroupId: (byte)GroupID.Client, message: null, useMessageHandlers: true);
        }


        private void OnConnected(object sender, EventArgs e)
        {
            LogManager.Client.Info("Client Connected");
            string username = "Player_" + Client.Id;
            string version = "0.0.1";
            MessageSender.SendJoinRequest(new JoinRequestData(username, version));
        }

        private void OnConnectionFailed(object sender, EventArgs e)
        {
            LogManager.Client.Error("Client Failed to Connect");
        }

        private void OnDisconnected(object sender, EventArgs e)
        {
            LogManager.Client.Info("Client Disconnected");
        }
    }
}
