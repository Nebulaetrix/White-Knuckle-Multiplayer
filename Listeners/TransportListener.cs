using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using White_Knuckle_Multiplayer.deps;
using White_Knuckle_Multiplayer.Networking;

namespace White_Knuckle_Multiplayer.Listeners;

public class TransportListener : MonoBehaviour
{
    internal readonly List<ulong> ConnectedClientIds = [];
    private FacepunchTransport transport;
    private void Start()
    {
        transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as FacepunchTransport;
        if (transport != null)
        {
            Debug.Log("Transport listener subscribed");
            transport.OnTransportEvent += HandleTransportEvent;
        }
    }

    private void OnDestroy()
    {
        if (transport != null)
        {
            Debug.Log("Transport listener unsubscribed");
            transport.OnTransportEvent -= HandleTransportEvent;
        }
    }

    private void HandleTransportEvent(NetworkEvent eventType, ulong clientId, System.ArraySegment<byte> payload, float receiveTime)
    {
        if (WkMultiplayer.GameManager == null)
        {
            Debug.LogError("[TransportListener] MultiplayerManager is NULL!");
            return;
        }
        
        if (eventType == NetworkEvent.Connect)
        {   
            ConnectedClientIds.Add(clientId);
            WkMultiplayer.GameManager.OnClientConnect(clientId);
            
        }
        else if (eventType == NetworkEvent.Disconnect)
        {
            ConnectedClientIds.Remove(clientId);
            WkMultiplayer.GameManager.OnClientDisconnect(clientId);
            
        }
    }
    
    public bool IsClientConnected(ulong clientId)
    {
        Debug.Log($"Connected clients: {ConnectedClientIds.Count}");
        if (ConnectedClientIds.Count > 0)
            return true;
        
        return false;
    }
}