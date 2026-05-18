using System;
using UnityEngine;
using WhiteKnuckleMP.Utils;

namespace WhiteKnuckleMP.Framework.Managers;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; } = null!;

    public enum NetworkType
    {
        // ReSharper disable once InconsistentNaming
        LAN,
        Steam
    }
    
    public NetworkType CurrentNetworkType { get; private set; } = NetworkType.LAN;
    public string CurrentLobbyId { get; private set; } = string.Empty;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void CreateLobby(NetworkType type)
    {
        CurrentNetworkType = type;
        LogManager.Framework.Info($"Creating {type.ToString()} lobby...");

        if (type == NetworkType.LAN)
        {
            CurrentLobbyId = "localhost";
            NetworkManager.Instance.StartLanHost(7777);
            StateManager.Instance.TransitionTo(StateManager.State.InLobby);
        }
        else
        {
            LogManager.Framework.Warn("Steam lobbies are not fully implemented yet!");
        }
    }

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
}