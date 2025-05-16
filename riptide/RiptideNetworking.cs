using System;
using System.Collections.Generic;
using UnityEngine;
using Riptide;

namespace White_Knuckle_Multiplayer.Networking
{
    
    public class RiptideNetworking : MonoBehaviour
    {
        [SerializeField] private ushort port = 7777;
        [SerializeField] private string clientConnectAddress = "localhost";
        [SerializeField] private ushort maxConnections = 10;
        
        // Riptide components
        private Server server;
        private Client client;
        
        // Events
        public event Action<string> OnClientConnected;
        public event Action<string> OnClientDisconnected;
        public event Action<string> OnServerStarted;
        public event Action<string> OnServerStopped;
        
        // State properties
        public GameObject PlayerPrefab { get; set; }
        public bool IsServer => server != null && server.IsRunning;
        public bool IsClient => client != null && client.IsConnected;
        public bool IsConnected => client != null && client.IsConnected;
        public string ClientConnectAddress 
        { 
            get => clientConnectAddress;
            set => clientConnectAddress = value;
        }
        
        // Track connected clients and spawned players
        private readonly Dictionary<ushort, Connection> connectedPlayers = new Dictionary<ushort, Connection>();
        private readonly Dictionary<ushort, GameObject> spawnedPlayers = new Dictionary<ushort, GameObject>();
        
        // Timer for sending host position updates
        private float hostPositionUpdateTimer = 0f;
        private float hostPositionUpdateInterval = 0.05f; // 20 updates per second
        private Vector3 lastHostPosition = Vector3.zero;
        private Quaternion lastHostRotation = Quaternion.identity;
        
        // Message IDs
        public enum MessageId : ushort
        {
            SpawnPlayer = 1,
            PlayerMovement = 2,
            // use this later
            PlayerAction = 3
        }
        
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            transform.parent = null;
            InitializeComponents();
        }
        
        private void InitializeComponents()
        {
            // Initialize server
            server = new Server();
            server.ClientConnected += ServerClientConnected;
            server.ClientDisconnected += ServerClientDisconnected;
            server.MessageReceived += HandleServerMessage;
            
            // Initialize client
            client = new Client();
            client.Connected += ClientConnected;
            client.Disconnected += ClientDisconnected;
            client.MessageReceived += HandleClientMessage;
        }
        
        private void HandleServerMessage(object sender, Riptide.MessageReceivedEventArgs args)
        {
            switch ((MessageId)args.MessageId)
            {
                case MessageId.SpawnPlayer:
                    HandleSpawnRequestMessage(args.Message);
                    break;
                
                case MessageId.PlayerMovement:
                    try {
                        ushort movementClientId = args.Message.GetUShort();
                        if (movementClientId != 0) {
                            HandlePlayerMovementMessage(args.Message, movementClientId);
                        }
                    }
                    catch { }
                    break;
                // when needed, we will use this
                case MessageId.PlayerAction:
                    try {
                        ushort actionClientId = args.Message.GetUShort();
                        if (actionClientId != 0) {
                            HandlePlayerActionMessage(args.Message, actionClientId);
                        }
                    }
                    catch { }
                    break;
            }
        }
        
        // Handle player movement
        private void HandlePlayerMovementMessage(Riptide.Message message, ushort fromClientId)
        {
            // Extract position and rotation
            Vector3 position = GetVector3(message);
            Quaternion rotation = GetQuaternion(message);
            
            
            if (spawnedPlayers.TryGetValue(fromClientId, out GameObject playerObject))
            {
                NetworkPlayer networkPlayer = playerObject.GetComponent<NetworkPlayer>();
                if (networkPlayer != null)
                {
                    networkPlayer.UpdatePosition(position, rotation);
                }
                else
                {
                    playerObject.transform.position = position;
                    playerObject.transform.rotation = rotation;
                }
            }
            
            // Forward to clients
            Riptide.Message forwardMessage = Riptide.Message.Create(MessageSendMode.Unreliable, (ushort)MessageId.PlayerMovement);
            forwardMessage.Add(fromClientId);
            AddVector3(forwardMessage, position);
            AddQuaternion(forwardMessage, rotation);
            
            
            bool isHostPlayer = fromClientId == GetClientId() && IsServer;
            if (isHostPlayer)
            {
                server.SendToAll(forwardMessage);
            }
            else
            {
                server.SendToAll(forwardMessage, fromClientId);
            }
        }
        
        private void HandlePlayerActionMessage(Riptide.Message message, ushort fromClientId)
        {
            byte actionId = message.GetByte();
            
            // When Actions are implemented, then send them to the other people.
            Riptide.Message forwardMessage = Riptide.Message.Create(MessageSendMode.Reliable, (ushort)MessageId.PlayerAction);
            forwardMessage.Add(fromClientId);
            forwardMessage.Add(actionId);
            server.SendToAll(forwardMessage, fromClientId);
        }
        
