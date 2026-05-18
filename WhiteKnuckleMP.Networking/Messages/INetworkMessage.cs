namespace WhiteKnuckleMP.Networking.Messages;

public interface INetworkMessage
{
    /// <summary>
    /// The unique Riptide/Mod network ID associated with this type of message.
    /// </summary>
    ushort MessageId { get; }

    /// <summary>
    /// Packs the fields of this object into the binary packet sequence.
    /// </summary>
    void WriteTo(NetworkPacket packet);
}