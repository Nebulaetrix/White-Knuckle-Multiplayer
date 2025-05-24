using System.Collections.Generic;
using UnityEngine;
using Riptide;
using UnityEngine.Rendering.PostProcessing;
using Object = UnityEngine.Object;
using White_Knuckle_Multiplayer.Networking.Controllers;
using White_Knuckle_Multiplayer.Networking.Messages;

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

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
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
                var networkClone = InstantiatePlayerPrefab(player, netID);
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
        private GameObject InstantiatePlayerPrefab(GameObject player, ushort netID)
        {
            LogManager.Net.Info($"Initializing network clone for {netID}");
            GameObject capsule;
            var prefab = Object.Instantiate(player);
            Object.DontDestroyOnLoad(prefab);
            
            DestroyUnwantedComponents(prefab);

            prefab.name = $"{playerPrefabName}_{netID}";
            prefab.transform.SetParent(transform.Find("Players").transform);
            prefab.SetActive(false);
            
            capsule = prefab.transform.Find("Capsule").gameObject;
            capsule.layer = LayerMask.NameToLayer("Player");
            LogManager.Net.Info($"Instantiated Networked Player Prefab for ID {netID}");

            return prefab;
        }

        private static void DestroyUnwantedComponents(GameObject prefab)
        {
            // Object.Destroy(prefab.GetComponent<CharacterController>());
            Object.Destroy(prefab.GetComponent<Inventory>());
            Object.Destroy(prefab.GetComponent<MonoBehaviour>());

            var unwantedCameraComponents = new[]
            {
            "Main Cam Root", "Main Cam Root/Main Camera Shake Root/Main Camera",
            "Main Cam Root/Main Camera Shake Root/Main Camera/Inventory Camera"
        };
            foreach (var path in unwantedCameraComponents)
            {
                var camObject = prefab.transform.Find(path);
                if (camObject != null)
                {
                    Object.Destroy(camObject.GetComponent<CRTEffect>());
                    Object.Destroy(camObject.GetComponent<PostProcessVolume>());
                    Object.Destroy(camObject.GetComponent<PostProcessLayer>());
                    Object.Destroy(camObject.GetComponent<Camera>());
                    Object.Destroy(camObject.GetComponent<FX_CameraShaderController>());
                }
            }

            var unwantedGameObjects = new[]
            {
                "Main Cam Root/Main Camera Shake Root/Main Camera/Inventory Camera/Inventory",
                "Main Cam Root/Main Camera Shake Root/Main Camera/Inventory Camera/InventoryBagCamera",
                "Main Cam Target", "Particle System", "Wind Sound", "Fatigue Sound",
                "Main Cam Target", "Particle System", "Wind Sound", "Fatigue Sound",
                "CorruptionSurround", "Aim Circle", "Fake Handholds", "FXCam", "EffectRoot", "Death Sound"
            };

            foreach (var path in unwantedGameObjects)
            {
                var unwantedObject = prefab.transform.Find(path);
                if (unwantedObject != null) Object.Destroy(unwantedObject.gameObject);
            }

            Destroy(prefab.transform
                .Find(
                    "Main Cam Root/Main Camera Shake Root/Main Camera/Inventory Camera/Inventory-Root/Right_Hand_Target/Item_Hand_Right")
                .GetComponent<ViewSway>());
            Destroy(prefab.transform
                .Find(
                    "Main Cam Root/Main Camera Shake Root/Main Camera/Inventory Camera/Inventory-Root/Left_Hand_Target/Item_Hand_Left")
                .GetComponent<ViewSway>());
        }
    }
}