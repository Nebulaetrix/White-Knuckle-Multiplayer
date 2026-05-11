using System;
using UnityEngine;
using Riptide;
using Riptide.Utils;
using Unity.Mathematics;

namespace White_Knuckle_Multiplayer.Managers;

public class MultiplayerGameManager : MonoBehaviour
{
    public static MultiplayerGameManager Instance;

    public bool IsServer => Server.IsRunning;

    public Server Server { get; private set; }
    public Client Client { get; private set; }
    
    // Dummy
    private GameObject dummyPrefab;
    

    private void Awake()
    {
        Instance = this;
        
        RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);

        Server = new Server();
        Client = new Client();

        Client.Connected += OnJoinedServer;
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

    private void OnJoinedServer(object sender, EventArgs e)
    {
        LogManager.Net.Info("Server joined!");
    }

    private void CreateDummyPrefab()
    {
        throw new NotImplementedException();
    }
}