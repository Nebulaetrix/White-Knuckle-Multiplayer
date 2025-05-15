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
    
    // Direct Mirage networking
    internal MirageNetworking mirageNetworking;
    
    private const string MirageNetworkingObjectName = "MirageNetworking";
    private const string PlayerObjectName = "CL_Player";
    private const string PlayerPrefabName = "CL_Player_Network_Prefab";

    public GameManager(ManualLogSource logger)
    {
        this.logger = logger;
    }
    
    public MirageNetworking GetMirageNetworking() => mirageNetworking;
    
    public void InitializeMirageNetworking()
    {
        if (mirageNetworking != null) return;
        
        var mirageNetworkingObject = new GameObject(MirageNetworkingObjectName);
        Object.DontDestroyOnLoad(mirageNetworkingObject);
        
        mirageNetworking = mirageNetworkingObject.AddComponent<MirageNetworking>();
        
        // Set up event handlers for logging
        SetupMirageEventHandlers();
        
        logger.LogInfo("Initialized Mirage Networking");
    }
    
    private void SetupMirageEventHandlers()
    {
        if (mirageNetworking == null) return;
        
        // Clear any existing handlers
        mirageNetworking.OnClientConnected -= OnMirageClientConnected;
        mirageNetworking.OnClientDisconnected -= OnMirageClientDisconnected;
        mirageNetworking.OnServerStarted -= OnMirageServerStarted;
        mirageNetworking.OnServerStopped -= OnMirageServerStopped;
        
        // Add event handlers
        mirageNetworking.OnClientConnected += OnMirageClientConnected;
        mirageNetworking.OnClientDisconnected += OnMirageClientDisconnected;
        mirageNetworking.OnServerStarted += OnMirageServerStarted;
        mirageNetworking.OnServerStopped += OnMirageServerStopped;
    }
    
    private void OnMirageClientConnected(string message)
    {
        logger.LogInfo($"Mirage connection event: {message}");
    }
    
    private void OnMirageClientDisconnected(string message)
    {
        logger.LogInfo($"Mirage disconnection event: {message}");
    }
    
    private void OnMirageServerStarted(string message)
    {
        logger.LogInfo($"Mirage server event: {message}");
    }
    
    private void OnMirageServerStopped(string message)
    {
        logger.LogInfo($"Mirage server event: {message}");
    }
    
    public void SetMirageClientAddress(string clientAddress)
    {
        if (mirageNetworking != null)
        {
            mirageNetworking.ClientConnectAddress = clientAddress;
            logger.LogInfo($"Set Mirage client address to {clientAddress}");
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

        if (mirageNetworking != null)
        {
            // Use the SetPlayerPrefab method to ensure NetworkIdentity is added
            mirageNetworking.SetPlayerPrefab(playerPrefab);
            logger.LogInfo("Set player prefab for Mirage networking");
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
        
        // Initialize Mirage networking if needed
        InitializeMirageNetworking();
        
        try
        {
            logger.LogInfo("Starting local Mirage server...");
            mirageNetworking.StartServer();
            
            // Connect local client to server
            mirageNetworking.ConnectClient();
            
            logger.LogInfo("Mirage server started!");
            
            // Log local IPs for others to connect
            LogLocalIps();
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to start Mirage host: {ex.Message}");
        }
    }
    
    public void StartServer()
    {
        // Create player prefab
        CreatePlayerPrefab();
        
        // Initialize Mirage networking if needed
        InitializeMirageNetworking();
        
        try
        {
            logger.LogInfo("Starting Mirage server...");
            mirageNetworking.StartServer();
            logger.LogInfo("Mirage server started!");
            
            // Log local IPs for others to connect
            LogLocalIps();
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to start Mirage server: {ex.Message}");
        }
    }
    
    public void StartClient(string serverAddress)
    {
        // Create player prefab
        CreatePlayerPrefab();
        
        // Initialize Mirage networking if needed
        InitializeMirageNetworking();
        
        try
        {
            logger.LogInfo($"Joining local Mirage server at {serverAddress}...");
            
            // Set client address
            SetMirageClientAddress(serverAddress);
            
            // Connect to server
            mirageNetworking.ConnectClient();
            
            logger.LogInfo("Mirage client connecting...");
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to connect Mirage client: {ex.Message}");
        }
    }
    
    public void DisconnectClient()
    {
        if (mirageNetworking != null)
        {
            try
            {
                logger.LogInfo("Disconnecting from Mirage server...");
                mirageNetworking.Disconnect();
                logger.LogInfo("Disconnected from Mirage server");
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to disconnect Mirage client: {ex.Message}");
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
                    logger.LogInfo($"Your local IP is {ip}. Others can join with 'localjoin {ip}'");
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
        if (mirageNetworking != null)
        {
            mirageNetworking.LogConnectedClients();
        }
        else
        {
            logger.LogWarning("Mirage networking is not initialized");
        }
    }
    
}