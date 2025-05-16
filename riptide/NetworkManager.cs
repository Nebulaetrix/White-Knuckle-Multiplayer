using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BepInEx.Logging;
using White_Knuckle_Multiplayer.Networking;

namespace White_Knuckle_Multiplayer.Managers
{

    public class NetworkManager
    {
        private readonly ManualLogSource logger;
        private RiptideNetworking riptideNetworking;
        
        private const string NetworkManagerObjectName = "RiptideNetworkManager";
        private const string PlayerObjectName = "CL_Player";
        private const string PlayerPrefabName = "CL_Player_Network_Prefab";
        
        // Dictionary to track active players by their client IDs
        private readonly Dictionary<ushort, GameObject> activePlayers = new Dictionary<ushort, GameObject>();
        
        public NetworkManager(ManualLogSource logSource)
        {
            logger = logSource;
        }
        
        public RiptideNetworking GetRiptideNetworking() => riptideNetworking;
        
        public void InitializeNetworking()
        {
            // Check if a RiptideNetworking instance already exists
            riptideNetworking = UnityEngine.Object.FindObjectOfType<RiptideNetworking>();
            
            if (riptideNetworking == null)
            {
                // Create a new GameObject for the network manager
                var networkManagerGameObject = new GameObject(NetworkManagerObjectName);
                UnityEngine.Object.DontDestroyOnLoad(networkManagerGameObject);
                
                // Add the RiptideNetworking component
                riptideNetworking = networkManagerGameObject.AddComponent<RiptideNetworking>();
                
                // Set up event handlers
                riptideNetworking.OnClientConnected += OnClientConnected;
                riptideNetworking.OnClientDisconnected += OnClientDisconnected;
                riptideNetworking.OnServerStarted += OnServerStarted;
                riptideNetworking.OnServerStopped += OnServerStopped;
                
                logger.LogInfo("Initialized RiptideNetworking");
            }
            
            // Create the player prefab
            CreatePlayerPrefab();
        }
        
        private void CreatePlayerPrefab()
        {
            logger.LogDebug("Creating Player Prefab...");
            GameObject player = GameObject.Find(PlayerObjectName);
            if (player == null)
            {
                logger.LogError("Cannot create player prefab, no player found!");
                return;
            }

            GameObject playerPrefab = InstantiatePlayerPrefab(player);
            DestroyUnwantedComponents(playerPrefab);

            // Set the player prefab for the RiptideNetworking component
            riptideNetworking.SetPlayerPrefab(playerPrefab);
            logger.LogDebug("Player Prefab created and assigned to RiptideNetworking");
        }

        private GameObject InstantiatePlayerPrefab(GameObject player)
        {
            GameObject prefab = UnityEngine.Object.Instantiate(player);
            UnityEngine.Object.DontDestroyOnLoad(prefab);

            prefab.name = PlayerPrefabName;
            prefab.SetActive(false);

            // Add NetworkPlayer component for Riptide networking
            if (prefab.GetComponent<NetworkPlayer>() == null)
                prefab.AddComponent<NetworkPlayer>();

            return prefab;
        }

