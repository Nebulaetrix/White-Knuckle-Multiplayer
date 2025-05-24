using Riptide;

namespace White_Knuckle_Multiplayer.Networking.Messages;

/// <summary>
/// Message Containing the data for Spawning a networked copy (other players)
/// </summary>
public struct SpawnPlayerData : IMessageSerializable
{
    public ushort NetID;
    public SpawnPlayerData(ushort id)
    {
        NetID = id;
    }

    public void Serialize(Riptide.Message msg)
    {
        msg.AddUShort(NetID);
    }

    public void Deserialize(Riptide.Message msg)
    {
        NetID = msg.GetUShort();
    }
}