using System;
using System.Collections;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;
using White_Knuckle_Multiplayer.Networking;

namespace White_Knuckle_Multiplayer.Managers;

internal class CommandManager
{
    private readonly GameManager gameManager;
    private readonly ManualLogSource logger;
    private readonly MonoBehaviour coroutineHost;
    private readonly CoroutineRunner coroutineRunner;

    private const string MessageShutdown = "Shutting down host and clients...";
    private const string SceneMainMenu = "Main-Menu";

    // Events
    private MirageNetworking mirageNetworking => gameManager.GetMirageNetworking();

    public CommandManager(GameManager gameManager, ManualLogSource logger, MonoBehaviour coroutineHost, CoroutineRunner coroutineRunner)
    {
        this.gameManager = gameManager;
        this.logger = logger;
        this.coroutineHost = coroutineHost;
        this.coroutineRunner = coroutineRunner;
    }

    public void HandleLocalHostCommand(string[] args)
    {
        logger.LogInfo("Starting local Mirage server...");
        
        try
        {
            // Start server and connect local client
            gameManager.StartHost();
            
            CommandConsole.Log("Mirage server started!");
        }
        catch (Exception ex)
        {
            logger.LogError($"Error starting Mirage server: {ex.Message}");
            CommandConsole.LogError($"Error starting Mirage server: {ex.Message}");
        }
    }
    
    public void HandleLocalJoinCommand(string[] args)
    {
        string serverAddress = "localhost";
        if (args.Length >= 1 && !string.IsNullOrEmpty(args[0]))
        {
            serverAddress = args[0];
        }
        
        logger.LogInfo($"Joining local Mirage server at {serverAddress}...");
        
        try
        {
            // Start the client
            gameManager.StartClient(serverAddress);
            
            CommandConsole.Log($"Connecting to local Mirage server at {serverAddress}...");
        }
        catch (Exception ex)
        {
            logger.LogError($"Error connecting to Mirage server: {ex.Message}");
            CommandConsole.LogError($"Error connecting to Mirage server: {ex.Message}");
        }
    }

    public void HandleDisconnectCommand(string[] args)
    {
        logger.LogInfo("HandleDisconnectCommand called");
        try
        {
            CommandConsole.Log("Disconnecting from Mirage server/stopping Mirage server...");
            gameManager.DisconnectClient();
            CommandConsole.Log("Disconnected from Mirage network");
        }
        catch (Exception ex)
        {
            logger.LogError($"Error disconnecting: {ex.Message}");
            CommandConsole.LogError($"Error disconnecting: {ex.Message}");
        }
    }
    
    private void RegisterMirageEventHandlers()
    {
        if (mirageNetworking == null)
        {
            logger.LogWarning("Cannot register Mirage events - MirageNetworking not initialized");
            return;
        }
        
        // Clear previous handlers to avoid duplicates
        mirageNetworking.OnClientConnected -= OnMirageClientConnected;
        mirageNetworking.OnClientDisconnected -= OnMirageClientDisconnected;
        mirageNetworking.OnServerStarted -= OnMirageServerStarted;
        mirageNetworking.OnServerStopped -= OnMirageServerStopped;
        
        // Register new handlers
        mirageNetworking.OnClientConnected += OnMirageClientConnected;
        mirageNetworking.OnClientDisconnected += OnMirageClientDisconnected;
        mirageNetworking.OnServerStarted += OnMirageServerStarted;
        mirageNetworking.OnServerStopped += OnMirageServerStopped;
    }
    
    private void OnMirageClientConnected(string message)
    {
        CommandConsole.Log(message);
        logger.LogInfo($"Mirage client connected: {message}");
    }
    
    private void OnMirageClientDisconnected(string message)
    {
        CommandConsole.Log(message);
        logger.LogInfo($"Mirage client disconnected: {message}");
    }
    
    private void OnMirageServerStarted(string message)
    {
        CommandConsole.Log(message);
        logger.LogInfo($"Mirage server started: {message}");
    }
    
    private void OnMirageServerStopped(string message)
    {
        CommandConsole.Log(message);
        logger.LogInfo($"Mirage server stopped: {message}");
    }
    
    // Methods to display players and simulate spawning
    public void HandlePlayersCommand(string[] args)
    {
        if (mirageNetworking == null)
        {
            CommandConsole.LogError("Mirage networking not initialized");
            return;
        }
        
        mirageNetworking.LogConnectedClients();
    }
    
}
