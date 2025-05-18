using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Riptide;
using UnityEngine.Rendering.PostProcessing;
using White_Knuckle_Multiplayer.Networking.Controllers;
using Object = UnityEngine.Object;

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
    // Responsible for Containing the data within the message
    // Can only handle basic types, NOT VECTOR3
    // Advanced types must be deconstructed to basic types
    
    /// <summary>
    /// The Message that gets sent when player joins
    /// </summary>
    public struct JoinRequestData : IMessageSerializable
    {
        public string Username;
        public string Version;
        
        
        public JoinRequestData(string username, string version)
        {
            Username = username;
            Version = version;
        }
        
        public void Serialize(Riptide.Message message)
        {
            message.AddString(Username);
            message.AddString(Version);
        }

        public void Deserialize(Riptide.Message message)
        {
            Username = message.GetString();
            Version = message.GetString();
        }
    }

    // Message containing all the data that the networked copies(other player) need to set on their end
    public struct PlayerData : IMessageSerializable
    {
        public ushort NetID;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 HandLeftPosition;
        public Vector3 HandRightPosition;
        public string HandLeftState;
        public string HandRightState;
        public Color HandLeftColor;  
        public Color HandRightColor; 

        public PlayerData(ushort playerID, Vector3 position, Quaternion rotation,
            Vector3 handLeftPosition, Vector3 handRightPosition, string handLeftState, string handRightState, Color handLeftColor, Color handRightColor)
        {
            NetID = playerID;
            Position = position;
            Rotation = rotation;
            HandLeftPosition = handLeftPosition;
            HandRightPosition = handRightPosition;
            HandLeftState = handLeftState;
            HandRightState = handRightState;
            HandLeftColor = handLeftColor;
            HandRightColor = handRightColor;
            
        }

        // Data needs to be deconstructed to its core components,
        // So it can be sent over the network in this message
        public void Serialize(Riptide.Message message)
        {
            message.AddUShort(NetID);

            // Player Position - Vector3
            message.AddFloat(Position.x);
            message.AddFloat(Position.y);
            message.AddFloat(Position.z);
            
            // Player Rotation - Quaternion
            message.AddFloat(Rotation.x);
            message.AddFloat(Rotation.y);
            message.AddFloat(Rotation.z);
            message.AddFloat(Rotation.w);
            
            // Left Hand - Vector3
            message.AddFloat(HandLeftPosition.x);
            message.AddFloat(HandLeftPosition.y);
            message.AddFloat(HandLeftPosition.z);
            
            // Right Hand - Vector3
            message.AddFloat(HandRightPosition.x);
            message.AddFloat(HandRightPosition.y);
            message.AddFloat(HandRightPosition.z);
            
            // Hand States - String
            message.AddString(HandLeftState);
            message.AddString(HandRightState);
                        
            // Vector4, Left Hand Color
            message.AddFloat(HandLeftColor.r);
            message.AddFloat(HandLeftColor.g);
            message.AddFloat(HandLeftColor.b);
            message.AddFloat(HandLeftColor.a);

            
            // Vector4, Right Hand Color
            message.AddFloat(HandRightColor.r);
            message.AddFloat(HandRightColor.g);
            message.AddFloat(HandRightColor.b);
            message.AddFloat(HandRightColor.a);
        }

        // Reconstruct the message from basic types to advanced ones
        // So that this struct can be used normally in code
        // Without doing anything extra to get the correct type
        public void Deserialize(Riptide.Message message)
        {
            NetID = message.GetUShort();

            // Player Position - Vector3
            float x = message.GetFloat();
            float y = message.GetFloat();
            float z = message.GetFloat();
            Position = new Vector3(x, y, z);

            // Player rotation - Quaternion
            float rx = message.GetFloat();
            float ry = message.GetFloat();
            float rz = message.GetFloat();
            float rw = message.GetFloat();
            Rotation = new Quaternion(rx, ry, rz, rw);
            
            // Left Hand - Vector3
            float handLeftX = message.GetFloat();
            float handLeftY = message.GetFloat();
            float handLeftZ = message.GetFloat();
            HandLeftPosition = new Vector3(handLeftX, handLeftY, handLeftZ);
            
            // Right Hand - Vector3
            float handRightX = message.GetFloat();
            float handRightY = message.GetFloat();
            float handRightZ = message.GetFloat();
            HandRightPosition = new Vector3(handRightX, handRightY, handRightZ);
            
            // Hand States - String
            HandLeftState = message.GetString();
            HandRightState = message.GetString();
            
            // Hand Color - Vector4
            float rL = message.GetFloat();
            float gL = message.GetFloat();
            float bL = message.GetFloat();
            float aL = message.GetFloat();
            HandLeftColor = new Color(rL, gL, bL, aL);

            // Hand Color - Vector4
            float rR = message.GetFloat();
            float gR = message.GetFloat();
            float bR = message.GetFloat();
            float aR = message.GetFloat();
            HandRightColor = new Color(rR, gR, bR, aR);
        }
    }

    // Message Containing the data for Spawning a networked copy (other players)
    public struct SpawnPlayerData : IMessageSerializable
    {
        public ushort NetID;
        public SpawnPlayerData(ushort id)
        {
            NetID = id;
        }

        public void Serialize(Riptide.Message msg)
        {
            msg.AddUShort(NetID);
        }

        public void Deserialize(Riptide.Message msg)
        {
            NetID = msg.GetUShort();
        }
    }
    
    // Message for handling Scene Changes
    // TODO: Replace with actual level synchronization
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

    // Message containing data for despawning networked copy (other players)
    public struct DespawnPlayerData : IMessageSerializable
    {
        public ushort NetID;

        public DespawnPlayerData(ushort netID)
        {
            NetID = netID;
        }
        
        public void Serialize(Riptide.Message msg)
        {
            msg.AddUShort(NetID);
        }

        public void Deserialize(Riptide.Message msg)
        {
            NetID = msg.GetUShort();
        }
    }

    // (STATIC) MESSAGE SENDER //

    /// <summary>
    /// Static class for sending Messages over the network
    /// </summary>
    public static class MessageSender
    {
        // Sending Request on join, server authorizes and keeps track of this
        public static void SendJoinRequest(JoinRequestData data)
        {
            Riptide.Message msg = Riptide.Message.Create(MessageSendMode.Reliable, (ushort)MessageID.JoinRequest);
            
            msg.AddSerializable(data);

            NetworkClient.Instance.Client.Send(msg);
        }

        // Sending Player Object Data
        public static void SendPlayerData(PlayerData data)
        {
            Riptide.Message msg = Riptide.Message.Create(MessageSendMode.Unreliable, (ushort)MessageID.PlayerDataSync);
            
            msg.AddSerializable(data);

            if (NetworkClient.Instance?.Client != null && NetworkClient.Instance.Client.IsConnected)
            {
                NetworkClient.Instance.Client.Send(msg);
                return;
            }

            if (NetworkServer.Instance?.Server != null && NetworkServer.Instance.Server.IsRunning)
            {
                NetworkServer.Instance.Server.SendToAll(msg, data.NetID);
                return;
            }

            LogManager.Net.Error("Cannot send PlayerDataSync: no client or server available.");
        }

        // Sending Scene Change
        // TODO: Replace this with actual level synchronization
        public static void SendSceneChange(string sceneName)
        {
            var data = new SceneChangeData(sceneName);
            var msg = Riptide.Message.Create(MessageSendMode.Reliable, (ushort)MessageID.SceneChange);
            msg.AddSerializable(data);
            // Host is both server & client
            if (NetworkServer.Instance.isActive)
                NetworkServer.Instance.Server.SendToAll(msg);
            else
                NetworkClient.Instance.Client.Send(msg);
        }

        // Sending DespawnPlayer to all connected clients
        public static void SendDespawn(ushort netID)
        {
            var data = new DespawnPlayerData(netID);
            var msg = Riptide.Message.Create(MessageSendMode.Reliable, (ushort)MessageID.DespawnPlayer);
            msg.AddSerializable(data);
            if (NetworkServer.Instance.isActive)
                NetworkServer.Instance.Server.SendToAll(msg, netID);
        }
    }

    // MESSAGE ROUTER && SPAWN MANAGER //

    /// <summary>
    /// Handles Incoming Message from the network,
    /// only one instance exists always
    /// </summary>
    public class MessageHandler : MonoBehaviour
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

        // SERVER‑SIDE HANDLERS (groupId = 0)
        
        // Handles JoinRequest,
        // Spawns player for itself, relays message to all connected clients to also spawn the new player
        [MessageHandler((ushort)MessageID.JoinRequest, (byte)GroupID.Server)]
        private static void HandleJoinRequest_Server(ushort clientId, Riptide.Message msg)
        {
            JoinRequestData data = msg.GetSerializable<JoinRequestData>();
            LogManager.Server.Info($"JoinRequest from {clientId}: {data.Username}");

            // Broadcast spawn to everyone
            Riptide.Message spawnMsg = Riptide.Message.Create(MessageSendMode.Reliable, (ushort)MessageID.SpawnPlayer);
            spawnMsg.AddSerializable(new SpawnPlayerData(clientId));
            NetworkServer.Instance.Server.SendToAll(spawnMsg);

            // Always tell the new Client about the host client, don't send this to host himself
            if (clientId != 1)
            {
                LogManager.Server.Info("Telling new client about host");
                Riptide.Message hostMessage = Riptide.Message.Create(MessageSendMode.Reliable, (ushort)MessageID.SpawnPlayer);
                hostMessage.AddSerializable(new SpawnPlayerData((ushort)1));
                NetworkServer.Instance.Server.Send(hostMessage, clientId);
            }
            

            // Tell new client about existing players
            foreach (ushort existingID in Instance._players.Keys)
            {
                LogManager.Server.Info($"Sending SpawnPlayer for ID {existingID}; Not Sending to {clientId}");
                Riptide.Message m = Riptide.Message.Create(MessageSendMode.Reliable, (ushort)MessageID.SpawnPlayer);
                m.AddSerializable(new SpawnPlayerData(existingID));
                NetworkServer.Instance.Server.Send(m, clientId);
            }
        }

        // Handles PlayerData synchronization
        // this one acts as a relay, because the server doesn't need to do anything else with this
        [MessageHandler((ushort)MessageID.PlayerDataSync, (byte)GroupID.Server)]
        private static void HandlePlayerDataSync_Server(ushort fromClientId, Riptide.Message msg)
        {
            // Relay to all except sender
            NetworkServer.Instance.Server.SendToAll(msg, fromClientId);
        }

        // Handles SceneChange
        // acts as a relay to other clients
        // TODO: Replace with actual level synchronization
        [MessageHandler((ushort)MessageID.SceneChange, (byte)GroupID.Server)]
        private static void HandleSceneChange_Server(ushort fromClientId, Riptide.Message msg)
        {
            // Relay scene‐change
            NetworkServer.Instance.Server.SendToAll(msg);
        }

        // CLIENT‑SIDE HANDLERS (groupId = 1)

        // Handles Spawning the player on the local game
        // Gets the data, and spawn player with their NetworkID so they can be manipulated easily
        [MessageHandler((ushort)MessageID.SpawnPlayer, (byte)GroupID.Client)]
        private static void HandleSpawnPlayer_Client(Riptide.Message msg)
        {
            SpawnPlayerData data = msg.GetSerializable<SpawnPlayerData>();
            LogManager.Client.Info($"SpawnPlayer for ID {data.NetID}");
            Instance.SpawnPlayer_Internal(data.NetID);
        }

        // Handles Despawning players
        // Handles the Message for despawning a client, this happens when the other client disconnects,
        // Server notifies all other clients that this client disconnected
        [MessageHandler((ushort)MessageID.DespawnPlayer, (byte)GroupID.Client)]
        private static void HandleDespawnPlayer_Client(Riptide.Message msg)
        {
            DespawnPlayerData data = msg.GetSerializable<DespawnPlayerData>();
            LogManager.Client.Info($"DespawnPlayer for ID {data.NetID}");
            Instance.DespawnPlayer(data.NetID);
        }

        // Handles Player Data Synchronization
        // Gets the data for a client identified with NetworkID
        // Gets the networked clone and manipulates it
        [MessageHandler((ushort)MessageID.PlayerDataSync, (byte)GroupID.Client)]
        private static void HandlePlayerDataSync_Client(Riptide.Message msg)
        {
            PlayerData data = msg.GetSerializable<PlayerData>();
            
            // Get the network clone
            if (Instance._players.TryGetValue(data.NetID, out var go))
            {
                PlayerNetworkController playerNetworkController = go.GetComponent<PlayerNetworkController>();
                
                // Separating game logic from network logic
                // Passes data recieved from the message to the components to do synchronization
                playerNetworkController.UpdateHands(
                    data.HandLeftPosition, data.HandRightPosition,
                    data.HandLeftState, data.HandRightState, data.HandLeftColor, data.HandRightColor
                );
                playerNetworkController.UpdatePositionRotation(data.Position, data.Rotation);
            }
        }

        // Handles SceneChange
        // TODO: replace this with actual level synchronization
        [MessageHandler((ushort)MessageID.SceneChange, (byte)GroupID.Client)]
        private static void HandleSceneChange_Client(Riptide.Message msg)
        {
            SceneChangeData data = msg.GetSerializable<SceneChangeData>();
            LogManager.Client.Info($"Loading scene {data.SceneName}");
            UnityEngine.SceneManagement.SceneManager.LoadScene(data.SceneName);
        }

        // INTERNAL (SHARED) LOGIC //

        
        // Spawns the player, with suffix: _NetworkID
        // If this is triggered for local player, adds the PlayerNetworkController script
        private void SpawnPlayer_Internal(ushort netID)
        {
            LogManager.Net.Info($"Starting SpawnPlayer on ID {netID}");
            if (_players.ContainsKey(netID)) return;

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

            GameObject go = player;

            if (netID == NetworkClient.Instance.Client.Id && NetworkClient.Instance.Client != null && netID != 0)
            {
                // Player is local, attach the PlayerNetworkController script, so it can properly synchronize to other clients
                
                HandsNetworkController leftHandController = go.transform.Find("Main Cam Root/Main Camera Shake Root/Main Camera/Inventory Camera/Inventory-Root/Left_Hand_Target/Item_Hand_Left/Item_Hands_Left").gameObject.AddComponent<HandsNetworkController>().Initialize(netID);
                HandsNetworkController rightHandController = go.transform.Find("Main Cam Root/Main Camera Shake Root/Main Camera/Inventory Camera/Inventory-Root/Right_Hand_Target/Item_Hand_Right/Item_Hands_Right").gameObject.AddComponent<HandsNetworkController>().Initialize(netID);
                go.AddComponent<PlayerNetworkController>().Initialize(netID, leftHandController, rightHandController);
                LogManager.Net.Info($"Player not spawned, it's local ; ID {netID}");
            }
            else if (netID != 0)
            {
                // Player is a networked copy, instantiate new copy
                
                LogManager.Net.Info($"Player spawned with netID {netID}");
                go = InstantiatePlayerPrefab(player, netID);
                go.SetActive(true);
                _players.Add(netID, go);
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

        // Instantiates the networked copy
        private GameObject InstantiatePlayerPrefab(GameObject player, ushort netID)
        {
            GameObject capsule;
            GameObject prefab = Object.Instantiate(player);
            Object.DontDestroyOnLoad(prefab);
            
            DestroyUnwantedComponents(prefab);

            prefab.name = $"{playerPrefabName}_{netID}";
            prefab.SetActive(false);
            
            // Add network controllers and initialize with netID
            HandsNetworkController leftHandController = prefab.transform.Find("Main Cam Root/Main Camera Shake Root/Main Camera/Inventory Camera/Inventory-Root/Left_Hand_Target/Item_Hand_Left/Item_Hands_Left").gameObject.AddComponent<HandsNetworkController>().Initialize(netID);
            HandsNetworkController rightHandController = prefab.transform.Find("Main Cam Root/Main Camera Shake Root/Main Camera/Inventory Camera/Inventory-Root/Right_Hand_Target/Item_Hand_Right/Item_Hands_Right").gameObject.AddComponent<HandsNetworkController>().Initialize(netID);
            prefab.AddComponent<PlayerNetworkController>().Initialize(netID, leftHandController, rightHandController);
            capsule = prefab.transform.Find("Capsule").gameObject;
            capsule.layer = LayerMask.NameToLayer("Player");
            LogManager.Net.Info($"Instantiated Networked Player Prefab for ID {netID}");

            return prefab;
        }

        private static void DestroyUnwantedComponents(GameObject prefab)
        {
            Object.Destroy(prefab.GetComponent<CharacterController>());
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
                    Object.Destroy(camObject.GetComponent<CameraShaderController>());
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
        }
    }
}