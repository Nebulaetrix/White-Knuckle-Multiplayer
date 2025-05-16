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
    private RiptideNetworking riptideNetworking => gameManager.GetRiptideNetworking();

    public CommandManager(GameManager gameManager, ManualLogSource logger, MonoBehaviour coroutineHost, CoroutineRunner coroutineRunner)
    {
        this.gameManager = gameManager;
        this.logger = logger;
        this.coroutineHost = coroutineHost;
        this.coroutineRunner = coroutineRunner;
    }

    public void HandleLocalHostCommand(string[] args)
    {
        logger.LogInfo("Starting local Riptide server...");
        
        try
        {
            // Start server and connect local client
            gameManager.StartHost();
            
            CommandConsole.Log("Riptide server started!");
        }
        catch (Exception ex)
        {
            logger.LogError($"Error starting Riptide server: {ex.Message}");
            CommandConsole.LogError($"Error starting Riptide server: {ex.Message}");
        }
    }
    
    public void HandleLocalJoinCommand(string[] args)
    {
        string serverAddress = "localhost";
        if (args.Length >= 1 && !string.IsNullOrEmpty(args[0]))
        {
            serverAddress = args[0];
        }
        
        logger.LogInfo($"Joining Riptide server at {serverAddress}...");
        
        try
        {
            // Start the client
            gameManager.StartClient(serverAddress);
            
            CommandConsole.Log($"Connecting to Riptide server at {serverAddress}...");
        }
        catch (Exception ex)
        {
            logger.LogError($"Error connecting to Riptide server: {ex.Message}");
            CommandConsole.LogError($"Error connecting to Riptide server: {ex.Message}");
        }
    }

    public void HandleDisconnectCommand(string[] args)
    {
        logger.LogInfo("HandleDisconnectCommand called");
        try
        {
            CommandConsole.Log("Disconnecting from Riptide server/stopping Riptide server...");
            gameManager.DisconnectClient();
            CommandConsole.Log("Disconnected from Riptide network");
        }
        catch (Exception ex)
        {
            logger.LogError($"Error disconnecting: {ex.Message}");
            CommandConsole.LogError($"Error disconnecting: {ex.Message}");
        }
    }
    
    private void RegisterRiptideEventHandlers()
    {
        if (riptideNetworking == null)
        {
            logger.LogWarning("Cannot register Riptide events - RiptideNetworking not initialized");
            return;
        }
        
        // Clear previous handlers to avoid duplicates
        riptideNetworking.OnClientConnected -= OnRiptideClientConnected;
        riptideNetworking.OnClientDisconnected -= OnRiptideClientDisconnected;
        riptideNetworking.OnServerStarted -= OnRiptideServerStarted;
        riptideNetworking.OnServerStopped -= OnRiptideServerStopped;
        
        // Register new handlers
        riptideNetworking.OnClientConnected += OnRiptideClientConnected;
        riptideNetworking.OnClientDisconnected += OnRiptideClientDisconnected;
        riptideNetworking.OnServerStarted += OnRiptideServerStarted;
        riptideNetworking.OnServerStopped += OnRiptideServerStopped;
    }
    
    private void OnRiptideClientConnected(string message)
    {
        CommandConsole.Log(message);
        logger.LogInfo($"Riptide client connected: {message}");
    }
    
    private void OnRiptideClientDisconnected(string message)
    {
        CommandConsole.Log(message);
        logger.LogInfo($"Riptide client disconnected: {message}");
    }
    
    private void OnRiptideServerStarted(string message)
    {
        CommandConsole.Log(message);
        logger.LogInfo($"Riptide server started: {message}");
    }
    
    private void OnRiptideServerStopped(string message)
    {
        CommandConsole.Log(message);
        logger.LogInfo($"Riptide server stopped: {message}");
    }
    
    // Methods to display players and simulate spawning
    public void HandlePlayersCommand(string[] args)
    {
        if (riptideNetworking == null)
        {
            CommandConsole.LogError("Riptide networking not initialized");
            return;
        }
        
        riptideNetworking.LogConnectedClients();
    }
}
