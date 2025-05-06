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
        private NetworkManager manager;
        internal FacepunchTransport Transport;
        internal TransportListener Listener;
        public FacepunchTransport GetTransport() => Transport;

        public void SpawnNetworkManager()
        {
            if (NetworkManager.Singleton != null)
                return;

            GameObject netObj = new GameObject("NetworkManager");
            Object.DontDestroyOnLoad(netObj);

            manager = netObj.AddComponent<NetworkManager>();
            Transport = netObj.AddComponent<FacepunchTransport>();
            
            Listener = netObj.AddComponent<TransportListener>();
            netObj.AddComponent<CoroutineRunner>();

            NetworkManager.Singleton.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = Transport
            };

            logger.LogInfo("Created NetworkManager");
        }

        
        private void CreatePlayerPrefab()
        {
            logger.LogDebug("Creating Player Prefab...");
            
            GameObject player = GameObject.Find("CL_Player");
            if (player == null)
            {
                logger.LogError("Cannot start host, no player found!");
                return;
            }
            
            GameObject playerPrefab = Object.Instantiate(player);
            Object.DontDestroyOnLoad(playerPrefab);
            playerPrefab.name = "CL_Player_Network_Prefab";
            playerPrefab.SetActive(false);

            if (playerPrefab.GetComponent<NetworkObject>() == null)
                playerPrefab.AddComponent<NetworkObject>();

            if (playerPrefab.GetComponent<NetworkTransform>() == null)
                playerPrefab.AddComponent<NetworkTransform>();

            //playerPrefab.GetComponent<NetworkTransform>().UseQuaternionSynchronization = true;

            Object.Destroy(playerPrefab.GetComponent<CharacterController>());
            Object.Destroy(playerPrefab.GetComponent<Inventory>());
            Object.Destroy(playerPrefab.GetComponent<MonoBehaviour>());
            Object.Destroy(playerPrefab.transform.Find("Main Cam Root").GetComponent<CL_CameraControl>());
            Object.Destroy(playerPrefab.transform.Find("Main Cam Root/Main Camera Shake Root/Main Camera").GetComponent<CRTEffect>());
            Object.Destroy(playerPrefab.transform.Find("Main Cam Root/Main Camera Shake Root/Main Camera").GetComponent<PostProcessVolume>());
            Object.Destroy(playerPrefab.transform.Find("Main Cam Root/Main Camera Shake Root/Main Camera").GetComponent<PostProcessLayer>());
            Object.Destroy(playerPrefab.transform.Find("Main Cam Root/Main Camera Shake Root/Main Camera").GetComponent<Camera>());
            Object.Destroy(playerPrefab.transform.Find("Main Cam Root/Main Camera Shake Root/Main Camera").GetComponent<CameraShaderController>());
            Object.Destroy(playerPrefab.transform.Find("Main Cam Root/Main Camera Shake Root/Main Camera/Inventory Camera").GetComponent<CRTEffect>());
            Object.Destroy(playerPrefab.transform.Find("Main Cam Root/Main Camera Shake Root/Main Camera/Inventory Camera").GetComponent<Camera>());
            Object.Destroy(playerPrefab.transform.Find("Main Cam Root/Main Camera Shake Root/Main Camera/Inventory Camera").GetComponent<CameraShaderController>());
            Object.Destroy(playerPrefab.transform.Find("Main Cam Root/Main Camera Shake Root/Main Camera/Inventory Camera/Inventory").gameObject);
            Object.Destroy(playerPrefab.transform.Find("Main Cam Root/Main Camera Shake Root/Main Camera/Inventory Camera/InventoryBagCamera").gameObject);
            
            // TODO: Write code to destroy MantleCheckpoint, every 1st child except camera
            

            NetworkManager.Singleton.NetworkConfig.PlayerPrefab = playerPrefab;
            logger.LogDebug("Player Prefab created");
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
                CommandConsole.LogError($"Unable to initialise host! Ex:{ex}");
                logger.LogError($"Unable to initialise host! Ex:{ex}");
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
                logger.LogError($"Unable to initialise client! Ex:{ex}");
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
            networkObject.name = $"CL_Player_Network_Prefab({clientId})";
            if (networkObject != null)
            {
                networkObject.SpawnAsPlayerObject(clientId);
                logger.LogInfo("Client connected to lobby!");
            }
            else
            {
                logger.LogError("Instantiated player missing NetworkObject component!");
            }
        }

        public void OnClientDisconnect(ulong clientId)
        {
            Object.Destroy(GameObject.Find($"CL_Player_Network_Prefab({clientId})"));
        }
    }
}
