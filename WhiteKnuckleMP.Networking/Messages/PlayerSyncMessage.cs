using UnityEngine;

namespace WhiteKnuckleMP.Networking.Messages;

public readonly struct PlayerSyncMessage : INetworkMessage
{
    public ushort MessageId => (ushort)Networking.MessageId.PlayerSync;

    public ushort NetId { get; }
    public Vector3 Position { get; }
    public Quaternion Rotation { get; }
    public string LeftItemName { get; }
    public string RightItemName { get; }

    public PlayerSyncMessage(ushort netId, Vector3 position, Quaternion rotation, string leftItemName,
        string rightItemName)
    {
        NetId = netId;
        Position = position;
        Rotation = rotation;
        LeftItemName = leftItemName;
        RightItemName = rightItemName;
    }
    
    public static PlayerSyncMessage FromPacket(NetworkPacket packet)
    {
        ushort id = packet.ReadUShort();
        Vector3 position = packet.ReadVector3();
        Quaternion rotation = packet.ReadQuaternion();
        string leftItemName = packet.ReadString();
        string rightItemName = packet.ReadString();

        return new PlayerSyncMessage(id, position, rotation, leftItemName, rightItemName);
    }
    
    public void WriteTo(NetworkPacket packet)
    {
        packet.Write(NetId)
            .Write(Position)
            .Write(Rotation)
            .Write(LeftItemName)
            .Write(RightItemName);
    }
}