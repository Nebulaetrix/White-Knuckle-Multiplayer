using System;
using UnityEngine;

namespace WhiteKnuckleMP.Networking;

public static class NetworkEvents
{
    public static event Action<ushort, string, ulong> OnPlayerSpawned;
    public static event Action<ushort, Vector3, Quaternion, string, string> OnPlayerMoved;

    /// <summary>
    /// Invoked when a new player is spawned (e.g., joined the host).
    /// </summary>
    /// <param name="netId">The network ID of the player.</param>
    /// <param name="username">The username of the player.</param>
    /// <param name="steamId">The Steam ID of the player.</param>
    internal static void RaisePlayerSpawned(ushort netId, string username, ulong steamId)
    {
        OnPlayerSpawned?.Invoke(netId, username, steamId);
    }

    /// <summary>
    /// Invoked every time a player moves.
    /// </summary>
    /// <param name="netId">The network ID of the player.</param>
    /// <param name="pos">The new position of the player.</param>
    /// <param name="rot">The new rotation of the player.</param>
    /// <param name="leftItem">The item being held in the left hand.</param>
    /// <param name="rightItem">The item being held in the right hand.</param>
    internal static void RaisePlayerMoved(ushort netId, Vector3 pos, Quaternion rot, string leftItem, string rightItem)
    {
        OnPlayerMoved?.Invoke(netId, pos, rot, leftItem, rightItem);
    }
}