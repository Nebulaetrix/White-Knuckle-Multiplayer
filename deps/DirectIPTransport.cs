using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using Unity.Netcode;
using UnityEngine;

namespace White_Knuckle_Multiplayer.deps
{
    [DisallowMultipleComponent]
    public class DirectIPTransport : NetworkTransport
    {
        // For direct IP connections (local mode)
        public string directIPAddress = "";
        public ushort directIPPort = 7777;
        
        // Connection status
        public enum DirectConnectionStatus
        {
            Disconnected,
            Connecting,
            Connected
        }
        public DirectConnectionStatus directStatus = DirectConnectionStatus.Disconnected;
        
        // Socket-based transport for direct IP connections
        private System.Net.Sockets.Socket serverSocket;
        private System.Net.Sockets.Socket clientSocket;
        private bool isServer = false;
        private bool isClient = false;
        private const int bufferSize = 8192;
        private byte[] receiveBuffer;

        private LogLevel LogLevel => NetworkManager.Singleton?.LogLevel ?? LogLevel.Normal;

        private void Awake()
        {
            receiveBuffer = new byte[bufferSize];
            Debug.Log("DirectIPTransport initialized");
        }

        private void Update()
        {
            // Handle direct IP connections
            if (isServer)
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
            Shutdown();
        }

        public override ulong ServerClientId => 0;

        public override void Initialize(NetworkManager networkManager = null)
        {
            // Nothing specific for initialization
        }

        public override bool StartClient()
        {
            Shutdown();
            
            try
            {
                // Direct IP connection (local mode)
                Debug.Log($"Starting client with direct IP connection to {directIPAddress}:{directIPPort}");
                
                clientSocket = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                clientSocket.BeginConnect(directIPAddress, directIPPort, OnClientConnect, null);
                
                isClient = true;
                directStatus = DirectConnectionStatus.Connecting;
                return true;
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
                // Direct IP server (local mode)
                Debug.Log($"Starting server with direct IP on port {directIPPort}");
                
                serverSocket = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                serverSocket.Bind(new IPEndPoint(IPAddress.Any, directIPPort));
                serverSocket.Listen(10);
                
                isServer = true;
                directStatus = DirectConnectionStatus.Connected;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to start server: {ex.Message}");
                return false;
            }
        }

        public override void Shutdown()
        {
            try
            {
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
                
                isServer = false;
                isClient = false;
                directStatus = DirectConnectionStatus.Disconnected;
                Debug.Log("DirectIPTransport shutdown complete");
            }
            catch (Exception e)
            {
                Debug.LogError($"Caught an exception while shutting down: {e}");
            }
        }

        public override void DisconnectLocalClient()
        {
            if (isClient && clientSocket != null)
            {
                clientSocket.Close();
                clientSocket = null;
                isClient = false;
                directStatus = DirectConnectionStatus.Disconnected;
                Debug.Log("Disconnected local client.");
            }
        }

        public override void DisconnectRemoteClient(ulong clientId)
        {
            // In the original FacepunchTransport, this was not fully implemented
            // for direct IP mode with multiple clients
            Debug.Log($"DisconnectRemoteClient called for client {clientId}");
        }

        public override unsafe ulong GetCurrentRtt(ulong clientId)
        {
            return 50; // Default value from original code
        }

        public override void Send(ulong clientId, ArraySegment<byte> data, NetworkDelivery delivery)
        {
            try
            {
                // Send data directly over TCP
                if (isServer)
                {
                    // Send to specific client
                    // This needs a proper client socket tracking system
                    Debug.Log($"Server sending data to client {clientId}");
                }
                else if (isClient && clientSocket != null && clientSocket.Connected)
                {
                    // Send to server
                    clientSocket.Send(data.Array, data.Offset, data.Count, SocketFlags.None);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error sending data: {ex.Message}");
            }
        }

        public override NetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload, out float receiveTime)
        {
            clientId = 0;
            payload = default;
            receiveTime = Time.realtimeSinceStartup;
            return NetworkEvent.Nothing;
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
    }
} 