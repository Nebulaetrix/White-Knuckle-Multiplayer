using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Riptide;
using UnityEngine.Rendering.PostProcessing;
using Object = UnityEngine.Object;

namespace White_Knuckle_Multiplayer.Networking
{
    public enum MessageID : ushort
    {
        Unknown = 0,
        JoinRequest = 1,
        ConnectionError = 2,
        SteamAuthError = 3,
        PlayerDataSync = 4,
        SpawnPlayer = 5,
        SceneChange = 6, // This one will propably be replaced
    }

    // DATA STRUCTS //

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

    public struct PlayerData : IMessageSerializable
    {
        public ushort NetID;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 HandLeftPosition;
        public Vector3 HandRightPosition;
        public string HandLeftState;
        public string HandRightState;
        public PlayerData(ushort playerID, Vector3 position, Quaternion rotation,
            Vector3 handLeftPosition, Vector3 handRightPosition, string handLeftState, string handRightState)
        {
            NetID = playerID;
            Position = position;
            Rotation = rotation;
            HandLeftPosition = handLeftPosition;
            HandRightPosition = handRightPosition;
            HandLeftState = handLeftState;
            HandRightState = handRightState;
        }

        public void Serialize(Riptide.Message message)
        {
            message.AddUShort(NetID);

            // Vector3
            message.AddFloat(Position.x);
            message.AddFloat(Position.y);
            message.AddFloat(Position.z);
            
            // Quaternion
            message.AddFloat(Rotation.x);
            message.AddFloat(Rotation.y);
            message.AddFloat(Rotation.z);
            message.AddFloat(Rotation.w);
            
            // Vector3
            message.AddFloat(HandLeftPosition.x);
            message.AddFloat(HandLeftPosition.y);
            message.AddFloat(HandLeftPosition.z);
            
            // Vector3
            message.AddFloat(HandRightPosition.x);
            message.AddFloat(HandRightPosition.y);
            message.AddFloat(HandRightPosition.z);
            
            // String
            message.AddString(HandLeftState);
            message.AddString(HandRightState);
        }

        public void Deserialize(Riptide.Message message)
        {
            NetID = message.GetUShort();

            // Vector3
            float x = message.GetFloat();
            float y = message.GetFloat();
            float z = message.GetFloat();
            Position = new Vector3(x, y, z);

            // Quaternion
            float rx = message.GetFloat();
            float ry = message.GetFloat();
            float rz = message.GetFloat();
            float rw = message.GetFloat();
            Rotation = new Quaternion(rx, ry, rz, rw);
            
            // Vector3
            float handLeftX = message.GetFloat();
            float handLeftY = message.GetFloat();
            float handLeftZ = message.GetFloat();
            HandLeftPosition = new Vector3(handLeftX, handLeftY, handLeftZ);
            
            // Vector3
            float handRightX = message.GetFloat();
            float handRightY = message.GetFloat();
            float handRightZ = message.GetFloat();
            HandLeftPosition = new Vector3(handRightX, handRightY, handRightZ);
            
            // String
            HandLeftState = message.GetString();
            HandRightState = message.GetString();
        }
    }

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

    // (STATIC) MESSAGE SENDER //

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
    }

    // MESSAGE ROUTER && SPAWN MANAGER //

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

        [MessageHandler((ushort)MessageID.JoinRequest, 0)]
        private static void HandleJoinRequest_Server(ushort clientId, Riptide.Message msg)
        {
            JoinRequestData data = msg.GetSerializable<JoinRequestData>();
            LogManager.Server.Info($"JoinRequest from {clientId}: {data.Username}");

            // Broadcast spawn to everyone
            Riptide.Message spawnMsg = Riptide.Message.Create(MessageSendMode.Reliable, (ushort)MessageID.SpawnPlayer);
            spawnMsg.AddSerializable(new SpawnPlayerData(clientId));
            NetworkServer.Instance.Server.SendToAll(spawnMsg);

            // Always tell the new Client about host client, don't send this to host himself
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

            // Track on server
            // Instance.SpawnPlayer(clientId); // No.
        }

        [MessageHandler((ushort)MessageID.PlayerDataSync, 0)]
        private static void HandlePlayerDataSync_Server(ushort fromClientId, Riptide.Message msg)
        {
            // Relay to all except sender
            NetworkServer.Instance.Server.SendToAll(msg, fromClientId);
        }

        [MessageHandler((ushort)MessageID.SceneChange, 0)]
        private static void HandleSceneChange_Server(ushort fromClientId, Riptide.Message msg)
        {
            // Relay scene‐change
            NetworkServer.Instance.Server.SendToAll(msg);
        }

        // CLIENT‑SIDE HANDLERS (groupId = 1)

        [MessageHandler((ushort)MessageID.SpawnPlayer, 1)]
        private static void HandleSpawnPlayer_Client(Riptide.Message msg)
        {
            SpawnPlayerData data = msg.GetSerializable<SpawnPlayerData>();
            LogManager.Client.Info($"SpawnPlayer for ID {data.NetID}");
            Instance.SpawnPlayer_Internal(data.NetID);
        }

        [MessageHandler((ushort)MessageID.PlayerDataSync, 1)]
        private static void HandlePlayerDataSync_Client(Riptide.Message msg)
        {
            PlayerData data = msg.GetSerializable<PlayerData>();
            if (Instance._players.TryGetValue(data.NetID, out var go))
                // Separating game logic from network logic
                go.GetComponent<PlayerNetworkController>().UpdatePositionRotation(data.Position, data.Rotation);
        }

        [MessageHandler((ushort)MessageID.SceneChange, 1)]
        private static void HandleSceneChange_Client(Riptide.Message msg)
        {
            SceneChangeData data = msg.GetSerializable<SceneChangeData>();
            LogManager.Client.Info($"Loading scene {data.SceneName}");
            UnityEngine.SceneManagement.SceneManager.LoadScene(data.SceneName);
        }

        // INTERNAL (SHARED) LOGIC //

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

            if (netID == NetworkClient.Instance.Client.Id && NetworkClient.Instance.Client != null)
            {
                go.AddComponent<PlayerNetworkController>().Initialize(netID);
                LogManager.Net.Info($"Player not spawned, it's local ; ID {netID}");
            }
            else
            {
                LogManager.Net.Info($"Player spawned with netID {netID}");
                go = InstantiatePlayerPrefab(player, netID);
                go.SetActive(true);
                _players.Add(netID, go);
            }
        }

        public void DespawnPlayer(ushort netID)
        {
            if (!_players.TryGetValue(netID, out var go)) return;
            Destroy(go);
            _players.Remove(netID);
            LogManager.Net.Info($"Despawned player with ID {netID}");
        }

        // HELPER FUNCTIONS //
        // TODO: Make An intermidiate Script containing all these

        private GameObject InstantiatePlayerPrefab(GameObject player, ushort netID)
        {
            GameObject prefab = Object.Instantiate(player);
            Object.DontDestroyOnLoad(prefab);

            DestroyUnwantedComponents(prefab);

            prefab.name = $"{playerPrefabName}_{netID}";
            prefab.SetActive(false);
            
            // Add network controller and initialize with netID
            prefab.AddComponent<PlayerNetworkController>().Initialize(netID);

            LogManager.Net.Info($"Instantiated Networked Player Prefab for ID {netID}");

            return prefab;
        }

        private void DestroyUnwantedComponents(GameObject prefab)
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
            "Main Cam Target", "Capsule", "Particle System", "Wind Sound", "Fatigue Sound",
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