        private void HandleClientMessage(object sender, MessageReceivedEventArgs e)
        {
            switch ((MessageId)e.MessageId)
            {
                case MessageId.SpawnPlayer:
                    HandlePlayerSpawnMessage(e.Message);
                    break;
                
                case MessageId.PlayerMovement:
                    HandleClientPlayerMovementMessage(e.Message);
                    break;
                
                case MessageId.PlayerAction:
                    try {
                        ushort clientId = e.Message.GetUShort();
                        if (clientId != 0) {
                            HandleClientPlayerActionMessage(e.Message, clientId);
                        }
                    }
                    catch {  }
                    break;
            }
        }
        
        private void HandleClientPlayerActionMessage(Riptide.Message message, ushort clientId)
        {
           // TODO: Implement actions like grabbing and such.
        }
        
    
        private void HandleClientPlayerMovementMessage(Riptide.Message message)
        {
            // Get player ID from message
            ushort playerId = message.GetUShort();
            if (playerId == 0) return;
            
            // Get position and rotation data
            Vector3 position = GetVector3(message);
            Quaternion rotation = GetQuaternion(message);
            
            // Dont update position of player if it is the local player
            bool isOwnPlayer = IsClient && client != null && client.Id == playerId;
            if (isOwnPlayer) return;
            
            
            if (spawnedPlayers.TryGetValue(playerId, out GameObject playerObject))
            {
                NetworkPlayer networkPlayer = playerObject.GetComponent<NetworkPlayer>();
                if (networkPlayer != null)
                {
                    networkPlayer.UpdatePosition(position, rotation);
                }
                else
                {
                    playerObject.transform.position = position;
                    playerObject.transform.rotation = rotation;
                }
            }
        }
        
        private void Update()
        {
          
            if (server != null && server.IsRunning)
            {
                server.Update();
                
                
                if (IsClient && IsServer)
                {
                    SendHostPositionUpdates();
                }
            }
            
            if (client != null)
            {
                client.Update();
            }
        }
        
        // Host should sent its position, WHY DOESN'T IT???
        private void SendHostPositionUpdates()
        {
            hostPositionUpdateTimer -= Time.deltaTime;
            
            if (hostPositionUpdateTimer <= 0)
            {
                hostPositionUpdateTimer = hostPositionUpdateInterval;
                
               
                GameObject clPlayer = GameObject.Find("CL_Player");
                if (clPlayer != null)
                {
                    Vector3 currentPos = clPlayer.transform.position;
                    Quaternion currentRot = clPlayer.transform.rotation;
                    
                    // Only send if position changed significantly
                    float distance = Vector3.Distance(currentPos, lastHostPosition);
                    if (distance > 0.1f || hostPositionUpdateTimer <= 0)
                    {
                        lastHostPosition = currentPos;
                        lastHostRotation = currentRot;
                        
                        // Host creates message with its own client ID
                        Riptide.Message message = Riptide.Message.Create(MessageSendMode.Unreliable, (ushort)MessageId.PlayerMovement);
                        message.Add(client.Id);
                        AddVector3(message, currentPos);
                        AddQuaternion(message, currentRot);
                        
                        // Send to all clients
                        server.SendToAll(message);
                    }
                }
            }
        }
        
        private void OnDestroy()
        {
            Disconnect();
        }
        
        // Server event handlers
        private void ServerClientConnected(object sender, ServerConnectedEventArgs args)
        {
            connectedPlayers[args.Client.Id] = args.Client;
            OnClientConnected?.Invoke($"Client {args.Client.Id} connected");
        }
        
        private void ServerClientDisconnected(object sender, ServerDisconnectedEventArgs e)
        {
            string connectionInfo = $"Client {e.Client.Id} disconnected";
            
            // Remove client from connections
            connectedPlayers.Remove(e.Client.Id);
            
            // Destroy player GameObject if it exists
            if (spawnedPlayers.TryGetValue(e.Client.Id, out GameObject playerObject))
            {
                Destroy(playerObject);
                spawnedPlayers.Remove(e.Client.Id);
            }
            
            // Send message to all clients to remove this player
            Riptide.Message disconnectMessage = Riptide.Message.Create(MessageSendMode.Reliable, (ushort)MessageId.PlayerAction);
            disconnectMessage.Add(e.Client.Id);
            disconnectMessage.Add((byte)0); 
            server.SendToAll(disconnectMessage);
            
            OnClientDisconnected?.Invoke(connectionInfo);
        }
        
