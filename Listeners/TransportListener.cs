using System.Collections.Generic;
using Steamworks;
using Unity.Netcode;
using UnityEngine;
using White_Knuckle_Multiplayer.deps;

namespace White_Knuckle_Multiplayer.Listeners;

public class TransportListener(List<SteamId> connectedClients) : MonoBehaviour
{
    private void Start()
    {
        var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as FacepunchTransport;
        if (transport != null)
        {
            Debug.Log("Transport listener subscribed");
            transport.OnTransportEvent += HandleTransportEvent;
        }
    }

    private void OnDestroy()
    {
        var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as FacepunchTransport;
        if (transport != null)
        {
            Debug.Log("Transport listener unsubscribed");
            transport.OnTransportEvent -= HandleTransportEvent;
        }
    }

    private void HandleTransportEvent(NetworkEvent eventType, ulong clientId, System.ArraySegment<byte> payload, float receiveTime)
    {
        if (eventType == NetworkEvent.Connect)
        {
            WkMultiplayer.MultiplayerManager.OnClientConnect(clientId);
            connectedClients.Add(clientId);
            Debug.Log($"Client connected: {clientId}");
        }
        else if (eventType == NetworkEvent.Disconnect)
        {
            WkMultiplayer.MultiplayerManager.OnClientDisconnect(clientId);
            Debug.Log($"Client disconnected: {clientId}");
        }
    }
    
    public bool IsClientConnected(ulong clientId)
    {
        foreach (var id in connectedClients)
        {
            Debug.Log($"ID: {id}, Client ID: {clientId}");
            if (id == clientId)
                return true;
                
        }
        return false;
    }
}