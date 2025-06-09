using System;
using Steamworks;

namespace White_Knuckle_Multiplayer.Networking.SteamLayer;

public class Lobby : ILobby
{
    public string Name => SteamMatchmaking.GetLobbyData(lobbyID, "name");
    public int CurrentMembers => SteamMatchmaking.GetNumLobbyMembers(lobbyID);
    public int MaxMembers { get; private set; }
    public bool IsModded => SteamMatchmaking.GetLobbyData(lobbyID, "modded") == "1";
    public bool InviteOnly => lobbyType == ELobbyType.k_ELobbyTypePrivate;

    private readonly CSteamID lobbyID;
    private readonly ELobbyType lobbyType;

    private bool IsLocal;
    
    public event Action<LobbyMember> OnMemberJoined;
    public event Action<LobbyMember> OnMemberLeft;
    
    public Lobby(CSteamID id, ELobbyType type, int maxMembers, bool isLocal = false)
    {
        lobbyID = id;
        lobbyType = type;
        MaxMembers = maxMembers;
        IsLocal = isLocal;
    }
    
    public void EnterLobby()
    {
        for (var i = 0; i < CurrentMembers; i++)
        {
            var memberID = SteamMatchmaking.GetLobbyMemberByIndex(lobbyID, i);
            OnMemberJoined?.Invoke(new LobbyMember(memberID));
        }
    }

    public void LeaveLobby()
    {
        SteamMatchmaking.LeaveLobby(lobbyID);
    }

    public void MemberJoined(CSteamID memberID)
    {
        OnMemberJoined?.Invoke(new LobbyMember(memberID));
    }

    public void MemberLeft(CSteamID memberID)
    {
        OnMemberLeft?.Invoke(new LobbyMember(memberID));
    }

    public CSteamID GetLobbyID() => lobbyID;
    
    public bool GetIsLocal() => IsLocal;
}