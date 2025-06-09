using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using White_Knuckle_Multiplayer.Networking;
using White_Knuckle_Multiplayer.Networking.Messages;
using White_Knuckle_Multiplayer.Networking.Routing;
using White_Knuckle_Multiplayer.Utils;

namespace White_Knuckle_Multiplayer.Managers;

public class PlayerStateManager : MonoBehaviour
{
    public static PlayerStateManager Instance { get; private set; }

    [System.Serializable]
    public enum PlayerState
    {
        Disconnected,
        ConnectedToLobby,
        LoadingGame,
        ReadyInGame,
        InGame
    }

    [System.Serializable]
    public class PlayerInfo
    {
        public ushort netID;
        public string username;
        public PlayerState state;
        public float lastPingTime;
        public bool isHost;

        public PlayerInfo(ushort _netID, string _username, bool _isHost)
        {
            netID = _netID;
            username = _username;
            state = PlayerState.InGame;
            lastPingTime = Time.time;
            isHost = _isHost;
        }
    }
    
    // Player Tracking
    private readonly Dictionary<ushort, PlayerInfo> playerStates = new();
    private PlayerState localPlayerState = PlayerState.Disconnected;
    
    // Game State tracking
    public bool IsInLobby => LobbyManager.Instance != null && LobbyManager.Instance.isInLobby;
    public bool IsInGame => LobbyManager.Instance != null && LobbyManager.Instance.isInGame;
    public bool CanSpawnPlayers => IsInGame && localPlayerState == PlayerState.InGame;
    
    // Events
    public event Action<ushort, PlayerState> OnPlayerStateChanged;
    public event Action<PlayerState> OnLocalPlayerStateChanged;
    public event Action OnAllPlayersReady;

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

    private void Start()
    {
        // Subscribe to scene loading event
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    #region Local Player State Management
    
    public PlayerState GetLocalPlayerState() => localPlayerState;

    public void SetLocalPlayerState(PlayerState newState)
    {
        if (localPlayerState == newState) return;
        
        var oldState = localPlayerState;
        localPlayerState = newState;
        
        LogManager.Debug($"Local Player state changed: {oldState} -> {newState}");
        OnLocalPlayerStateChanged?.Invoke(newState);
        
        // Send state update to server if connected
        if (NetworkClient.Instance?.Client?.IsConnected == true)
        {
            MessageSender.SendPlayerStateUpdate(new PlayerStateUpdateData(
                NetworkClient.Instance.Client.Id, newState));
        }

        HandleLocalStateChange(oldState, newState);
    }

    private void HandleLocalStateChange(PlayerState from, PlayerState to)
    {
        switch (to)
        {
            case PlayerState.LoadingGame:
                LogManager.Debug("Local player started loading game...");
                break;
            
            case PlayerState.ReadyInGame:
                LogManager.Debug("Local player is ready in game, sending join request...");
                SendJoinRequest();
                break;
            
            case PlayerState.InGame:
                LogManager.Debug("Local player fully in game");
                CheckIfAllPlayersReady();
                break;
            
            case PlayerState.Disconnected:
                playerStates.Clear();
                break;
        }
    }
    
    #endregion
    
    #region Remote Player State Management

    public void UpdatePlayerState(ushort netId, PlayerState newState, string username = null)
    {
        if (!playerStates.TryGetValue(netId, out var playerInfo))
        {
            if (username != null)
            {
                LogManager.Debug($"Cannot create player info for {netId} without username");
                return;
            }
            
            playerInfo = new PlayerInfo(netId, username, netId == 1);
            playerStates[netId] = playerInfo;
            LogManager.Debug($"Added new player: {username} (ID: {netId})");
        }

        if (playerInfo.state == newState) return;
        
        var oldState = playerInfo.state;
        playerInfo.state = newState;
        playerInfo.lastPingTime = Time.time;
        
        LogManager.Debug($"Player {playerInfo.username} (ID: {netId}) state changed: {oldState} -> {newState}");
        OnPlayerStateChanged?.Invoke(netId, newState);

        if (newState == PlayerState.InGame)
        {
            CheckIfAllPlayersReady();
        }
    }

    public void RemovePlayer(ushort netId)
    {
        if (playerStates.TryGetValue(netId, out var playerInfo))
        {
            LogManager.Debug($"Removing player: {playerInfo.username} (ID: {netId})");
            playerStates.Remove(netId);
            OnPlayerStateChanged?.Invoke(netId, PlayerState.Disconnected);
        }
    }

    public PlayerInfo GetPlayerInfo(ushort netId)
    {
        return playerStates.GetValueOrDefault(netId);
    }

    public Dictionary<ushort, PlayerInfo> GetAllPlayers()
    {
        return new Dictionary<ushort, PlayerInfo>(playerStates);
    }
    
    #endregion
    
    #region Game Flow Management

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        LogManager.Debug($"Scene loaded: {scene.name}");

        if (scene.name == "Game-Main" && IsInGame)
        {
            // Scene loaded, wait for game objects to be ready
            SetLocalPlayerState(PlayerState.LoadingGame);

            // Start a coroutine to check when everything is ready
            StartCoroutine(WaitForGameReady());
        }
    }

