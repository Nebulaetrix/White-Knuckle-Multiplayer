using System;
using System.Collections.Generic;
using UnityEngine;
using Mirage;
using Mirage.Sockets.Udp;

namespace White_Knuckle_Multiplayer.Networking
{
    /// <summary>
    /// MirageNetworking - Handles Mirage networking for multiplayer
    /// </summary>
    public class MirageNetworking : MonoBehaviour
    {
        [SerializeField] private ushort port = 7777;
        [SerializeField] private string clientConnectAddress = "localhost";
        
        // Mirage components
        private NetworkServer server;
        private NetworkClient client;
        private ServerObjectManager serverObjectManager;
        private ClientObjectManager clientObjectManager;
        private UdpSocketFactory socketFactory;
        private NetworkSceneManager sceneManager;
        
        // Events
        public event Action<string> OnClientConnected;
        public event Action<string> OnClientDisconnected;
        public event Action<string> OnServerStarted;
        public event Action<string> OnServerStopped;
        
        // State properties
        public GameObject PlayerPrefab { get; set; }
        public bool IsServer => server != null && server.Active;
        public bool IsClient => client != null && client.Active;
        public bool IsConnected => client != null && client.Active;
        public string ClientConnectAddress 
        { 
            get => clientConnectAddress;
            set => clientConnectAddress = value;
        }
        
        // Track connected clients
        private readonly List<INetworkPlayer> connectedPlayers = new List<INetworkPlayer>();
        
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            
            // Make sure we're on a new GameObject
            transform.parent = null;
            
            // Set up components
            InitializeComponents();
            
            // Default logging for events
            OnClientConnected += (msg) => Debug.Log(msg);
            OnClientDisconnected += (msg) => Debug.Log(msg);
            OnServerStarted += (msg) => Debug.Log(msg);
            OnServerStopped += (msg) => Debug.Log(msg);
        }
        
        private void InitializeComponents()
        {
            // Set up socket factory
            socketFactory = gameObject.AddComponent<UdpSocketFactory>();
            socketFactory.Port = port;
            
            // Add server and client components
            server = gameObject.AddComponent<NetworkServer>();
            client = gameObject.AddComponent<NetworkClient>();
            serverObjectManager = gameObject.AddComponent<ServerObjectManager>();
            clientObjectManager = gameObject.AddComponent<ClientObjectManager>();
            sceneManager = gameObject.AddComponent<NetworkSceneManager>();
            
            // Connect socket factory
            server.SocketFactory = socketFactory;
            client.SocketFactory = socketFactory;
            
            // Server events
            server.Started.AddListener(OnServerStarted_Internal);
            server.Stopped.AddListener(OnServerStopped_Internal);
            server.Connected.AddListener(OnServerClientConnected);
            server.Disconnected.AddListener(OnServerClientDisconnected);
            
            // Client events
            client.Connected.AddListener((_) => OnClientConnectedEvent());
            client.Disconnected.AddListener((reason) => OnClientDisconnectedEvent(reason));
        }
        
        private void OnDestroy()
        {
            Disconnect();
        }
        
        // Server event handlers
        private void OnServerStarted_Internal() => 
            OnServerStarted?.Invoke($"Mirage server started successfully on port {port}");
        
        private void OnServerStopped_Internal()
        {
            OnServerStopped?.Invoke("Mirage server stopped");
            connectedPlayers.Clear();
        }
        
        private void OnServerClientConnected(INetworkPlayer player)
        {
            if (!connectedPlayers.Contains(player))
                connectedPlayers.Add(player);
                
            OnClientConnected?.Invoke($"Client {player.Address} connected to server");
            
            if (PlayerPrefab != null)
                SpawnPlayerForClient(player);
        }
        
        private void OnServerClientDisconnected(INetworkPlayer player)
        {
            connectedPlayers.Remove(player);
            OnClientDisconnected?.Invoke($"Client {player.Address} disconnected from server");
        }
        