        private void DestroyUnwantedComponents(GameObject prefab)
        {
            // Remove physics components that would interfere with network replication
            if (prefab.GetComponent<CharacterController>() != null)
                UnityEngine.Object.Destroy(prefab.GetComponent<CharacterController>());
        
            var inventoryComponent = prefab.GetComponent("Inventory");
            if (inventoryComponent != null)
                UnityEngine.Object.Destroy(inventoryComponent);
                
            var unwantedCameraComponents = new[]
            {
                "Main Cam Root", "Main Cam Root/Main Camera Shake Root/Main Camera",
                "Main Cam Root/Main Camera Shake Root/Main Camera/Inventory Camera"
            };

            foreach (var path in unwantedCameraComponents)
            {
                var camObject = prefab.transform.Find(path);
                if (camObject != null)
                {
                    // Remove camera-related components that should only exist on local player
                    var cameraComponent = camObject.GetComponent<Camera>();
                    if (cameraComponent != null)
                        UnityEngine.Object.Destroy(cameraComponent);
                    
                    // Try to find and remove post-processing components by name
                    var postProcessComponents = camObject.GetComponents<MonoBehaviour>();
                    foreach (var component in postProcessComponents)
                    {
                        if (component.GetType().Name.Contains("PostProcess") ||
                            component.GetType().Name.Contains("CameraShader") ||
                            component.GetType().Name.Contains("CRTEffect"))
                        {
                            UnityEngine.Object.Destroy(component);
                        }
                    }
                }
            }

            var unwantedGameObjects = new[]
            {
                "Main Cam Root/Main Camera Shake Root/Main Camera/Inventory Camera/Inventory",
                "Main Cam Root/Main Camera Shake Root/Main Camera/Inventory Camera/InventoryBagCamera",
                "Main Cam Target", "Particle System", "Wind Sound", "Fatigue Sound",
                "CorruptionSurround", "Aim Circle", "Fake Handholds", "FXCam", "EffectRoot", "Death Sound"
            };

            foreach (var path in unwantedGameObjects)
            {
                var unwantedObject = prefab.transform.Find(path);
                if (unwantedObject != null) 
                    UnityEngine.Object.Destroy(unwantedObject.gameObject);
            }
        }
        
        // Event handlers
        private void OnClientConnected(string message)
        {
            logger.LogInfo(message);
        }
        
        private void OnClientDisconnected(string message)
        {
            logger.LogInfo(message);
        }
        
        private void OnServerStarted(string message)
        {
            logger.LogInfo(message);
        }
        
        private void OnServerStopped(string message)
        {
            logger.LogInfo(message);
        }
        
        // Network operations
        public void StartHost()
        {
            if (riptideNetworking == null)
            {
                logger.LogError("RiptideNetworking not initialized!");
                return;
            }
            
            logger.LogDebug("Starting Riptide Host...");
            
            try
            {
                // Output local IP information for connection
                LogLocalIPs();
                
                // Start host (server + client)
                riptideNetworking.StartHost();
                CommandConsole.Log("Riptide host started!");
                logger.LogInfo("Riptide host started!");
            }
            catch (Exception ex)
            {
                CommandConsole.LogError($"Unable to initialize Riptide host! Ex: {ex}");
                logger.LogError($"Unable to initialize Riptide host! Ex: {ex}");
            }
        }
        
        public void StartServer()
        {
            if (riptideNetworking == null)
            {
                logger.LogError("RiptideNetworking not initialized!");
                return;
            }
            
            logger.LogDebug("Starting Riptide Server...");
            
            try
            {
                // Output local IP information for connection
                LogLocalIPs();
                
                // Start only the server
                riptideNetworking.StartServer();
                CommandConsole.Log("Riptide server started!");
                logger.LogInfo("Riptide server started!");
            }
            catch (Exception ex)
            {
                CommandConsole.LogError($"Unable to initialize Riptide server! Ex: {ex}");
                logger.LogError($"Unable to initialize Riptide server! Ex: {ex}");
            }
        }
        
        public void StartClient()
        {
            if (riptideNetworking == null)
            {
                logger.LogError("RiptideNetworking not initialized!");
                return;
            }
            
            logger.LogDebug("Starting Riptide Client...");
            
            try
            {
                // Connect to the server
                riptideNetworking.ConnectClient();
                CommandConsole.Log("Riptide client started!");
                logger.LogInfo("Riptide client started!");
            }
            catch (Exception ex)
            {
                CommandConsole.LogError($"Unable to initialize Riptide client! Ex: {ex}");
                logger.LogError($"Unable to initialize Riptide client! Ex: {ex}");
            }
        }
        
