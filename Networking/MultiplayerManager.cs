using System;
using BepInEx.Logging;
using Steamworks;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using White_Knuckle_Multiplayer.deps;
using White_Knuckle_Multiplayer.Listeners;
using Object = UnityEngine.Object;

namespace White_Knuckle_Multiplayer.Networking
{
    public class MultiplayerManager(ManualLogSource logger)
    {
        private NetworkManager networkManagerInstance;
        internal FacepunchTransport Transport;
        internal TransportListener Listener;

        private const string NetworkManagerObjectName = "NetworkManager";
        private const string PlayerObjectName = "CL_Player";
        private const string PlayerPrefabName = "CL_Player_Network_Prefab";

        public FacepunchTransport GetTransport() => Transport;

        public void InitializeNetworkManager()
        {
            if (NetworkManager.Singleton != null) return;

            var networkManagerGameObject = new GameObject(NetworkManagerObjectName);
            Object.DontDestroyOnLoad(networkManagerGameObject);

            networkManagerInstance = networkManagerGameObject.AddComponent<NetworkManager>();
            Transport = networkManagerGameObject.AddComponent<FacepunchTransport>();
            Listener = networkManagerGameObject.AddComponent<TransportListener>();
            networkManagerGameObject.AddComponent<CoroutineRunner>();

            NetworkManager.Singleton.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = Transport
            };

            logger.LogInfo("Initialized NetworkManager");
        }

        private void CreatePlayerPrefab()
        {
            logger.LogDebug("Creating Player Prefab...");
            GameObject player = GameObject.Find(PlayerObjectName);
            if (player == null)
            {
                logger.LogError("Cannot start host, no player found!");
                return;
            }

            GameObject playerPrefab = InstantiatePlayerPrefab(player);
            DestroyUnwantedComponents(playerPrefab);

            NetworkManager.Singleton.NetworkConfig.PlayerPrefab = playerPrefab;
            logger.LogDebug("Player Prefab created");
        }

        private GameObject InstantiatePlayerPrefab(GameObject player)
        {
            GameObject prefab = Object.Instantiate(player);
            Object.DontDestroyOnLoad(prefab);

            prefab.name = PlayerPrefabName;
            prefab.SetActive(false);

            if (prefab.GetComponent<NetworkObject>() == null)
                prefab.AddComponent<NetworkObject>();
            if (prefab.GetComponent<NetworkTransform>() == null)
                prefab.AddComponent<NetworkTransform>();

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

        public void StartHost()
        {
            if (NetworkManager.Singleton == null) return;
            CreatePlayerPrefab();
            logger.LogDebug("Starting Host...");

            try
            {
                NetworkManager.Singleton.StartHost();
                CommandConsole.Log("Host started!");
                logger.LogInfo("Host started!");
            }
            catch (Exception ex)
            {
                CommandConsole.LogError($"Unable to initialize host! Ex:{ex}");
                logger.LogError($"Unable to initialize host! Ex:{ex}");
            }
        }

        public void StartClient()
        {
            if (NetworkManager.Singleton == null) return;
            CreatePlayerPrefab();
            logger.LogDebug("Starting Client...");

            try
            {
                NetworkManager.Singleton.StartClient();
                CommandConsole.Log("Client started!");
                logger.LogInfo("Client started!");
            }
            catch (Exception ex)
            {
                logger.LogError($"Unable to initialize client! Ex:{ex}");
            }
        }

        public void OnClientConnect(ulong clientId)
        {
            if ((ulong)SteamClient.SteamId == clientId)
            {
                logger.LogError("Cannot instantiate self!");
                return;
            }

            if (NetworkManager.Singleton.NetworkConfig.PlayerPrefab == null)
            {
                logger.LogError("Player prefab not registered!");
                return;
            }

            GameObject player = Object.Instantiate(NetworkManager.Singleton.NetworkConfig.PlayerPrefab);
            player.SetActive(true);

            var networkObject = player.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                logger.LogError("Instantiated player missing NetworkObject component!");
                return;
            }

            networkObject.name = $"{PlayerPrefabName}({clientId})";
            networkObject.SpawnAsPlayerObject(clientId);
            logger.LogInfo("Client connected to lobby!");
        }

        public void OnClientDisconnect(ulong clientId)
        {
            Object.Destroy(GameObject.Find($"{PlayerPrefabName}({clientId})"));
        }
    }
}
