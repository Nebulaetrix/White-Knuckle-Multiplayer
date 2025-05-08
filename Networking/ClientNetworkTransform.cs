using System;
using Unity.Netcode.Components;

namespace White_Knuckle_Multiplayer.Networking;

public class ClientNetworkTransform : NetworkTransform
{
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}