        public void DisconnectClient()
        {
            if (riptideNetworking != null)
            {
                riptideNetworking.Disconnect();
                logger.LogInfo("Disconnected from Riptide server");
            }
        }
        
        private void LogLocalIPs()
        {
            // Get local IP addresses for connection information
            var localIPs = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName())
                .AddressList
                .Where(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                .Select(ip => ip.ToString())
                .ToArray();
            
            if (localIPs.Length > 0)
            {
                logger.LogInfo("Local IP Addresses for connections:");
                foreach (var ip in localIPs)
                {
                    logger.LogInfo($"  - {ip}");
                    CommandConsole.Log($"Server IP: {ip}");
                }
            }
            else
            {
                logger.LogWarning("No local IP addresses found!");
            }
        }
        
        // Handle client connections
        public void OnClientConnected(ushort clientId, string playerName)
        {
            // Only spawn players on the server
            if (!riptideNetworking.IsServer)
                return;
                
            if (riptideNetworking.PlayerPrefab == null)
            {
                logger.LogError("Player prefab not registered!");
                return;
            }
            
            // Spawn the player instance
            GameObject player = UnityEngine.Object.Instantiate(riptideNetworking.PlayerPrefab);
            player.SetActive(true);
            player.name = $"{PlayerPrefabName}({clientId})";
            
            // Initialize the NetworkPlayer component
            var networkPlayerComponent = player.GetComponent<NetworkPlayer>();
            if (networkPlayerComponent == null)
            {
                logger.LogError("Instantiated player missing NetworkPlayer component!");
                UnityEngine.Object.Destroy(player);
                return;
            }
            
            // Initialize the player with the client ID
            networkPlayerComponent.Initialize(clientId);
            
            // Add to active players dictionary
            RegisterActivePlayer(clientId, player);
            
            logger.LogInfo($"Player spawned for client {clientId}!");
        }
        
        public void OnClientDisconnected(ushort clientId)
        {
            // Clean up player object when client disconnects
            if (activePlayers.TryGetValue(clientId, out GameObject playerObject))
            {
                UnityEngine.Object.Destroy(playerObject);
                UnregisterActivePlayer(clientId);
                logger.LogInfo($"Player removed for client {clientId}");
            }
        }
        
        public void LogConnectedPlayers()
        {
            if (riptideNetworking == null || !riptideNetworking.IsServer)
            {
                logger.LogInfo("Not running as server - no player information available");
                return;
            }
            
            // Use RiptideNetworking's method to log connected clients
            riptideNetworking.LogConnectedClients();
        }
        
        public void SimulatePlayerJoin(string playerName = "TestPlayer")
        {
            if (riptideNetworking == null || !riptideNetworking.IsServer)
            {
                logger.LogWarning("Cannot simulate player join - not running as server");
                return;
            }
            
            // Use RiptideNetworking's method to simulate a player join
            riptideNetworking.SimulatePlayerJoin(playerName);
        }
        
        // Active player management
        private void RegisterActivePlayer(ushort clientId, GameObject playerObject)
        {
            if (!activePlayers.ContainsKey(clientId))
            {
                activePlayers.Add(clientId, playerObject);
            }
            else
            {
                logger.LogWarning($"Player with clientId {clientId} already registered!");
            }
        }
        
        private void UnregisterActivePlayer(ushort clientId)
        {
            if (activePlayers.ContainsKey(clientId))
            {
                activePlayers.Remove(clientId);
            }
            else
            {
                logger.LogWarning($"Player with clientId {clientId} not found for unregistering!");
            }
        }
        
        // Helper class for console commands
        public static class CommandConsole
        {
            public static void Log(string message)
            {
                // This would interface with your command console system
                Debug.Log($"[Command] {message}");
            }
            
            public static void LogError(string message)
            {
                Debug.LogError($"[Command Error] {message}");
            }
            
            public static void LogWarning(string message)
            {
                Debug.LogWarning($"[Command Warning] {message}");
            }
        }
    }
} 