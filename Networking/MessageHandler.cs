using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Riptide;
using UnityEngine.Rendering.PostProcessing;
using White_Knuckle_Multiplayer.Managers;
using Object = UnityEngine.Object;
using White_Knuckle_Multiplayer.Networking.Controllers;
using White_Knuckle_Multiplayer.Networking.Messages;
using White_Knuckle_Multiplayer.Utils;

namespace White_Knuckle_Multiplayer.Networking
{
    /// <summary>
    /// Defines the Unique <see cref="ushort"/> MessageID,
    /// for the client/server to know what the message contains
    /// </summary>
    public enum MessageID : ushort
    {
        Unknown = 0,
        JoinRequest = 1,
        ConnectionError = 2,
        SteamAuthError = 3,
        PlayerDataSync = 4,
        SpawnPlayer = 5,
        DespawnPlayer = 6,
        SceneChange = 7, // This one will propably be replaced
        PlayerStateUpdate = 8,
    }
    
    /// <summary>
    /// GroupID, that defines what is server and what client
    /// </summary>
    public enum GroupID : byte
    {
        Server = 0,
        Client = 1,
    }

    // DATA STRUCTS //
    // Responsible for containing the data within the message
    // Can only handle basic and custom types
    // for custom types refer to Serializers/MessageExtensions.cs
    //
    // Messages are now in Messages/
    
    // Message for handling Scene Changes
    // Replaced already by Nebby
    public struct SceneChangeData : IMessageSerializable
    {
        public string SceneName;
        public SceneChangeData(string scene)
        {
            SceneName = scene;
        }

        public void Serialize(Riptide.Message msg)
        {
            msg.AddString(SceneName);
        }

        public void Deserialize(Riptide.Message msg)
        {
            SceneName = msg.GetString();
        }
    }

    
    // MESSAGE ROUTER && SPAWN MANAGER //

    
    /// <summary>
    /// Handles Incoming Message from the network,
    /// only one instance exists always
    /// </summary>
    public partial class MessageHandler : MonoBehaviour
    {
        public static MessageHandler Instance { get; private set; }

        private string playerPrefabName = "CL_Player";

        // Keeping Track of NetID -> GameObject
        public readonly Dictionary<ushort, GameObject> _players = new();

        public readonly List<ushort> PendingSpawns = new();
        private Coroutine _pendingSpawnCheck;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Subscribe to player state changes
            if (PlayerStateManager.Instance != null)
            {
                PlayerStateManager.Instance.OnLocalPlayerStateChanged += OnLocalPlayerStateChanged;
                PlayerStateManager.Instance.OnPlayerStateChanged += OnPlayerStateChanged;
            }
            
            // Start checking for pending spawns
            _pendingSpawnCheck = StartCoroutine(CheckPendingSpawns());
        }

        private void OnLocalPlayerStateChanged(PlayerStateManager.PlayerState newState)
        {
            if (newState == PlayerStateManager.PlayerState.InGame)
            {
                // Process any pending spawns now that we're ready
                ProcessPendingSpawns();
            }
        }

        private void OnPlayerStateChanged(ushort netID, PlayerStateManager.PlayerState newState)
        {
            if (newState == PlayerStateManager.PlayerState.InGame && PendingSpawns.Contains(netID))
            {
                LogManager.Net.Info($"Processing pending spawns for player {netID}");
                SpawnPlayer_Internal(netID);
                PendingSpawns.Remove(netID);
            }
        }

