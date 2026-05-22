namespace WhiteKnuckleMP.Networking
{
    /// <summary>
    /// ID's of the messages
    ///
    /// All message ID's below and including 100 are reserved for WKMP
    /// </summary>
    public static class MessageIds
    {
        public const ushort SpawnPlayer = 0;
        public const ushort ClientReady = 1;
        public const ushort PlayerSync = 2;
        public const ushort ItemSync = 3;
    }

}