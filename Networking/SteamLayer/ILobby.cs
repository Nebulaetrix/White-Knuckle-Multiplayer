using System;

namespace White_Knuckle_Multiplayer.Networking.SteamLayer;

public interface ILobby
{
    string Name { get; }
    int CurrentMembers { get; }
    int MaxMembers { get; }
    bool IsModded { get; }
    bool InviteOnly { get; }

    void EnterLobby();
    void LeaveLobby();

    event Action<LobbyMember> OnMemberJoined;
    event Action<LobbyMember> OnMemberLeft;
}