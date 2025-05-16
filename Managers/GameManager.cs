using System;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using White_Knuckle_Multiplayer.Networking;
using Object = UnityEngine.Object;

namespace White_Knuckle_Multiplayer.Managers;

public class GameManager
{
    private readonly ManualLogSource logger;
    
    // Riptide networking
    internal RiptideNetworking riptideNetworking;
    private const string RiptideNetworkingObjectName = "RiptideNetworking";
    private const string PlayerObjectName = "CL_Player";
    private const string PlayerPrefabName = "CL_Player_Network_Prefab";

    public GameManager(ManualLogSource logger)
    {
        this.logger = logger;
    }
    
    public RiptideNetworking GetRiptideNetworking() => riptideNetworking;
    
    public void InitializeRiptideNetworking()
    {
        if (riptideNetworking != null) return;
        
        var riptideNetworkingObject = new GameObject(RiptideNetworkingObjectName);
        Object.DontDestroyOnLoad(riptideNetworkingObject);
        
        riptideNetworking = riptideNetworkingObject.AddComponent<RiptideNetworking>();
        
        // Set up event handlers for logging
        SetupRiptideEventHandlers();
        
        logger.LogInfo("Initialized Riptide Networking");
    }
    
    private void SetupRiptideEventHandlers()
    {
        if (riptideNetworking == null) return;
        
        // Clear any existing handlers
        riptideNetworking.OnClientConnected -= OnRiptideClientConnected;
        riptideNetworking.OnClientDisconnected -= OnRiptideClientDisconnected;
        riptideNetworking.OnServerStarted -= OnRiptideServerStarted;
        riptideNetworking.OnServerStopped -= OnRiptideServerStopped;
        
        // Add event handlers
        riptideNetworking.OnClientConnected += OnRiptideClientConnected;
        riptideNetworking.OnClientDisconnected += OnRiptideClientDisconnected;
        riptideNetworking.OnServerStarted += OnRiptideServerStarted;
        riptideNetworking.OnServerStopped += OnRiptideServerStopped;
    }
    
    private void OnRiptideClientConnected(string message)
    {
        logger.LogInfo($"Riptide connection event: {message}");
    }
    
    private void OnRiptideClientDisconnected(string message)
    {
        logger.LogInfo($"Riptide disconnection event: {message}");
    }
    
    private void OnRiptideServerStarted(string message)
    {
        logger.LogInfo($"Riptide server event: {message}");
    }
    
    private void OnRiptideServerStopped(string message)
    {
        logger.LogInfo($"Riptide server event: {message}");
    }
    
    public void SetRiptideClientAddress(string clientAddress)
    {
        if (riptideNetworking != null)
        {
            riptideNetworking.ClientConnectAddress = clientAddress;
            logger.LogInfo($"Set Riptide client address to {clientAddress}");
        }
    }

    private void CreatePlayerPrefab()
    {
        logger.LogDebug("Creating Player Prefab...");
        GameObject player = GameObject.Find(PlayerObjectName);
        if (player == null)
        {
            logger.LogError("Cannot start host, no player found!");
            return;
        }

        GameObject playerPrefab = InstantiatePlayerPrefab(player);
        DestroyUnwantedComponents(playerPrefab);

        if (riptideNetworking != null)
        {
            // Set the player prefab
            riptideNetworking.SetPlayerPrefab(playerPrefab);
            logger.LogInfo("Set player prefab for Riptide networking");
        }
        
        logger.LogDebug("Player Prefab created");
    }

    private GameObject InstantiatePlayerPrefab(GameObject player)
    {
        GameObject prefab = Object.Instantiate(player);
        Object.DontDestroyOnLoad(prefab);

        prefab.name = PlayerPrefabName;
        prefab.SetActive(false);

        return prefab;
    }

