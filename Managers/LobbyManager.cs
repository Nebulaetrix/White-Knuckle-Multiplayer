using System;
using Steamworks;
using UnityEngine;
using White_Knuckle_Multiplayer.Networking;

namespace White_Knuckle_Multiplayer.Managers;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            LogManager.SteamClient.Warn("LobbyManager already created, destroying duplicate");
            Destroy(gameObject);
        }
    }
    
    protected CallResult<LobbyCreated_t> LobbyCreatedCR;
    protected Callback<GameLobbyJoinRequested_t> GameLobbyJoinRequestedCR;
    protected CallResult<LobbyEnter_t> LobbyEnterCR;
    protected Callback<P2PSessionRequest_t> P2PSessionRequestCb;

    public int maxPlayers = 10;

    public CSteamID LobbyID { get; private set; }

    private void OnEnable()
    {
        if (!SteamManager.initialized)
        {
            LogManager.SteamClient.Error("Steam is not initialized");
            enabled = false;
            return;
        }

        LogManager.SteamClient.Warn("LobbyManager created");

        LobbyCreatedCR = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);
        GameLobbyJoinRequestedCR = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        LobbyEnterCR = CallResult<LobbyEnter_t>.Create(OnLobbyEnter);
        P2PSessionRequestCb = Callback<P2PSessionRequest_t>.Create(OnP2PSessionRequest);
    }
    

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
        LogManager.SteamClient.Info("CreateLobby dispatched");
    }
    
    private void OnLobbyCreated(LobbyCreated_t callback, bool bIOFailure)
    {
        HandleLobbyCreated(callback, bIOFailure);
    }

    private void HandleLobbyCreated(LobbyCreated_t callback, bool bIOFailure)
    {
        if (bIOFailure || callback.m_eResult != EResult.k_EResultOK)
        {
            LogManager.SteamClient.Error(bIOFailure ? "Lobby creation failed; I/O Failure" : $"Lobby creation failed; {callback.m_eResult.ToString()}");
            return;
        }
        
        LogManager.SteamClient.Error("Callback triggered!");
        
        LobbyID = new CSteamID(callback.m_ulSteamIDLobby);
        LogManager.SteamClient.Info($"Lobby creation succeeded; Lobby ID: {LobbyID}");
        CommandConsole.Log($"Lobby created with ID: {LobbyID}");
        
        NetworkServer.Instance.StartServer(0, (ushort)maxPlayers, "steam");
        NetworkClient.Instance.StartClient("127.0.0.1", 0, "steam");
        
    }
    
    public void JoinLobby(ulong lobbyID)
    {
        SteamAPICall_t h = SteamMatchmaking.JoinLobby(new CSteamID(lobbyID));
        LobbyEnterCR.Set(h);
    }
    
    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }
    
    private void OnLobbyEnter(LobbyEnter_t callback, bool bIOFailure)
    {
        HandleLobbyEnter(callback, bIOFailure);
    }
    
    private void HandleLobbyEnter(LobbyEnter_t callback, bool bIOFailure)
    {
        LogManager.SteamClient.Error("LobbyEnter triggered!");
        
        LogManager.SteamClient.Warn($"Server running? {NetworkServer.Instance.Server.IsRunning}");

        if (bIOFailure)
        {
            LogManager.SteamClient.Error("Lobby enter failed; I/O Failure");
            return;
        }

        try
        {
            LobbyID = new CSteamID(callback.m_ulSteamIDLobby);
            CSteamID hostID = SteamMatchmaking.GetLobbyOwner(LobbyID);
            CSteamID localID = SteamUser.GetSteamID();

            if (hostID == localID)
            {
                LogManager.SteamClient.Info("We are the owner of lobby, don't create the client again!");
                return;
            }
            
            SteamNetworking.AcceptP2PSessionWithUser(hostID);
            
            LogManager.SteamClient.Info(
                $"Entering lobby {LobbyID}; hosted by {SteamFriends.GetFriendPersonaName(hostID)}"
            );

            LogManager.SteamClient.Info("Trying to start client connection to host");
            NetworkClient.Instance.StartClient(hostID.ToString(), 0, "steam");
            LogManager.SteamClient.Info("Post StartClient;");
        }
        catch (Exception ex)
        {
            LogManager.SteamClient.Error("Something went wrong when joining lobby:");
            LogManager.SteamClient.Error(ex.ToString());
        }
        
    }

    private void OnP2PSessionRequest(P2PSessionRequest_t req)
    {
        LogManager.SteamClient.Info($"Incoming P2P session request from {req.m_steamIDRemote}");
        SteamNetworking.AcceptP2PSessionWithUser(req.m_steamIDRemote);
    }
    
    public void LeaveLobby()
    {
        NetworkServer.Instance.StopServer();
        NetworkClient.Instance.Disconnect();
        SteamMatchmaking.LeaveLobby(LobbyID);
        LogManager.SteamClient.Error("Lobby left!");
    }

    public string[] ListLobbies()
    {
        SteamMatchmaking.RequestLobbyList();
        return new string[1];
    }
    
}