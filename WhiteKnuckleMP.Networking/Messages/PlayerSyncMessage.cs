using UnityEngine;

namespace WhiteKnuckleMP.Networking.Messages;

public readonly struct PlayerSyncMessage : INetworkMessage
{
    public ushort MessageId => MessageIds.PlayerSync;

    public ushort NetId { get; }
    public Vector3 Position { get; }
    public Quaternion Rotation { get; }
    public string LeftItemName { get; }
    public string RightItemName { get; }

    public Vector3 LeftHandPosition { get; }
    public Vector3 RightHandPosition { get; }
    
    public PlayerSyncMessage(ushort netId, Vector3 position, Quaternion rotation, string leftItemName,
        string rightItemName, Vector3 leftHandPosition, Vector3 rightHandPosition)
    {
        NetId = netId;
        Position = position;
        Rotation = rotation;
        LeftItemName = leftItemName;
        RightItemName = rightItemName;
        LeftHandPosition = leftHandPosition;
        RightHandPosition = rightHandPosition;
    }
    
    public static PlayerSyncMessage FromPacket(NetworkPacket packet)
    {
        ushort id = packet.ReadUShort();
        Vector3 position = packet.ReadVector3();
        Quaternion rotation = packet.ReadQuaternion();
        string leftItemName = packet.ReadString();
        string rightItemName = packet.ReadString();
        Vector3 leftHandPosition = packet.ReadVector3();
        Vector3 rightHandPosition = packet.ReadVector3();

        return new PlayerSyncMessage(id, position, rotation, leftItemName, rightItemName, leftHandPosition, rightHandPosition);
    }
    
    public void WriteTo(NetworkPacket packet)
    {
        packet.Write(NetId)
            .Write(Position)
            .Write(Rotation)
            .Write(LeftItemName)
            .Write(RightItemName)
            .Write(LeftHandPosition)
            .Write(RightHandPosition);
    }
}