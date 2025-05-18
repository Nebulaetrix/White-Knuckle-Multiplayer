using System;
using System.Net;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using White_Knuckle_Multiplayer.Networking;
using White_Knuckle_Multiplayer.Networking.Controllers;
using Object = UnityEngine.Object;

namespace White_Knuckle_Multiplayer.Managers;

public class GameManager
{

    private const string WKNetworkingObjectName = "WKNetworking";
    private const string PlayerObjectName = "CL_Player";
    private const string PlayerPrefabName = "CL_Player_Network_Prefab";

    public GameObject WKNetworkingObject { get; private set; }
    public NetworkClient networkClient { get; private set; }
    public NetworkServer networkServer { get; private set; }
    public MessageHandler messageHandler { get; private set; }

    public GameManager()
    {

    }

    public void InitializeWKNetworking()
    {
        if (WKNetworkingObject != null) return;

        // Persistent GameObject holding all networking stuff
        WKNetworkingObject = new GameObject(WKNetworkingObjectName);
        Object.DontDestroyOnLoad(WKNetworkingObject);

        // Attaching Networked objects to it
        networkClient = WKNetworkingObject.AddComponent<NetworkClient>();
        networkServer = WKNetworkingObject.AddComponent<NetworkServer>();
        messageHandler = WKNetworkingObject.AddComponent<MessageHandler>();

        SpriteCache.Preload(
            "Hands_idle",
            "Hands_grip",
            "HandsReach"
        );


        LogManager.Info("Initialized WKNetworking");
    }

    /// <summary>
    /// Starts a host: spins up a local server and then connects the client to it.
    /// </summary>
    public void StartHost()
    {
        // TODO: replace these variables with something better
        ushort maxClients = 10;
        ushort port = 7777;
        string address = "127.0.0.1";

        InitializeWKNetworking();

        try
        {
            LogManager.Info($"Starting host on port {port} (max {maxClients})...");
            networkServer.StartServer(port, maxClients);

            // Connect local client to server
            LogManager.Info($"Connecting local client to {address}:{port}...");
            networkClient.StartClient(address, port);

            LogManager.Info("Host started successfully.");

        }
        catch (Exception ex)
        {
            LogManager.Error($"Failed to start host: {ex.Message}");
        }
    }

    /// <summary>
    /// Connects as a client to an existing host.
    /// </summary>
    public void StartClient(string serverAddress = "127.0.0.1", ushort serverPort = 7777)
    {
        InitializeWKNetworking();

        try
        {
            LogManager.Info($"Connecting to server at {serverAddress}:{serverPort}...");
            networkClient.StartClient(serverAddress, serverPort);
        }
        catch (Exception ex)
        {
            LogManager.Error($"Failed to start client: {ex}");
        }
    }

    /// <summary>
    /// Disconnect this client (if connected).
    /// </summary>
    public void DisconnectClient()
    {
        if (networkClient != null && networkClient.Client == null) return;

        try
        {
            LogManager.Info("Disconnecting client...");
            networkClient.Client.Disconnect();
            LogManager.Info("Client disconnected.");
        }
        catch (Exception ex)
        {
            LogManager.Error($"Failed to disconnect client: {ex}");
        }
    }

    public void StopServer()
    {
        if (networkServer != null && networkServer.Server == null) return;

        try
        {
            LogManager.Info("Stopping server...");
            networkServer.StopServer();
            LogManager.Info("Server stopped.");
        }
        catch (Exception ex)
        {
            LogManager.Error($"Failed to stop server: {ex}");
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
                    LogManager.Info($"Your local IP is {ip}. Others can join with 'localjoin {ip}'");
                }
            }
        }
        catch (Exception ex)
        {
            LogManager.Error($"Failed to get local IP addresses: {ex.Message}");
        }
    }

}