    private void DestroyUnwantedComponents(GameObject prefab)
    {
        Object.Destroy(prefab.GetComponent<CharacterController>());
        Object.Destroy(prefab.GetComponent<Inventory>());
        Object.Destroy(prefab.GetComponent<MonoBehaviour>());

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
                Object.Destroy(camObject.GetComponent<CRTEffect>());
                Object.Destroy(camObject.GetComponent<PostProcessVolume>());
                Object.Destroy(camObject.GetComponent<PostProcessLayer>());
                Object.Destroy(camObject.GetComponent<Camera>());
                Object.Destroy(camObject.GetComponent<CameraShaderController>());
            }
        }

        var unwantedGameObjects = new[]
        {
            "Main Cam Root/Main Camera Shake Root/Main Camera/Inventory Camera/Inventory",
            "Main Cam Root/Main Camera Shake Root/Main Camera/Inventory Camera/InventoryBagCamera",
            "Main Cam Target", "Capsule", "Particle System", "Wind Sound", "Fatigue Sound",
            "CorruptionSurround", "Aim Circle", "Fake Handholds", "FXCam", "EffectRoot", "Death Sound"
        };

        foreach (var path in unwantedGameObjects)
        {
            var unwantedObject = prefab.transform.Find(path);
            if (unwantedObject != null) Object.Destroy(unwantedObject.gameObject);
        }
    }

    public void StartHost()
    {
        // Create player prefab
        CreatePlayerPrefab();
        
        // Initialize Riptide networking if needed
        InitializeRiptideNetworking();
        
        try
        {
            logger.LogInfo("Starting local Riptide server...");
            riptideNetworking.StartServer();
            
            // Connect local client to server
            riptideNetworking.ConnectClient();
            
            logger.LogInfo("Riptide server started!");
            
            // Log local IPs for others to connect
            LogLocalIps();
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to start Riptide host: {ex.Message}");
        }
    }
    
    public void StartServer()
    {
        // Create player prefab
        CreatePlayerPrefab();
        
        // Initialize Riptide networking if needed
        InitializeRiptideNetworking();
        
        try
        {
            logger.LogInfo("Starting Riptide server...");
            riptideNetworking.StartServer();
            logger.LogInfo("Riptide server started!");
            
            // Log local IPs for others to connect
            LogLocalIps();
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to start Riptide server: {ex.Message}");
        }
    }
    
    public void StartClient(string serverAddress)
    {
        // Create player prefab
        CreatePlayerPrefab();
        
        // Initialize Riptide networking if needed
        InitializeRiptideNetworking();
        
        try
        {
            logger.LogInfo($"Joining Riptide server at {serverAddress}...");
            
            // Set client address
            SetRiptideClientAddress(serverAddress);
            
            // Connect to server
            riptideNetworking.ConnectClient();
            
            logger.LogInfo("Riptide client connecting...");
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to connect Riptide client: {ex.Message}");
        }
    }
    
    public void DisconnectClient()
    {
        if (riptideNetworking != null)
        {
            try
            {
                logger.LogInfo("Disconnecting from Riptide server...");
                riptideNetworking.Disconnect();
                logger.LogInfo("Disconnected from Riptide server");
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to disconnect Riptide client: {ex.Message}");
            }
        }
    }
    
    private void LogLocalIps()
    {
        try
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    logger.LogInfo($"Your local IP is {ip}. Others can join with 'join {ip}'");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to get local IP addresses: {ex.Message}");
        }
    }
    
    public void LogConnectedPlayers()
    {
        if (riptideNetworking != null)
        {
            riptideNetworking.LogConnectedClients();
        }
        else
        {
            logger.LogWarning("Riptide networking is not initialized");
        }
    }
    
    public void SimulatePlayerJoin(string playerName)
    {
        if (riptideNetworking != null)
        {
            riptideNetworking.SimulatePlayerJoin(playerName);
        }
        else
        {
            logger.LogWarning("Riptide networking is not initialized");
        }
    }
}