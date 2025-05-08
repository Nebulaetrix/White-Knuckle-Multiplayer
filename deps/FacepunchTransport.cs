using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using Steamworks;
using Steamworks.Data;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;

namespace White_Knuckle_Multiplayer.deps
{
    using SocketConnection = Connection;

    [DisallowMultipleComponent]
    public class FacepunchTransport : NetworkTransport, IConnectionManager, ISocketManager
    {
        private ConnectionManager connectionManager;
        private SocketManager socketManager;
        private Dictionary<ulong, Client> connectedClients;

        // Define the connection status enum
        public enum DirectConnectionStatus
        {
            Disconnected,
            Connecting,
            Connected
        }

        [Space]
        [Tooltip("The Steam App ID of your game. Technically you're not allowed to use 480, but Valve doesn't do anything about it so it's fine for testing purposes.")]
        [SerializeField] private uint steamAppId = 3195790;

        [Tooltip("The Steam ID of the user targeted when joining as a client.")]
        [SerializeField] public ulong targetSteamId;

        [Header("Info")]
        [ReadOnly]
        [Tooltip("When in play mode, this will display your Steam ID.")]
        [SerializeField] private ulong userSteamId;

        public DirectConnectionStatus directStatus = DirectConnectionStatus.Disconnected;
        
        // For direct IP connections (local mode)
        public string directIPAddress = "";
        public ushort directIPPort = 7777;
        public bool useDirectIP = false;
        
        // Socket-based transport for direct IP connections
        private System.Net.Sockets.Socket serverSocket;
        private System.Net.Sockets.Socket clientSocket;
        private bool isServer = false;
        private bool isClient = false;
        private const int bufferSize = 8192;
        private byte[] receiveBuffer;

        private LogLevel LogLevel => NetworkManager.Singleton.LogLevel;

        private class Client
        {
            public SteamId steamId;
            public SocketConnection connection;
        }

        #region MonoBehaviour Messages

