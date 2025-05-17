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
    private readonly MonoBehaviour coroutineHost;
    private readonly CoroutineRunner coroutineRunner;

    private const string MessageShutdown = "Shutting down host and clients...";
    private const string SceneMainMenu = "Main-Menu";

    // Events

    public CommandManager(GameManager gameManager, MonoBehaviour coroutineHost, CoroutineRunner coroutineRunner)
    {
        this.gameManager = gameManager;
        this.coroutineHost = coroutineHost;
        this.coroutineRunner = coroutineRunner;
    }

    public void HandleLocalHostCommand(string[] args)
    {
        LogManager.Info("Starting local WKNetworking server...");
        
        try
        {
            // Start server and connect local client
            gameManager.StartHost();
            
            CommandConsole.Log("WKNetworking server started!");
        }
        catch (Exception ex)
        {
            LogManager.Error($"Error starting WKNetworking server: {ex.Message}");
            CommandConsole.LogError($"Error starting WKNetworking server: {ex.Message}");
        }
    }
    
    public void HandleLocalJoinCommand(string[] args)
    {
        string serverAddress = "127.0.0.1";
        ushort serverPort = 7777;
        if (args.Length >= 1 && !string.IsNullOrEmpty(args[0]))
        {
            serverAddress = args[0];
        }
        else if (args.Length >= 2 && !string.IsNullOrEmpty(args[0]) && !string.IsNullOrEmpty(args[1]))
        {
            serverAddress = args[0];
            serverPort = ushort.Parse(args[1]);
        }

            LogManager.Info($"Joining local Mirage server at {serverAddress}...");
        
        try
        {
            // Start the client
            gameManager.StartClient(serverAddress, serverPort);
            
            CommandConsole.Log($"Connecting to local Mirage server at {serverAddress}...");
        }
        catch (Exception ex)
        {
            LogManager.Error($"Error connecting to Mirage server: {ex.Message}");
            CommandConsole.LogError($"Error connecting to Mirage server: {ex.Message}");
        }
    }

    public void HandleDisconnectCommand(string[] args)
    {
        LogManager.Info("HandleDisconnectCommand called");
        try
        {
            CommandConsole.Log("Disconnecting from Mirage server/stopping Mirage server...");
            gameManager.DisconnectClient();
            CommandConsole.Log("Disconnected from Mirage network");
        }
        catch (Exception ex)
        {
            LogManager.Error($"Error disconnecting: {ex.Message}");
            CommandConsole.LogError($"Error disconnecting: {ex.Message}");
        }
    }
    
}
