using UnityEngine;
using WhiteKnuckleMP.Framework.Visuals;

namespace WhiteKnuckleMP.Framework.Controllers.Player;

public class PlayerController : MonoBehaviour
{
    // Identity Data
    public ushort NetID { get; private set; }
    public ulong SteamID { get; private set; }
    public string Username { get; private set; } = "Unknown";
    public bool IsLocal { get; private set; }
    
    // Component Links
    public PlayerStateSender? Sender { get; private set; }
    public PlayerStateReceiver? Receiver { get; private set; }
    public PlayerEquipmentManager? Equipment { get; private set; }

    // Hand Data
    public GameObject LeftHand { get; private set; }
    public GameObject RightHand { get; private set; }

    /// <summary>
    /// Called by the PlayerManager when this player is spawned into the world.
    /// </summary>
    /// <param name="netId">The network ID of the player.</param>
    /// <param name="steamId">The Steam ID of the player.</param>
    /// <param name="username">The username of the player.</param>
    /// <param name="isLocal">True if this is the local player, false otherwise.</param>
    /// <param name="leftHand">The GameObject of the left hand</param>
    /// <param name="rightHand">The GameObject of the right hand</param>
    public void Initialize(ushort netId, ulong steamId, string username, bool isLocal, GameObject leftHand, GameObject rightHand)
    {
        NetID = netId;
        SteamID = steamId;
        Username = username;
        IsLocal = isLocal;
        LeftHand = leftHand;
        RightHand = rightHand;

        gameObject.name = isLocal ? $"LocalPlayer_NetID_{netId}" : $"RemotePlayer_{username}_NetID_{netId}";

        // Visual Item Manager
        Equipment = GetComponent<PlayerEquipmentManager>();
        if (Equipment == null) 
        { 
            Equipment = gameObject.AddComponent<PlayerEquipmentManager>(); 
        }

        if (IsLocal)
        {
            // Local Player gets sender, they're NOT a receiver
            Sender = gameObject.GetComponent<PlayerStateSender>() ?? gameObject.AddComponent<PlayerStateSender>();
            // Initialize it with NetID
            Sender.Initialize(netId, leftHand, rightHand);
        }
        else
        {
            // Remote Player gets Receiver, as they do not need to send anything
            Receiver = gameObject.GetComponent<PlayerStateReceiver>() ?? gameObject.AddComponent<PlayerStateReceiver>();            
            // Initialize it with hand references and etc.
            // TODO: Add the hand sync
            Receiver.Initialize(netId, leftHand, rightHand);
            
            Equipment.BindToReceiver(Receiver);
        }
    }
}