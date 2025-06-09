using Steamworks;
using UnityEngine;

namespace White_Knuckle_Multiplayer.Networking.SteamLayer;

public interface ILobbyMember
{
    string Name { get; }
    CSteamID SteamID { get; }

    Texture2D GetSteamAvatar();
}