        // Client event handlers
        private void ClientConnected(object sender, EventArgs e)
        {
            OnClientConnected?.Invoke($"Connected to server at {clientConnectAddress}:{port}");
            
            // Try to find CL_Player in scene
            GameObject clPlayer = GameObject.Find("CL_Player");
            Vector3 startPos = Vector3.zero;
            Quaternion startRot = Quaternion.identity;
            
            if (clPlayer != null)
            {
                startPos = clPlayer.transform.position;
                startRot = clPlayer.transform.rotation;
            }
            
            // Send a request for player spawning with the actual player position
            Riptide.Message spawnRequestMessage = Riptide.Message.Create(MessageSendMode.Reliable, (ushort)MessageId.SpawnPlayer);
            spawnRequestMessage.Add(client.Id);
            AddVector3(spawnRequestMessage, startPos);
            AddQuaternion(spawnRequestMessage, startRot);
            client.Send(spawnRequestMessage);
        }
        
        private void ClientDisconnected(object sender, DisconnectedEventArgs e)
        {
            // Clean up any spawned remote players
            foreach (var playerObject in spawnedPlayers.Values)
            {
                Destroy(playerObject);
            }
            spawnedPlayers.Clear();
            
            OnClientDisconnected?.Invoke($"Disconnected from server: {e.Reason}");
        }
        
        // Start server
        public void StartServer()
        {
            if (IsServer) return;
            
            try
            {
                server.Start(port, maxConnections);
                OnServerStarted?.Invoke($"Server started on port {port}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to start server: {ex.Message}");
            }
        }
        
        // Connect client
        public void ConnectClient()
        {
            if (IsClient && IsConnected) return;
            
            try
            {
                client.Connect($"{clientConnectAddress}:{port}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to connect client: {ex.Message}");
            }
        }
        
        // Start host (server + client)
        public void StartHost()
        {
            if (IsServer || IsClient) return;
            
            try
            {
                StartServer();
                ClientConnectAddress = "localhost";
                ConnectClient();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to start host: {ex.Message}");
                Disconnect();
            }
        }
        
        // Get current client ID
        public ushort GetClientId()
        {
            if (IsClient && client != null)
                return client.Id;
            return 0;
        }
        
        // Disconnect client and stop server
        public void Disconnect()
        {
            if (IsClient)
            {
                // Clean up before disconnecting
                foreach (var playerObject in spawnedPlayers.Values)
                {
                    Destroy(playerObject);
                }
                spawnedPlayers.Clear();
                
                client.Disconnect();
            }
            
            if (IsServer)
            {
                // Notify all clients to clean up before disconnecting
                Riptide.Message disconnectAllMessage = Riptide.Message.Create(MessageSendMode.Reliable, (ushort)MessageId.PlayerAction);
                disconnectAllMessage.Add((ushort)0);
                disconnectAllMessage.Add((byte)0); // 0 = Disconnect action
                server.SendToAll(disconnectAllMessage);
                
                // Clean up server-side players
                foreach (var playerObject in spawnedPlayers.Values)
                {
                    Destroy(playerObject);
                }
                spawnedPlayers.Clear();
                
                server.Stop();
                OnServerStopped?.Invoke("Server stopped");
                connectedPlayers.Clear();
            }
        }
        
        // Set the player prefab
        public void SetPlayerPrefab(GameObject prefab)
        {
            PlayerPrefab = prefab;
            if (prefab != null && prefab.GetComponent<NetworkPlayer>() == null)
            {
                prefab.AddComponent<NetworkPlayer>();
            }
        }
        
        // Helper methods for serializing Vector3 and Quaternion
        public static void AddVector3(Riptide.Message message, Vector3 value)
        {
            message.Add(value.x);
            message.Add(value.y);
            message.Add(value.z);
        }

        public static void AddQuaternion(Riptide.Message message, Quaternion value)
        {
            message.Add(value.x);
            message.Add(value.y);
            message.Add(value.z);
            message.Add(value.w);
        }
        
        public static Vector3 GetVector3(Riptide.Message message)
        {
            return new Vector3(message.GetFloat(), message.GetFloat(), message.GetFloat());
        }
        
        public static Quaternion GetQuaternion(Riptide.Message message)
        {
            return new Quaternion(message.GetFloat(), message.GetFloat(), message.GetFloat(), message.GetFloat());
        }
        