        private IEnumerator CheckPendingSpawns()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f); // Check every second

                if (PlayerStateManager.Instance?.CanSpawnPlayers == true)
                {
                    ProcessPendingSpawns();
                }
            }
        }

        private void ProcessPendingSpawns()
        {
            for (int i = PendingSpawns.Count - 1; i >= 0; i--)
            {
                ushort netID = PendingSpawns[i];
                if (PlayerStateManager.Instance.ShouldSpawnPlayer(netID))
                {
                    LogManager.Net.Info($"Processing pending spawn for player {netID}");
                    SpawnPlayer_Internal(netID);
                    PendingSpawns.Remove(netID);
                }
            }
        }

        private void OnDestroy()
        {
            if (_pendingSpawnCheck != null)
            {
                StopCoroutine(_pendingSpawnCheck);
            }

            if (PlayerStateManager.Instance != null)
            {
                PlayerStateManager.Instance.OnLocalPlayerStateChanged -= OnLocalPlayerStateChanged;
                PlayerStateManager.Instance.OnPlayerStateChanged -= OnPlayerStateChanged;
            }
        }

        // HANDLERS //
        // Handle incoming messages from the network
        //
        // Server: MessageHandler.Server.cs
        // Client: MessageHandler.Client.cs

        
        // INTERNAL (SHARED) LOGIC //

        
        // Spawns the player, with suffix: _NetworkID
        // If this is triggered for local player, adds the PlayerNetworkController script
        private void SpawnPlayer_Internal(ushort netID)
        {
            // If not a valid netID, ignore it
            if (netID == 0)
            {
                LogManager.Net.Warn("SpawnPlayer_Internal called with netID 0; ignoring.");
                return;
            }
            
            if (_players.ContainsKey(netID)) return;
            LogManager.Net.Info($"Starting SpawnPlayer on ID {netID}");

            // Instantiate new player object
            // TODO: Move this to a separate file

            // Finding the original playerPrefab
            GameObject player = GameObject.Find(playerPrefabName);
            if (player == null)
            {
                LogManager.Net.Error("player was not found, can't spawn one");
                return;
            }
            LogManager.Net.Info("Player found, attempting to spawn...");

            if (netID == NetworkClient.Instance.Client.Id && NetworkClient.Instance.Client != null)
            {
                // Player is local, attach the NetworkControllers scripts, so it can properly synchronize to other clients
                LogManager.Net.Info($"Attaching network controllers to local player {netID}");
                AttachControllers(player, netID);
                return;
            }

            // Player is networked(not local), spawn him
            if (!_players.ContainsKey(netID) && transform.Find($"{playerPrefabName}_{netID}") == null)
            {
                LogManager.Net.Info($"Instantiating network clone for ID {netID}");
                var networkClone = InstantiatePlayerPrefab(netID);
                AttachControllers(networkClone, netID);
                networkClone.SetActive(true);
            
                // Add Networked clone to players
                _players.Add(netID, networkClone);
            }
            
        }

        // Handles Despawn of the networked copy
        public void DespawnPlayer(ushort netID)
        {
            // If the networked copy exists, destroy it
            if (!_players.TryGetValue(netID, out var go)) return;
            Destroy(go);
            _players.Remove(netID);
            LogManager.Net.Info($"Despawned player with ID {netID}");
        }

        // HELPER FUNCTIONS //
        // TODO: Make An intermediate Script containing all these

        // Function to attach NetworkControllers
        private static void AttachControllers(GameObject go, ushort netID)
        {
            if (go.GetComponent<PlayerNetworkController>() != null)
            {
                LogManager.Net.Warn($"NetID {netID} already has Controllers attached");
                return;
            }
            
            go.AddComponent<PlayerNetworkController>();
            
            LogManager.Net.Info($"Controllers attached for ID {netID}");
        }
        
        // Instantiates the networked copy
        private GameObject InstantiatePlayerPrefab(ushort netID)
        {
            LogManager.Net.Info($"Initializing network clone for {netID}");
            GameObject prefabFromBundle = AssetBundleLoader.PlayerPrefab;
            var prefab = Object.Instantiate(prefabFromBundle);
            prefab.name = $"{playerPrefabName}_{netID}";
            prefab.transform.SetParent(transform.Find("Players").transform);
            prefab.SetActive(false);
            LogManager.Net.Info($"Instantiated Networked Player Prefab for ID {netID}");

            return prefab;
        }
    }
}