    private IEnumerator WaitForGameReady()
    {
        yield return null;

        float timeout = 10f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            var player = GameObject.Find("CL_Player");
            var networking = MessageHandler.Instance;

            if (player != null && networking != null)
            {
                LogManager.Debug("Game objects ready, player can join");
                SetLocalPlayerState(PlayerState.ReadyInGame);
                yield break;
            }
            
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        
        LogManager.Debug("Timeout waiting for game ready");
        // Will need to handle timeout properly...
    }

    private void SendJoinRequest()
    {
        if (NetworkClient.Instance?.Client?.IsConnected != true)
        {
            LogManager.Debug("Cannot send join request - not connected");
            return;
        }

        string username;
        try
        {
            username = Steamworks.SteamFriends.GetPersonaName();
        }
        catch
        {
            username = $"Player_{NetworkClient.Instance.Client.Id}";
        }

        var modList = ModListHelper.GetLoadedModsList();
        MessageSender.SendJoinRequest(new JoinRequestData(username, MyPluginInfo.PLUGIN_VERSION, modList));
        
        SetLocalPlayerState(PlayerState.InGame);
    }

    private void CheckIfAllPlayersReady()
    {
        if (!IsInGame || localPlayerState != PlayerState.InGame) return;
        
        bool allReady = true;
        foreach (var playerInfo in playerStates.Values)
        {
            if (playerInfo.state != PlayerState.InGame)
            {
                allReady = false;
                break;
            }
        }

        if (allReady && playerStates.Count > 0)
        {
            LogManager.Debug("All players are ready in game!");
            OnAllPlayersReady?.Invoke();
        }
    }
    
    #endregion
    
    #region Public API

    public bool IsPlayerInGame(ushort netId)
    {
        var info = GetPlayerInfo(netId);
        return info != null && info.state == PlayerState.InGame;
    }

    public bool ShouldSpawnPlayer(ushort netId)
    {
        // Only Spawn players who are fully in game
        return CanSpawnPlayers && IsPlayerInGame(netId);
    }

    public int GetPlayerCount()
    {
        return playerStates.Count;
    }

    public int GetReadyPlayerCount()
    {
        int count = 0;
        foreach (var info in playerStates.Values)
        {
            if (info.state == PlayerState.InGame) count++;
        }
        return count;
    }

    /// <summary>
    /// Call this when starting to host game
    /// </summary>
    public void StartHosting(bool inGame = false)
    {
        SetLocalPlayerState(inGame ? PlayerState.ReadyInGame : PlayerState.ConnectedToLobby);
        LogManager.Debug("Started hosting - local player in lobby state");
    }

    /// <summary>
    /// Call this when connecting as a client
    /// </summary>
    public void StartAsClient(bool inGame = false)
    {
        SetLocalPlayerState(inGame ? PlayerState.ReadyInGame : PlayerState.ConnectedToLobby);
        LogManager.Debug("Connected as client - in lobby state");
    }

    /// <summary>
    /// Call this when Disconnecting
    /// </summary>
    public void HandleDisconnect()
    {
        SetLocalPlayerState(PlayerState.Disconnected);
        playerStates.Clear();
    }
    
    #endregion

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}