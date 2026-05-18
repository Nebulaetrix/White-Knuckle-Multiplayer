namespace WhiteKnuckleMP.Networking
{
    /// <summary>
    /// ID's of the messages
    ///
    /// All message ID's below and including 100 are reserved for WKMP
    /// </summary>
    public enum MessageId : ushort
    {
        SpawnDummy,
        DestroyDummy,
        PlayerSync,
    }

}