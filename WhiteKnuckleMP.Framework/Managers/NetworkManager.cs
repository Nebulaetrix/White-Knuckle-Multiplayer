using System;
using Riptide;
using Riptide.Utils;
using UnityEngine;
using WhiteKnuckleMP.Utils;

namespace WhiteKnuckleMP.Framework.Managers;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance = null!;

    public bool IsServer => Server?.IsRunning ?? false;

    public Server Server { get; private set; } = null!;
    public Client Client { get; private set; } = null!;

    public static ushort LocalClientId => Instance.Client.Id;
    
    // Dummy
    private GameObject dummyPrefab = null!;
    

    /// <summary>
    /// Initializes the NetworkManager instance.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);

        Server = new Server();
        Client = new Client();

        Client.Connected += OnJoinedServer;
        Client.ConnectionFailed += OnClientConnectionFailed;
        Server.ClientConnected += OnServerClientConnected;
    }


    private void FixedUpdate()
    {
        Server.Update();
        Client.Update();
    }

    private void OnApplicationQuit()
    {
        Server.Stop();
        Client.Disconnect();
    }

    /// <summary>
    /// Creates the Server and Auto-joins it by default
    /// </summary>
    /// <param name="ip">The IP Address that it should be hosted on. Defaults to 127.0.0.1 .</param>
    /// <param name="port">The port on which the server should be hosted. Defaults to 7777.</param>
    /// <param name="maxClients">The maximum number of clients that can join. Defaults to 256.</param>
    /// <param name="autoJoin">Whether to auto-join when hosting. Defaults to true.</param>
    public void Host(string ip = "127.0.0.1", ushort port = 7777, ushort maxClients = 256, bool autoJoin = true)
    {
        Server.Start(port, maxClients);
        if (autoJoin)
            Client.Connect($"{ip}:{port}");
    }

    /// <summary>
    /// Joins as a Client.
    /// </summary>
    /// <param name="ip">IP Address of the host.</param>
    /// <param name="port">Port which the host is hosting on.</param>
    public void Join(string ip, ushort port)
    {
        Client.Connect($"{ip}:{port}");
    }

    public void StartLanHost(ushort port)
    {
        Host(port: port);
    }

    public void ConnectToLanHost(string ip, ushort port)
    {
        Join(ip, port);
    }
    
    private void OnJoinedServer(object sender, EventArgs e)
    {
        LogManager.Client.Info("Client connected to server safely!");
    }

    private void OnClientConnectionFailed(object sender, ConnectionFailedEventArgs e)
    {
        LogManager.Client.Error($"Failed to connect, reason: {e.Reason}\n{e.Message}");
    }

    private void OnServerClientConnected(object sender, ServerConnectedEventArgs e)
    {
        throw new NotImplementedException();
    }
    
    private void CreateDummyPrefab()
    {
        throw new NotImplementedException();
    }

    private void OnDestroy()
    {
        if (Server.IsRunning) Server.Stop();
        if (Client.IsConnected) Client.Disconnect();
    }
}
