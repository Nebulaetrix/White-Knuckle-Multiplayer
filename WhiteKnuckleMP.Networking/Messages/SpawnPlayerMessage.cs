namespace WhiteKnuckleMP.Networking.Messages;

public class SpawnPlayerMessage : INetworkMessage
{
    public ushort MessageId => MessageIds.SpawnPlayer;
    public ushort NetId { get; }
    public string Username { get; }

    public SpawnPlayerMessage(ushort netId, string username)
    {
        NetId = netId;
        Username = username;
    }

    public static SpawnPlayerMessage FromPacket(NetworkPacket packet)
    {
        var netId = packet.ReadUShort();
        var username = packet.ReadString();

        return new SpawnPlayerMessage(netId, username);
    }
    
    public void WriteTo(NetworkPacket packet)
    {
        packet.Write(NetId)
            .Write(Username);
    }
}