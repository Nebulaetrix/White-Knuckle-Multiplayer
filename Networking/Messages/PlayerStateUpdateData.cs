using Riptide;
using White_Knuckle_Multiplayer.Managers;

namespace White_Knuckle_Multiplayer.Networking.Messages;

/// <summary>
/// Message for updating player state (lobby/loading/ready/in-game)
/// </summary>
public struct PlayerStateUpdateData : IMessageSerializable
{
    public ushort NetID;
    public PlayerStateManager.PlayerState State;
    public string Username;

    public PlayerStateUpdateData(ushort netID, PlayerStateManager.PlayerState state, string username = null)
    {
        NetID = netID;
        State = state;
        Username = username ?? "";
    }

    public void Serialize(Riptide.Message msg)
    {
        msg.AddUShort(NetID);
        msg.AddInt((int)State);
        msg.AddString(Username);
    }

    public void Deserialize(Riptide.Message msg)
    {
        NetID = msg.GetUShort();
        State = (PlayerStateManager.PlayerState)msg.GetInt();
        Username = msg.GetString();
    }
}