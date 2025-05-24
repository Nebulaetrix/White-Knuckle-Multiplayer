using Riptide;
using White_Knuckle_Multiplayer.Networking.Serializers;

namespace White_Knuckle_Multiplayer.Networking.Messages;

/// <summary>
/// The Message that gets sent when player joins
/// </summary>
public struct JoinRequestData : IMessageSerializable
{
    public string Username;
    public string Version;
    public string[] ModList;
        
        
    public JoinRequestData(string username, string version, string[] modList)
    {
        Username = username;
        Version = version;
        ModList = modList;
    }
        
    public void Serialize(Riptide.Message message)
    {
        message.AddString(Username);
        message.AddString(Version);
        message.AddList(ModList, (m, s) => m.AddString(s));
    }

    public void Deserialize(Riptide.Message message)
    {
        Username = message.GetString();
        Version = message.GetString();
        ModList = message.GetList(m => m.GetString()).ToArray();
    }
}