using System.Collections.Generic;
using UnityEngine;

namespace WhiteKnuckleMP.Networking.Messages;

public struct ItemStateUpdate
{
    public string ItemGuid;
    public Vector3 Position;
    public Quaternion Rotation;
}

public readonly struct ItemBatchSyncMessage : INetworkMessage
{
    public ushort MessageId => MessageIds.ItemSync;
    
    public List<ItemStateUpdate> Updates { get; }

    public ItemBatchSyncMessage(List<ItemStateUpdate> updates)
    {
        Updates = updates;
    }

    public static ItemBatchSyncMessage FromPacket(NetworkPacket packet)
    {
        int count = packet.ReadInt();
        var list = new List<ItemStateUpdate>(count);

        for (var i = 0; i < count; i++)
        {
            list.Add(new ItemStateUpdate
            {
                ItemGuid = packet.ReadString(),
                Position = packet.ReadVector3(),
                Rotation = packet.ReadQuaternion()
            });
        }

        return new ItemBatchSyncMessage(list);
    }
    
    public void WriteTo(NetworkPacket packet)
    {
        packet.Write(Updates.Count);
        foreach (var update in Updates)
        {
            packet.Write(update.ItemGuid)
                .Write(update.Position)
                .Write(update.Rotation);
        }
    }
}