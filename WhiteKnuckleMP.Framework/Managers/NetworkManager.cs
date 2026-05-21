using System;
using System.Threading;
using Riptide;
using Riptide.Utils;
using UnityEngine;
using WhiteKnuckleMP.Utils;

namespace WhiteKnuckleMP.Framework.Managers;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance = null!;

    public bool IsServer => Server.IsRunning;

    public Server Server { get; private set; } = null!;
    public Client Client { get; private set; } = null!;

    public static ushort LocalClientId => Instance.Client.Id;
    
    // Target network tick rate (60 ticks per second)
    private const float TargetTickRate = 60f;
    private const float TickInterval = 1f / TargetTickRate;
    private float _tickTimer = 0f;

    
    
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

        Server = new Server
        {
            TimeoutTime = 20000
        };
        Client = new Client
        {
            TimeoutTime = 20000
        };

        Client.Connected += OnJoinedServer;
        Client.ConnectionFailed += OnClientConnectionFailed;
        Client.Disconnected += OnClientDisconnected;
        Server.ClientConnected += OnServerClientConnected;
    }

    private void Update()
    {
        _tickTimer += Time.unscaledDeltaTime;

        while (_tickTimer >= TickInterval)
        {
            Server.Update();
            Client.Update();
            
            _tickTimer -= TickInterval;
        }
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
        
        StateManager.Instance.TransitionTo(StateManager.State.InLobby);
        LobbyManager.Instance.TryNotifyServerIAmReady();
    }

    private void OnClientConnectionFailed(object sender, ConnectionFailedEventArgs e)
    {
        LogManager.Client.Error($"Failed to connect, reason: {e.Reason}\n{e.Message}");
    }

    private void OnClientDisconnected(object sender, DisconnectedEventArgs e)
    {
        LogManager.Client.Info($"Client disconnected from server. Reason: {e.Reason}");
        LobbyManager.Instance.ResetReadyState();
    }

    private void OnServerClientConnected(object sender, ServerConnectedEventArgs e)
    {
        if (e.Client.Id == LocalClientId) return;
        LogManager.Server.Info($"A new client has joined! ID: {e.Client.Id}");
        
        LobbyManager.Instance.AddPlayerToLobby(e.Client.Id, $"Player {e.Client.Id}", 0);
    }

    private void OnDestroy()
    {
        if (Server.IsRunning) Server.Stop();
        if (Client.IsConnected) Client.Disconnect();
    }
}