        private void Awake()
        {
            if (SteamClient.IsValid) return;
            try
            {
                SteamClient.Init(steamAppId, false);
            }
            catch (Exception e)
            {
                if (LogLevel <= LogLevel.Error)
                    Debug.LogError($"[{nameof(FacepunchTransport)}] - Caught an exeption during initialization of Steam client: {e}");
            }
            finally
            {
                StartCoroutine(InitSteamworks());
            }

            receiveBuffer = new byte[bufferSize];
            
            try
            {
                if (SteamClient.IsValid)
                {
                    Debug.Log("Steam is initialized and valid");
                }
                else
                {
                    Debug.LogWarning("Steam is not initialized. Direct IP mode will be used for local connections.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error checking Steam state: {ex.Message}");
            }
        }

        private void Update()
        {
            SteamClient.RunCallbacks();

            // Handle direct IP connections if in use
            if (useDirectIP && isServer)
            {
                try
                {
                    // Check for new connections
                    if (serverSocket != null && serverSocket.Poll(0, SelectMode.SelectRead))
                    {
                        System.Net.Sockets.Socket clientSock = serverSocket.Accept();
                        HandleNewClientConnection(clientSock);
                    }
                    
                    // Poll for data on existing connections
                    // This would be enhanced for real-world use
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error in server update: {ex.Message}");
                }
            }
        }

        private void OnDestroy()
        {
            SteamClient.Shutdown();
            Shutdown();
        }

        #endregion

        #region NetworkTransport Overrides

        public override ulong ServerClientId => 0;

        public override void DisconnectLocalClient()
        {
            connectionManager?.Connection.Close();

            if (LogLevel <= LogLevel.Developer)
                Debug.Log($"[{nameof(FacepunchTransport)}] - Disconnecting local client.");
        }

        public override void DisconnectRemoteClient(ulong clientId)
        {
            if (connectedClients.TryGetValue(clientId, out Client user))
            {
                // Flush any pending messages before closing the connection
                user.connection.Flush();
                user.connection.Close();
                connectedClients.Remove(clientId);

                if (LogLevel <= LogLevel.Developer)
                    Debug.Log($"[{nameof(FacepunchTransport)}] - Disconnecting remote client with ID {clientId}.");
            }
            else if (LogLevel <= LogLevel.Normal)
                Debug.LogWarning($"[{nameof(FacepunchTransport)}] - Failed to disconnect remote client with ID {clientId}, client not connected.");
        }

        public override unsafe ulong GetCurrentRtt(ulong clientId)
        {
            return 0;
        }

        public override void Initialize(NetworkManager networkManager = null)
        {
            connectedClients = new Dictionary<ulong, Client>();
        }

        private SendType NetworkDeliveryToSendType(NetworkDelivery delivery)
        {
            return delivery switch
            {
                NetworkDelivery.Reliable => SendType.Reliable,
                NetworkDelivery.ReliableFragmentedSequenced => SendType.Reliable,
                NetworkDelivery.ReliableSequenced => SendType.Reliable,
                NetworkDelivery.Unreliable => SendType.Unreliable,
                NetworkDelivery.UnreliableSequenced => SendType.Unreliable,
                _ => SendType.Reliable
            };
        }

        public override void Shutdown()
        {
            try
            {
                if (LogLevel <= LogLevel.Developer)
                    Debug.Log($"[{nameof(FacepunchTransport)}] - Shutting down.");

                connectionManager?.Close();
                socketManager?.Close();

                // Close direct IP connections
                if (serverSocket != null)
                {
                    serverSocket.Close();
                    serverSocket = null;
                }
                
                if (clientSocket != null)
                {
                    clientSocket.Close();
                    clientSocket = null;
                }
                
                // Clean up Steam connections
                if (SteamClient.IsValid)
                {
                    // For the decompiled FacepunchTransport, the actual Steam SDK calls would be here
                    // This is where you'd clean up any Steam networking connections
                }
                
                isServer = false;
                isClient = false;
                directStatus = DirectConnectionStatus.Disconnected;
                Debug.Log("Transport shutdown complete");
            }
            catch (Exception e)
            {
                if (LogLevel <= LogLevel.Error)
                    Debug.LogError($"[{nameof(FacepunchTransport)}] - Caught an exception while shutting down: {e}");
            }
        }

        public override void Send(ulong clientId, ArraySegment<byte> data, NetworkDelivery delivery)
        {
            try
            {
                if (useDirectIP)
                {
                    // Send data directly over TCP
                    if (isServer)
                    {
                        // Send to specific client
                        // This needs a proper client socket tracking system
                    }
                    else if (isClient && clientSocket != null && clientSocket.Connected)
                    {
                        // Send to server
                        clientSocket.Send(data.Array, data.Offset, data.Count, SocketFlags.None);
                    }
                }
                else if (SteamClient.IsValid)
                {
                    // For the decompiled FacepunchTransport, the actual Steam SDK calls would be here
                    // This is where you'd send data via Steam networking
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error sending data: {ex.Message}");
            }
        }

        public override Unity.Netcode.NetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload, out float receiveTime)
        {
            connectionManager?.Receive();
            socketManager?.Receive();

            clientId = 0;
            receiveTime = Time.realtimeSinceStartup;
            payload = default;
            return Unity.Netcode.NetworkEvent.Nothing;
        }

        public override bool StartClient()
        {
            Shutdown();
            
            try
            {
                if (useDirectIP)
                {
                    // Direct IP connection (local mode)
                    Debug.Log($"Starting client with direct IP connection to {directIPAddress}:{directIPPort}");
                    
                    clientSocket = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    clientSocket.BeginConnect(directIPAddress, directIPPort, OnClientConnect, null);
                    
                    isClient = true;
                    directStatus = DirectConnectionStatus.Connecting;
                    return true;
                }
                else if (SteamClient.IsValid)
                {
                    // Steam connection
                    Debug.Log($"Starting client with Steam connection to {targetSteamId}");
                    connectionManager = SteamNetworkingSockets.ConnectRelay<ConnectionManager>(targetSteamId);
                    connectionManager.Interface = this;
                    
                    directStatus = DirectConnectionStatus.Connecting;
                    return true;
                }
                else
                {
                    Debug.LogError("Cannot start client - Steam is not initialized and direct IP mode is not enabled");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to start client: {ex.Message}");
                return false;
            }
        }

        public override bool StartServer()
        {
            Shutdown();
            
            try
            {
                if (useDirectIP)
                {
                    // Direct IP server (local mode)
                    Debug.Log($"Starting server with direct IP on port {directIPPort}");
                    
                    serverSocket = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    serverSocket.Bind(new IPEndPoint(IPAddress.Any, directIPPort));
                    serverSocket.Listen(10);
                    
                    isServer = true;
                    directStatus = DirectConnectionStatus.Connected;
                    return true;
                }
                else if (SteamClient.IsValid)
                {
                    // Steam server
                    Debug.Log("Starting server with Steam connection");
                    socketManager = SteamNetworkingSockets.CreateRelaySocket<SocketManager>();
                    socketManager.Interface = this;
                    
                    directStatus = DirectConnectionStatus.Connected;
                    return true;
                }
                else
                {
                    Debug.LogError("Cannot start server - Steam is not initialized and direct IP mode is not enabled");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to start server: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region ConnectionManager Implementation

        private byte[] payloadCache = new byte[4096];

        private void EnsurePayloadCapacity(int size)
        {
            if (payloadCache.Length >= size)
                return;

            payloadCache = new byte[Math.Max(payloadCache.Length * 2, size)];
        }

        void IConnectionManager.OnConnecting(ConnectionInfo info)
        {
            if (LogLevel <= LogLevel.Developer)
                Debug.Log($"[{nameof(FacepunchTransport)}] - Connecting with Steam user {info.Identity.SteamId}.");
        }

        void IConnectionManager.OnConnected(ConnectionInfo info)
        {
            InvokeOnTransportEvent(Unity.Netcode.NetworkEvent.Connect, ServerClientId, default, Time.realtimeSinceStartup);

            if (LogLevel <= LogLevel.Developer)
                Debug.Log($"[{nameof(FacepunchTransport)}] - Connected with Steam user {info.Identity.SteamId}.");
        }

        void IConnectionManager.OnDisconnected(ConnectionInfo info)
        {
            InvokeOnTransportEvent(Unity.Netcode.NetworkEvent.Disconnect, ServerClientId, default, Time.realtimeSinceStartup);

            if (LogLevel <= LogLevel.Developer)
                Debug.Log($"[{nameof(FacepunchTransport)}] - Disconnected Steam user {info.Identity.SteamId}.");
        }

        unsafe void IConnectionManager.OnMessage(IntPtr data, int size, long messageNum, long recvTime, int channel)
        {
            EnsurePayloadCapacity(size);

            fixed (byte* payload = payloadCache)
            {
                UnsafeUtility.MemCpy(payload, (byte*)data, size);
            }

            InvokeOnTransportEvent(Unity.Netcode.NetworkEvent.Data, ServerClientId, new ArraySegment<byte>(payloadCache, 0, size), Time.realtimeSinceStartup);
        }

        #endregion

        #region SocketManager Implementation

        void ISocketManager.OnConnecting(SocketConnection connection, ConnectionInfo info)
        {
            if (LogLevel <= LogLevel.Developer)
                Debug.Log($"[{nameof(FacepunchTransport)}] - Accepting connection from Steam user {info.Identity.SteamId}.");

            connection.Accept();
        }

        void ISocketManager.OnConnected(SocketConnection connection, ConnectionInfo info)
        {
            if (!connectedClients.ContainsKey(connection.Id))
            {
                connectedClients.Add(connection.Id, new Client()
                {
                    connection = connection,
                    steamId = info.Identity.SteamId
                });
                
                Debug.Log($"Connection ID: {connection.Id}, Steam ID: {info.Identity.SteamId}");

                InvokeOnTransportEvent(Unity.Netcode.NetworkEvent.Connect, connection.Id, default, Time.realtimeSinceStartup);

                if (LogLevel <= LogLevel.Developer)
                    Debug.Log($"[{nameof(FacepunchTransport)}] - Connected with Steam user {info.Identity.SteamId}.");
            }
            else if (LogLevel <= LogLevel.Normal)
                Debug.LogWarning($"[{nameof(FacepunchTransport)}] - Failed to connect client with ID {connection.Id}, client already connected.");
        }

        void ISocketManager.OnDisconnected(SocketConnection connection, ConnectionInfo info)
        {
            if (connectedClients.Remove(connection.Id))
	        {
	            InvokeOnTransportEvent(Unity.Netcode.NetworkEvent.Disconnect, connection.Id, default, Time.realtimeSinceStartup);

	            if (LogLevel <= LogLevel.Developer)
                    Debug.Log($"[{nameof(FacepunchTransport)}] - Disconnected Steam user {info.Identity.SteamId}");
	        }
     	    else if (LogLevel <= LogLevel.Normal)
                Debug.LogWarning($"[{nameof(FacepunchTransport)}] - Failed to diconnect client with ID {connection.Id}, client not connected.");
        }

        unsafe void ISocketManager.OnMessage(SocketConnection connection, NetIdentity identity, IntPtr data, int size, long messageNum, long recvTime, int channel)
        {
            EnsurePayloadCapacity(size);

            fixed (byte* payload = payloadCache)
            {
                UnsafeUtility.MemCpy(payload, (byte*)data, size);
            }

            InvokeOnTransportEvent(Unity.Netcode.NetworkEvent.Data, connection.Id, new ArraySegment<byte>(payloadCache, 0, size), Time.realtimeSinceStartup);
        }

        #endregion

        #region Utility Methods

        private IEnumerator InitSteamworks()
        {
            yield return new WaitUntil(() => SteamClient.IsValid);

            SteamNetworkingUtils.InitRelayNetworkAccess();

            if (LogLevel <= LogLevel.Developer)
                Debug.Log($"[{nameof(FacepunchTransport)}] - Initialized access to Steam Relay Network.");

            userSteamId = SteamClient.SteamId;

            if (LogLevel <= LogLevel.Developer)
                Debug.Log($"[{nameof(FacepunchTransport)}] - Fetched user Steam ID.");
        }
        
        #endregion

        #region Socket Callbacks
        
        private void OnClientConnect(IAsyncResult ar)
        {
            try
            {
                if (clientSocket == null)
                    return;
                
                clientSocket.EndConnect(ar);
                
                if (clientSocket.Connected)
                {
                    Debug.Log("Direct IP client connected successfully");
                    directStatus = DirectConnectionStatus.Connected;
                    
                    // Notify the transport layer about the connection
                    InvokeOnTransportEvent(Unity.Netcode.NetworkEvent.Connect, ServerClientId, default, Time.realtimeSinceStartup);
                    
                    // Start receiving data
                    clientSocket.BeginReceive(receiveBuffer, 0, bufferSize, SocketFlags.None, OnClientDataReceived, null);
                }
                else
                {
                    Debug.LogError("Failed to connect via direct IP");
                    directStatus = DirectConnectionStatus.Disconnected;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in client connect callback: {ex.Message}");
                directStatus = DirectConnectionStatus.Disconnected;
            }
        }
        
        private void OnClientDataReceived(IAsyncResult ar)
        {
            try
            {
                if (clientSocket == null || !clientSocket.Connected)
                    return;
                
                int bytesRead = clientSocket.EndReceive(ar);
                
                if (bytesRead > 0)
                {
                    // Create a copy of the received data
                    byte[] dataBuffer = new byte[bytesRead];
                    Buffer.BlockCopy(receiveBuffer, 0, dataBuffer, 0, bytesRead);
                    
                    // Process the received data
                    ArraySegment<byte> segment = new ArraySegment<byte>(dataBuffer, 0, bytesRead);
                    InvokeOnTransportEvent(Unity.Netcode.NetworkEvent.Data, ServerClientId, segment, Time.realtimeSinceStartup);
                    
                    // Continue receiving
                    clientSocket.BeginReceive(receiveBuffer, 0, bufferSize, SocketFlags.None, OnClientDataReceived, null);
                }
                else
                {
                    // Connection closed
                    Debug.Log("Server disconnected");
                    InvokeOnTransportEvent(Unity.Netcode.NetworkEvent.Disconnect, ServerClientId, default, Time.realtimeSinceStartup);
                    clientSocket.Close();
                    clientSocket = null;
                    directStatus = DirectConnectionStatus.Disconnected;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error receiving data: {ex.Message}");
                InvokeOnTransportEvent(Unity.Netcode.NetworkEvent.Disconnect, ServerClientId, default, Time.realtimeSinceStartup);
                directStatus = DirectConnectionStatus.Disconnected;
            }
        }
        
        private void HandleNewClientConnection(System.Net.Sockets.Socket clientSock)
        {
            // In a real implementation, you'd store the client socket and assign it a client ID
            // For this example, we'll use a simple implementation with just one client
            
            try
            {
                Debug.Log("New client connected to server");
                
                // Generate a client ID (this needs to be more robust in a real implementation)
                ulong newClientId = (ulong)clientSock.RemoteEndPoint.GetHashCode() + 1;
                
                // Notify the transport layer about the new connection
                InvokeOnTransportEvent(Unity.Netcode.NetworkEvent.Connect, newClientId, default, Time.realtimeSinceStartup);
                
                // Start receiving data from this client
                // This is a simplified version - a real implementation would track clients and their receive states
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error handling new client: {ex.Message}");
            }
        }
        
        #endregion
    }
}