        // Spawn a remote player on the client
        private GameObject SpawnRemotePlayer(ushort clientId, Vector3 position, Quaternion rotation)
        {
            if (PlayerPrefab == null || clientId == 0) return null;
            
            // Check if we've already spawned this player
            if (spawnedPlayers.ContainsKey(clientId)) return spawnedPlayers[clientId];
            
            bool isLocalPlayer = IsClient && client.Id == clientId;
            string playerName = isLocalPlayer ? "LocalPlayer" : $"RemotePlayer_{clientId}";
            
            // Create the player object
            GameObject playerObj = GameObject.Instantiate(PlayerPrefab, position, rotation);
            playerObj.name = playerName;
            playerObj.SetActive(true);
            
            // Store the player in our lookup table
            spawnedPlayers[clientId] = playerObj;
            
            // Add NetworkPlayer component if not already present
            NetworkPlayer networkPlayer = playerObj.GetComponent<NetworkPlayer>();
            if (networkPlayer == null)
            {
                networkPlayer = playerObj.AddComponent<NetworkPlayer>();
            }
            
            // Initialize the network player
            networkPlayer.Initialize(clientId);
            
            return playerObj;
        }
        
        // Send a message from client to server
        public void SendToServer(Riptide.Message message)
        {
            if (IsClient)
            {
                client.Send(message);
            }
        }
        
        // Send a message from server to all clients
        public void SendToAll(Riptide.Message message)
        {
            if (IsServer)
            {
                server.SendToAll(message);
            }
        }
        
        // Send a message from server to specific client
        public void SendToClient(ushort clientId, Riptide.Message message)
        {
            if (IsServer && connectedPlayers.TryGetValue(clientId, out Connection client))
            {
                server.Send(message, client.Id, false);
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
            
            foreach (var clientPair in connectedPlayers)
            {
                Debug.Log($"Client: {clientPair.Key}");
            }
        }
        
        // Manually spawn a player for debugging
        public void SimulatePlayerJoin(string playerName)
        {
            if (!IsServer)
            {
                Debug.LogWarning("Cannot spawn player - not running as server");
                return;
            }
            
            // Find a local player to spawn for if one exists
            Connection localPlayer = null;
            foreach (var client in connectedPlayers.Values)
            {
                if (client.Id == 1) // Assuming client ID 1 is local
                {
                    localPlayer = client;
                    break;
                }
            }
            
            if (localPlayer != null)
            {
                SpawnRemotePlayer(localPlayer.Id, Vector3.zero, Quaternion.identity);
                Debug.Log($"Manually spawned player with name {playerName}");
            }
            else
            {
                Debug.LogWarning("No local connection available for spawning");
            }
        }

        private void HandleSpawnRequestMessage(Riptide.Message message)
        {
            // Read the client ID from the message
            ushort clientId = message.GetUShort();
            if (clientId == 0) return;
            
            // Extract position and rotation from message
            Vector3 spawnPosition = GetVector3(message);
            Quaternion spawnRotation = GetQuaternion(message);
            
            // Create spawn message for the new player using their requested position
            Riptide.Message spawnMessage = Riptide.Message.Create(MessageSendMode.Reliable, (ushort)MessageId.SpawnPlayer);
            spawnMessage.Add(clientId);
            AddVector3(spawnMessage, spawnPosition);
            AddQuaternion(spawnMessage, spawnRotation);
            
            // Send to all clients including the new one
            server.SendToAll(spawnMessage);
            
            // Also spawn this player locally on the server
            if (!spawnedPlayers.ContainsKey(clientId))
            {
                SpawnRemotePlayer(clientId, spawnPosition, spawnRotation);
            }
            
            // Send spawn messages for all existing players to the new client
            foreach (var pair in spawnedPlayers)
            {
                // Skip sending spawn message for the client's own player
                if (pair.Key == clientId) continue;
                
                // Create spawn message for existing player with their current position
                Riptide.Message existingPlayerMessage = Riptide.Message.Create(MessageSendMode.Reliable, (ushort)MessageId.SpawnPlayer);
                existingPlayerMessage.Add(pair.Key);
                AddVector3(existingPlayerMessage, pair.Value.transform.position);
                AddQuaternion(existingPlayerMessage, pair.Value.transform.rotation);
                
                // Send only to the new client
                server.Send(existingPlayerMessage, clientId);
            }
        }
        
        private void HandlePlayerSpawnMessage(Riptide.Message message)
        {
            // Extract client ID from message
            ushort playerId = message.GetUShort();
            if (playerId == 0) return;
            
            // Extract position and rotation
            Vector3 position = GetVector3(message);
            Quaternion rotation = GetQuaternion(message);
            
            // Check if this player is already spawned, otherwise spawn them
            if (!spawnedPlayers.ContainsKey(playerId))
            {
                SpawnRemotePlayer(playerId, position, rotation);
            }
            else if (spawnedPlayers.TryGetValue(playerId, out GameObject existingPlayer))
            {
                existingPlayer.transform.position = position;
                existingPlayer.transform.rotation = rotation;
            }
        }
    }
} 