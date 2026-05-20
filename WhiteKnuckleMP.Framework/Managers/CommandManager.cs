using System;
using WhiteKnuckleMP.Utils;

namespace WhiteKnuckleMP.Framework.Managers;

public class CommandManager
{
    private readonly NetworkManager _netManager;
    private const string MessageShutdown = "Shutting down host and clients...";
    private const string SceneMainMenu = "Main-Menu";

    // Events

    public CommandManager(NetworkManager netManager)
    {
        _netManager = netManager;
    }

    public void HandleLocalHostCommand(string[] args)
    {
        try
        {
            StartServerAndConnectToLocalClient();
        }
        catch (Exception ex)
        {
            LogManager.Error($"Error starting WKNetworking server: {ex.Message}");
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

        try
        {
            JoinLocalServer(serverAddress, serverPort);
        }
        catch (Exception ex)
        {
            LogManager.Error($"Error connecting to server: {ex.Message}");
        }
    }

    public void HandleDisconnectCommand(string[] args)
    {
        LogManager.Info("HandleDisconnectCommand called");
        try
        {
            DisconnectFromNetwork();
        }
        catch (Exception ex)
        {
            LogManager.Error($"Error disconnecting: {ex.Message}");
            CommandConsole.LogError($"Error disconnecting: {ex.Message}");
        }
    }

    private void StartServerAndConnectToLocalClient()
    {
        LogManager.Info("Starting local WKNetworking server...");
        _netManager.Host();
        LogManager.Info("WKNetworking server started!");
    }

    private void JoinLocalServer(string address, ushort port)
    {
        LogManager.Info($"Joining local WKNetworking server at {address}...");
        _netManager.Join(address, port);
        LogManager.Info("Connected to local WKNetworking server!");
    }
    
    private void DisconnectFromNetwork()
    {
        LogManager.Info("Disconnecting from WKNetworking server/stopping WKNetworking server...");
        _netManager.Client.Disconnect();
        if (_netManager.IsServer)
        {
            _netManager.Server.Stop();
        }
        LobbyManager.Instance.ResetReadyState();
        CommandConsole.Log("Disconnected from WKNetworking server");
    }
    
}