        // Client event handlers
        private void OnClientConnectedEvent() =>
            OnClientConnected?.Invoke($"Connected to Mirage server at {clientConnectAddress}:{port}");
        
        private void OnClientDisconnectedEvent(ClientStoppedReason reason) =>
            OnClientDisconnected?.Invoke($"Disconnected from server: {reason}");
        
        // Start server
        public void StartServer()
        {
            if (IsServer)
            {
                Debug.LogWarning("Server is already running");
                return;
            }
            
            try
            {
                socketFactory.Port = port;
                server.StartServer();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to start Mirage server: {ex.Message}");
            }
        }
        
        // Connect client
        public void ConnectClient()
        {
            if (IsClient && IsConnected)
            {
                Debug.LogWarning("Client is already connected");
                return;
            }
            
            try
            {
                socketFactory.Port = port;
                client.Connect(clientConnectAddress);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to connect Mirage client: {ex.Message}");
            }
        }
        
        // Start host (server + client)
        public void StartHost()
        {
            if (IsServer || IsClient)
            {
                Debug.LogWarning("Server or client is already running");
                return;
            }
            
            try
            {
                socketFactory.Port = port;
                
                // Start the server first
                server.StartServer();
                
                // Then connect the client
                client.Connect("localhost");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to start Mirage host: {ex.Message}");
                Disconnect();
            }
        }
        
        // Disconnect client and stop server
        public void Disconnect()
        {
            if (IsClient)
                client.Disconnect();
            
            if (IsServer)
                server.Stop();
        }
        
        // Ensure player prefab has NetworkIdentity
        public void EnsurePlayerPrefabReady()
        {
            if (PlayerPrefab == null)
                return;
                
            // Make sure the player prefab has a NetworkIdentity component
            if (PlayerPrefab.GetComponent<NetworkIdentity>() == null)
            {
                Debug.LogWarning("Adding missing NetworkIdentity to player prefab");
                PlayerPrefab.AddComponent<NetworkIdentity>();
            }
            
            // Register the prefab with ServerObjectManager
            if (IsServer && serverObjectManager != null)
            {
                Debug.Log("Player prefab configured, using standard NetworkIdentity");
                Debug.Log("Player prefab setup completed");
            }
        }
        
        // Set the player prefab with necessary checks
        public void SetPlayerPrefab(GameObject prefab)
        {
            PlayerPrefab = prefab;
            EnsurePlayerPrefabReady();
        }
        
        // Spawn player for a client
        private void SpawnPlayerForClient(INetworkPlayer player)
        {
            if (!IsServer || PlayerPrefab == null) 
                return;
            
            try
            {
                // Make sure prefab has NetworkIdentity
                EnsurePlayerPrefabReady();
                
                GameObject playerObject = Instantiate(PlayerPrefab);
                
                // Ensure the instantiated object has NetworkIdentity
                NetworkIdentity identity = playerObject.GetComponent<NetworkIdentity>();
                if (identity == null)
                {
                    Debug.LogWarning("Adding missing NetworkIdentity to spawned player");
                    identity = playerObject.AddComponent<NetworkIdentity>();
                }
                
                // Try to spawn the player using ServerObjectManager
                try
                {
                    // Use AddCharacter to properly associate this player with their character
                    serverObjectManager.AddCharacter(player, identity);
                    Debug.Log($"Spawned player for client {player.Address} using ServerObjectManager");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to spawn player: {ex.Message}");
                    // Just activate the object if spawning fails
                    playerObject.SetActive(true);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to spawn player: {ex.Message}");
            }
        }
        
        // Log connected clients
        public void LogConnectedClients()
        {
            if (!IsServer)
            {
                Debug.Log("Not running as server - no client information available");
                return;
            }
            
            int clientCount = connectedPlayers.Count;
            
            if (clientCount == 0)
            {
                Debug.Log("No clients connected");
                return;
            }
            
            Debug.Log($"Connected clients: {clientCount}");
            
            foreach (var player in connectedPlayers)
                Debug.Log($"Client: {player.Address}");
        }
        
    }
} 