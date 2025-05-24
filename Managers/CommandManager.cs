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
            CommandConsole.LogError($"Error starting WKNetworking  server: {ex.Message}");
        }
    }

    public void HandleSteamHostCommand(string[] args)
    {
        LogManager.Info("Starting Steam WKNetworking server...");

        try
        {
            // Start Steam Server and connect local steam client
            gameManager.StartSteamHost();
            CommandConsole.Log("Steam WKNetworking server started!");
        }
        catch (Exception ex)
        {
            LogManager.Error($"Error starting Steam WKNetworking server: {ex.Message}");
            CommandConsole.LogError($"Error starting Steam WKNetworking server: {ex.Message}");
        }
    }

    public void HandleSteamJoinCommand(string[] args)
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
        
        LogManager.Info($"Joining steam WKNetworking server at {serverAddress}...");
        
        try
        {
            // Start the client
            gameManager.StartSteamClient(serverAddress, serverPort);;
            
            CommandConsole.Log($"Connecting to steam WKNetworking server at {serverAddress}...");
        }
        catch (Exception ex)
        {
            LogManager.Error($"Error connecting to steam WKNetworking server: {ex.Message}");
            CommandConsole.LogError($"Error connecting to steam WKNetworking server: {ex.Message}");
        }
    }

    public void HandleSteamLobbyCreate(string[] args)
    {
        if (!SteamManager.initialized)
        {
            CommandConsole.LogError("Steam is not initialized yet!");
            return;
        }
        
        try
        {
            LobbyManager.Instance.CreateLobby("friend");
            // CommandConsole.Log($"Lobby created! ID: {LobbyManager.Instance.LobbyID}");
            // LogManager.SteamClient.Info($"Lobby Created; ID: {LobbyManager.Instance.LobbyID}");
        }
        catch (Exception e)
        {
            CommandConsole.LogError($"Error creating lobby!");
            LogManager.SteamClient.Error($"Error creating lobby: {e.Message}");
        }
    }

    public void HandleSteamLobbyJoin(string[] args)
    {
        if (!SteamManager.initialized)
        {
            CommandConsole.LogError("Steam is not initialized yet!");
            return;
        }
        
        if (args.Length >= 1 && !string.IsNullOrEmpty(args[0]))
        {
            try
            {
                LobbyManager.Instance.JoinLobby(ulong.Parse(args[0]));
                CommandConsole.Log($"Joined lobby with ID {args[0]}");
            }
            catch (Exception ex)
            {
                CommandConsole.LogError($"Error joining lobby with ID {args[0]}");
                LogManager.SteamClient.Error($"Error joining lobby: {ex.Message}");
            }
        }
        else
        {
            CommandConsole.LogError("You must specify a lobby ID.");
            LogManager.SteamClient.Error("You must specify a lobby ID.");
        }
    }

    public void HandleSteamLobbyLeave(string[] args)
    {
        if (!SteamManager.initialized)
        {
            CommandConsole.LogError("Steam is not initialized yet!");
            return;
        }
        
        try
        {
            LobbyManager.Instance.LeaveLobby();
            CommandConsole.Log("Lobby left!");
            LogManager.SteamClient.Info("Lobby left successfully!");
        }
        catch (Exception ex)
        {
            CommandConsole.LogError($"Error leaving lobby: {ex.Message}");
            LogManager.SteamClient.Error($"Error leaving lobby: {ex.Message}");
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

        LogManager.Info($"Joining local WKNetworking server at {serverAddress}...");
        
        try
        {
            // Start the client
            gameManager.StartClient(serverAddress, serverPort);
            
            CommandConsole.Log($"Connecting to local WKNetworking server at {serverAddress}...");
        }
        catch (Exception ex)
        {
            LogManager.Error($"Error connecting to  server: {ex.Message}");
            CommandConsole.LogError($"Error connecting to WKNetworking server: {ex.Message}");
        }
    }

    public void HandleDisconnectCommand(string[] args)
    {
        LogManager.Info("HandleDisconnectCommand called");
        try
        {
            CommandConsole.Log("Disconnecting from WKNetworking server/stopping WKNetworking server...");
            gameManager.DisconnectClient();
            CommandConsole.Log("Disconnected from WKNetworking server");
        }
        catch (Exception ex)
        {
            LogManager.Error($"Error disconnecting: {ex.Message}");
            CommandConsole.LogError($"Error disconnecting: {ex.Message}");
        }
    }
    
}
