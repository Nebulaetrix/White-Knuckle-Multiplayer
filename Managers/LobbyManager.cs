using System;
using Steamworks;
using UnityEngine;
using White_Knuckle_Multiplayer.Networking;
using White_Knuckle_Multiplayer.Networking.SteamLayer;

namespace White_Knuckle_Multiplayer.Managers;

public class LobbyManager : MonoBehaviour
{
    #region Singleton
    
    public static LobbyManager Instance;

    private void Awake()
    {
        if (Instance == null || Instance == this)
        {
            Instance = this;
        }
        else
        {
            LogManager.SteamClient.Debug("LobbyManager already created, destroying duplicate");
            Destroy(gameObject);
        }
    }
    
    #endregion

    #region Steam Callbacks Init
    
    protected CallResult<LobbyCreated_t> LobbyCreatedCR;
    protected Callback<GameLobbyJoinRequested_t> GameLobbyJoinRequestedCR;
    protected CallResult<LobbyEnter_t> LobbyEnterCR;
    protected Callback<P2PSessionRequest_t> P2PSessionRequestCb;
    
    private void OnEnable()
    {
        if (!SteamManager.initialized)
        {
            LogManager.SteamClient.Error("Steam is not initialized");
            return;
        }

        LogManager.SteamClient.Warn("LobbyManager created");

        LobbyCreatedCR = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);
        GameLobbyJoinRequestedCR = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        LobbyEnterCR = CallResult<LobbyEnter_t>.Create(OnLobbyEnter);
        P2PSessionRequestCb = Callback<P2PSessionRequest_t>.Create(OnP2PSessionRequest);
    }
    
    #endregion

    #region Public Properties
    
    public int maxPlayers = 10;

    public Lobby CurrentLobby { get; private set; }
    public bool isInLobby;
    public bool isInGame;
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Create a new Steam lobby (friends-only by default)
    /// </summary>
    /// <param name="type">Lobby type - <c>public | private | anything -> friends-only</c></param>
    public void CreateLobby(string type = "friend")
    {
        if (!SteamManager.connected)
        {
            CommandConsole.LogError("Steam is not connected");
            return;
        }

        ELobbyType lobbyType = type.ToLower() switch
        {
            "public" => ELobbyType.k_ELobbyTypePublic,
            "private" => ELobbyType.k_ELobbyTypePrivate,
            _ => ELobbyType.k_ELobbyTypeFriendsOnly
        };

        
        SteamAPICall_t h = SteamMatchmaking.CreateLobby(lobbyType, maxPlayers);
        LobbyCreatedCR.Set(h);
        LogManager.SteamClient.Debug("CreateLobby dispatched");
    }
    
    /// <summary>
    /// Join a lobby by its SteamID
    /// </summary>
    /// <param name="lobbyID">SteamID of the lobby</param>
    public void JoinLobby(ulong lobbyID)
    {
        SteamAPICall_t h = SteamMatchmaking.JoinLobby(new CSteamID(lobbyID));
        LobbyEnterCR.Set(h);
    }
    
    /// <summary>
    /// Leave current lobby and shutdown any network stuff(if running)
    /// </summary>
    public void LeaveLobby()
    {
        if (isInGame)
        {
            NetworkServer.Instance.StopServer();
            NetworkClient.Instance.Disconnect();
        }

        CurrentLobby?.LeaveLobby();
        LogManager.SteamClient.Debug("Lobby left!");
        isInLobby = false;
    }
    
    public string[] ListLobbies()
    {
        SteamMatchmaking.RequestLobbyList();
        return new string[1];
    }
    
    #endregion

    #region Steam Callbacks
    
    private void OnLobbyCreated(LobbyCreated_t callback, bool bIOFailure)
    {
        if (bIOFailure || callback.m_eResult != EResult.k_EResultOK)
        {
            LogManager.SteamClient.Debug(bIOFailure ? "Lobby creation failed; I/O Failure" : $"Lobby creation failed; {callback.m_eResult.ToString()}");
            return;
        }

        var steamID = new CSteamID(callback.m_ulSteamIDLobby);
        var lobby = new Lobby(steamID, ELobbyType.k_ELobbyTypeFriendsOnly, maxPlayers);

        lobby.OnMemberJoined += OnMemberJoin;
        lobby.OnMemberLeft += OnMemberLeft;

        CurrentLobby = lobby;
        isInLobby = true;
        
        LogManager.SteamClient.Debug($"Lobby creation succeeded; Lobby ID: {CurrentLobby.GetLobbyID()}");
        
        
        // UiManager.CreatePlayerCardOnJoin(SteamFriends.GetPersonaName(), SteamFriends.GetLargeFriendAvatar(SteamUser.GetSteamID()));

        if (isInGame)
        {
            NetworkServer.Instance.StartServer(0, (ushort)maxPlayers, "steam");
            NetworkClient.Instance.StartClient("127.0.0.1", 0, "steam");
        }
        
        lobby.EnterLobby();
    }
    
    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }
    
    private void OnLobbyEnter(LobbyEnter_t callback, bool bIOFailure)
    {
        LogManager.SteamClient.Debug($"Server running? {NetworkServer.Instance.Server.IsRunning}");

        if (bIOFailure)
        {
            LogManager.SteamClient.Error("Lobby enter failed; I/O Failure");
            return;
        }

        try
        {
            var steamID = new CSteamID(callback.m_ulSteamIDLobby);
            CurrentLobby = new Lobby(steamID, ELobbyType.k_ELobbyTypeFriendsOnly, maxPlayers);
            
            
            CSteamID hostID = SteamMatchmaking.GetLobbyOwner(steamID);
            CSteamID localID = SteamUser.GetSteamID();

            // UiManager.CreatePlayerCardOnJoin(SteamFriends.GetPersonaName(), SteamFriends.GetLargeFriendAvatar(localID));
            
            if (hostID == localID)
            {
                LogManager.SteamClient.Debug("We are the owner of lobby, don't create the client again!");
                return;
            }
            
            SteamNetworking.AcceptP2PSessionWithUser(hostID);
            
            LogManager.SteamClient.Debug(
                $"Entering lobby {CurrentLobby.GetLobbyID()}; hosted by {SteamFriends.GetFriendPersonaName(hostID)}"
            );

            if (isInGame)
                NetworkClient.Instance.StartClient(hostID.ToString(), 0, "steam");
            

            CurrentLobby.EnterLobby();
            CurrentLobby.MemberJoined(localID);
            isInLobby = true;
        }
        catch (Exception ex)
        {
            LogManager.SteamClient.Error("Something went wrong when joining lobby");
            LogManager.SteamClient.Debug(ex.ToString());
        }
    }
    
    #endregion

    #region Event Dispatching
    
    public event Action<LobbyMember> MemberJoined;
    public event Action<LobbyMember> MemberLeft;
    
    private void OnMemberJoin(LobbyMember member)
    {
        MemberJoined?.Invoke(member);
    }
    
    private void OnMemberLeft(LobbyMember member)
    {
        MemberLeft?.Invoke(member);
    }
    
    #endregion

    #region Misc
    
    private void OnP2PSessionRequest(P2PSessionRequest_t req)
    {
        if (!SteamManager.connected) return;
        
        LogManager.SteamClient.Info($"Incoming P2P session request from {req.m_steamIDRemote}");
        SteamNetworking.AcceptP2PSessionWithUser(req.m_steamIDRemote);
    }
    
    #endregion
}