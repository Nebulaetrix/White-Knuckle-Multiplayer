using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using WhiteKnuckleMP.Networking;
using WhiteKnuckleMP.Utils;

namespace WhiteKnuckleMP.Framework.Managers;

public struct LobbyPlayerInfo
{
    public ushort NetId;
    public string Username;
    public ulong SteamId;
}

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; } = null!;

    public List<LobbyPlayerInfo> ConnectedLobbyPlayers { get; private set; } = [];

    public enum NetworkType
    {
        // ReSharper disable once InconsistentNaming
        LAN,
        Steam
    }

    public NetworkType CurrentNetworkType { get; private set; } = NetworkType.LAN;
    public string CurrentLobbyId { get; private set; } = string.Empty;

    private bool _isGameMainLoaded;
    private bool _hasSentClientReady;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        SceneManager.sceneLoaded += SceneLoaded;
    }

    private void SceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (scene.name == "Game-Main")
        {
            _isGameMainLoaded = true;
            TryNotifyServerIAmReady();
        }
        else
        {
            ResetReadyState();
        }
    }

    /// <summary>
    /// Tries to notify the server that the client is ready.
    /// </summary>
    public void TryNotifyServerIAmReady()
    {
        if (_hasSentClientReady)
            return;
        if (!_isGameMainLoaded)
            return;
        if (!NetworkManager.Instance.Client.IsConnected)
        {
            LogManager.Framework.Warn("Cannot send Clientready: client is not connected.");
            return;
        }
        
        NotifyServerIAmReady();
        _hasSentClientReady = true;
    }
    
    public void NotifyServerIAmReady()
    {
        if (!NetworkManager.Instance.Client.IsConnected)
        {
            LogManager.Framework.Warn("Cannot send Clientready: client is not connected.");
            return;
        }

        using var packet = new NetworkPacket(MessageIds.ClientReady);
        NetworkManager.Instance.Client.Send(packet.RawMessage);
    }

    /// <summary>
    /// Creates a lobby of the specified network type.
    /// </summary>
    /// <param name="type">The type of network to create the lobby for.</param>
    public void CreateLobby(NetworkType type)
    {
        CurrentNetworkType = type;
        LogManager.Framework.Info($"Creating {type.ToString()} lobby...");

        if (type == NetworkType.LAN)
        {
            CurrentLobbyId = "localhost";
            NetworkManager.Instance.StartLanHost(7777);
        }
        else
        {
            LogManager.Framework.Warn("Steam lobbies are not fully implemented yet!");
        }
    }

    /// <summary>
    /// Joins a lobby of the specified network type using the provided connection string.
    /// </summary>
    /// <param name="type">The type of network to join the lobby for.</param>
    /// <param name="connectionString">The connection string for the lobby.</param>
    public void JoinLobby(NetworkType type, string connectionString)
    {
        CurrentNetworkType = type;
        CurrentLobbyId = connectionString;
        LogManager.Framework.Info($"Joining {type.ToString()} lobby via: {connectionString}");

        if (type == NetworkType.LAN)
        {
            NetworkManager.Instance.ConnectToLanHost(connectionString, 7777);
        }
        else
        {
            LogManager.Framework.Warn("Steam lobbies are not fully implemented yet!");
        }
    }

    /// <summary>
    /// Adds a player to the lobby.
    /// </summary>
    /// <param name="netId">The network ID of the player.</param>
    /// <param name="username">The username of the player.</param>
    /// <param name="steamId">The Steam ID of the player.</param>
    public void AddPlayerToLobby(ushort netId, string username, ulong steamId)
    {
        if (ConnectedLobbyPlayers.Exists(p => p.NetId == netId)) return;

        var newPlayer = new LobbyPlayerInfo
        {
            NetId = netId,
            Username = username,
            SteamId = steamId
        };
        
        ConnectedLobbyPlayers.Add(newPlayer);
        
        LogManager.Framework.Info($"Added {username} to the lobby list. Total players: {ConnectedLobbyPlayers.Count}");
    }

    /// <summary>
    /// Removes a player from the lobby by their network ID.
    /// </summary>
    /// <param name="netId">The network ID of the player to remove.</param>
    public void RemovePlayerFromLobby(ushort netId)
    {
        ConnectedLobbyPlayers.RemoveAll(p => p.NetId == netId);
        LogManager.Framework.Info($"Removed Player ID {netId} from the lobby list.");
    }
    
    public int GetClientId()
    {
        if (NetworkManager.Instance.Client.IsConnected)
        {
            return NetworkManager.Instance.Client.Id;
        }

        return -1;
    }

    /// <summary>
    /// Resets the ready state of the client.
    /// </summary>
    public void ResetReadyState()
    {
        _isGameMainLoaded = false;
        _hasSentClientReady = false;
    }
}