using Riptide;

namespace White_Knuckle_Multiplayer.Networking.Messages;

/// <summary>
/// Message containing data for despawning networked copy (other players)
/// </summary>
public struct DespawnPlayerData : IMessageSerializable
{
    public ushort NetID;

    public DespawnPlayerData(ushort netID)
    {
        NetID = netID;
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