using Steamworks;
using UnityEngine;

namespace White_Knuckle_Multiplayer.Networking.SteamLayer;

public class LobbyMember : ILobbyMember
{
    public string Name { get; }
    public CSteamID SteamID { get; }

    public LobbyMember(CSteamID steamID)
    {
        Name = SteamFriends.GetFriendPersonaName(steamID);
        SteamID = steamID;
    }
    
    public Texture2D GetSteamAvatar()
    {
        int avatarInt = SteamFriends.GetLargeFriendAvatar(SteamID);
        if (avatarInt == -1) return null;

        bool success = SteamUtils.GetImageSize(avatarInt, out uint width, out uint height);
        if (!success) return null;

        byte[] image = new byte[width * height * 4];
        if (!SteamUtils.GetImageRGBA(avatarInt, image, (int)(width * height * 4))) return null;

        Texture2D texture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false, true);
        texture.LoadRawTextureData(image);
        texture.Apply();
        return texture;
    }
}