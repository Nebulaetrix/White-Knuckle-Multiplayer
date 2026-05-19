using System;
using System.Collections.Generic;
using Riptide;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using WhiteKnuckleMP.Framework.Controllers;
using WhiteKnuckleMP.Framework.Controllers.Player;
using WhiteKnuckleMP.Networking;
using WhiteKnuckleMP.Networking.Messages;
using WhiteKnuckleMP.Utils;

namespace WhiteKnuckleMP.Framework.Managers;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; } = null!;
    public Dictionary<ushort, PlayerController> ActivePlayers = new();

    private GameObject _hollowRemotePrefab = null!;
    private bool _hasCreatedPrefab;
    
    private void Awake()
    {
        Instance = this;

        StateManager.OnStateChanged += HandleStateChanged;
    }

    private void OnDestroy()
    {
        StateManager.OnStateChanged -= HandleStateChanged;
    }

    public void CreatePlayerPrefab(GameObject localPlayer)
    {
        if (_hasCreatedPrefab) return;
        
        LogManager.Framework.Info("Capturing local player to create a hollow remote prefab clone...");

        _hollowRemotePrefab = Instantiate(localPlayer, null);
        _hollowRemotePrefab.name = "HollowRemotePlayer_Prefab";
        _hollowRemotePrefab.SetActive(false);
        DontDestroyOnLoad(_hollowRemotePrefab);

        if (_hollowRemotePrefab.TryGetComponent(out PostProcessLayer postProcessLayer))
            Destroy(postProcessLayer);
        
        if (_hollowRemotePrefab.TryGetComponent(out ViewSway viewSway))
            Destroy(viewSway);
        
        if (_hollowRemotePrefab.TryGetComponent(out CRTEffect crtEffect))
            Destroy(crtEffect);
        
        var cameras = _hollowRemotePrefab.GetComponentsInChildren<Camera>(true);
        foreach (var cam in cameras) Destroy(cam);

        var listeners = _hollowRemotePrefab.GetComponentsInChildren<AudioListener>(true);
        foreach (var listener in listeners) Destroy(listener);
        
        if (_hollowRemotePrefab.TryGetComponent(out ENT_Player playerScript))
            Destroy(playerScript);

        _hollowRemotePrefab.AddComponent<PlayerController>();
        
        _hasCreatedPrefab = true;
        LogManager.Framework.Info("Hollow remote player prefab successfully created!");
    }
    
    private void HandleStateChanged(StateManager.State oldState, StateManager.State newState)
    {
        if (newState == StateManager.State.Disconnected)
        {
            foreach (var player in ActivePlayers.Values)
            {
                if (player != null) Destroy(player.gameObject);
            }
            ActivePlayers.Clear();
            LogManager.Framework.Info("Cleared all networked player due to disconnection.");
        }
    }

    public void HandleNetworkSpawn(ushort netId, string username, ulong steamId, bool isLocal)
    {
        if (ActivePlayers.ContainsKey(netId)) return;

        PlayerController controller;
        
        if (isLocal)
        {
            var localGo = GameObject.Find("CL_Player");

            controller = localGo.GetComponent<PlayerController>();
            
            if (controller == null)
            {
                controller = localGo.AddComponent<PlayerController>();
                controller.Initialize(netId, steamId, username, isLocal);
            }
            
            // TODO: Fix this
            // CreatePlayerPrefab(localGo);
        }
        else
        {
            var remoteGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            remoteGo.name = $"RemotePlayer_{username}_{netId}";
            DontDestroyOnLoad(remoteGo);

            if (remoteGo.TryGetComponent(out Collider capsuleCollider))
            {
                capsuleCollider.isTrigger = true;
            }

            if (remoteGo.TryGetComponent(out Renderer renderer))
            {
                renderer.material.color = Color.cyan; // Fren
            }

            controller = remoteGo.GetComponent<PlayerController>();
            
            if (controller == null)
            {
                controller = remoteGo.AddComponent<PlayerController>();
                controller.Initialize(netId, steamId, username, isLocal);
            }
        }
        
        ActivePlayers.Add(netId, controller);
    }

    #region Message Handlers
    
    [MessageHandler(MessageIds.PlayerSync)]
    public static void HandlePlayerSync(Riptide.Message message)
    {
        using var packet = new NetworkPacket(message);
        
        // Read data from packet
        PlayerSyncMessage data = PlayerSyncMessage.FromPacket(packet);
            
        ushort netId = data.NetId;
        Vector3 pos = data.Position;
        Quaternion rot = data.Rotation;
        string leftItem = data.LeftItemName;
        string rightItem = data.RightItemName;

        if (Instance.ActivePlayers.TryGetValue(netId, out var player))
        {
            if (!player.IsLocal && player.Receiver != null)
            {
                player.Receiver.ApplyNetworkData(pos, rot, leftItem, rightItem);
            }
        }
    }

    [MessageHandler(MessageIds.ClientReady)]
    public static void HandleClientReady(ushort fromClientId, Riptide.Message message)
    {
        if (!NetworkManager.Instance.IsServer) return;
        
        LogManager.Server.Info($"Client {fromClientId} reports ready. Syncing players...");
        
        var newPlayerInfo = LobbyManager.Instance.ConnectedLobbyPlayers.Find(p => p.NetId == fromClientId);
        string username = string.IsNullOrEmpty(newPlayerInfo.Username) ? $"Player {fromClientId}" : newPlayerInfo.Username;
        
        // Tell Everyone including the new client to spawn this new client
        var spawnNewGuyMsg = new SpawnPlayerMessage(fromClientId, newPlayerInfo.Username);
        using (var p1 = new NetworkPacket(spawnNewGuyMsg.MessageId, Riptide.MessageSendMode.Reliable))
        {
            spawnNewGuyMsg.WriteTo(p1);
            NetworkManager.Instance.Server.SendToAll(p1.RawMessage);
        }
        
        // Tell only the new client to spawn all the players who were already spawned
        foreach (var existingPlayer in Instance.ActivePlayers.Values)
        {
            if (existingPlayer.NetID == fromClientId) continue; // Skip sending themselves

            var spawnExistingMsg = new SpawnPlayerMessage(existingPlayer.NetID, existingPlayer.Username);
            using (var p2 = new NetworkPacket(spawnExistingMsg.MessageId, Riptide.MessageSendMode.Reliable))
            {
                spawnExistingMsg.WriteTo(p2);
                NetworkManager.Instance.Server.Send(p2.RawMessage, fromClientId);
            }
        }
    }

    [MessageHandler(MessageIds.SpawnPlayer)]
    public static void HandleSpawnPlayer(Riptide.Message message)
    {
        using (var packet = new NetworkPacket(message))
        {
            SpawnPlayerMessage data = SpawnPlayerMessage.FromPacket(packet);

            var isLocal = (data.NetId == NetworkManager.LocalClientId);
            
            LogManager.Framework.Info($"Spawning player for: {data.Username} (Local={isLocal})");
            
            Instance.HandleNetworkSpawn(data.NetId, data.Username, 0, isLocal);
        }
    }
    
    #endregion
}