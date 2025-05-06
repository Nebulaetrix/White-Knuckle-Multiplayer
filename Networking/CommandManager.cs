using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx.Logging;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using UnityEngine;
using White_Knuckle_Multiplayer.deps;
using White_Knuckle_Multiplayer.Listeners;

namespace White_Knuckle_Multiplayer.Networking
{
    public class CommandManager
    {
        private readonly MultiplayerManager multiplayerManager;
        private readonly ManualLogSource logger;
        private readonly MonoBehaviour coroutineHost;

        public CommandManager(MultiplayerManager manager, ManualLogSource logger, MonoBehaviour coroutineHost)
        {
            multiplayerManager = manager;
            this.logger = logger;
            this.coroutineHost = coroutineHost;
        }

        public void HandleHostCommand(string[] args)
        {
            if (coroutineHost == null)
            {
                CommandConsole.LogError("Coroutine host is null! Cannot start host command.");
                logger.LogError("Coroutine host is null! Cannot start host command.");
                return;
            }
            coroutineHost.StartCoroutine(CreateLobbyAndHostGame());
            
        }

        public void HandleJoinCommand(string[] args)
        {
            if (args.Length != 1 || !ulong.TryParse(args[0], out ulong steamId))
            {
                CommandConsole.LogError("Please provide a valid Steam ID.");
                logger.LogWarning("Invalid or missing Steam ID.");
                return;
            }

            JoinLobbyAndStartClient(steamId);
        }
        
        public void HandleDisconnectCommand(string[] args)
        {
            ShutdownHostAndDisconnectClients();
        }

        
        
        
        private IEnumerator CreateLobbyAndHostGame()
        {
            if (!SteamClient.IsValid)
            {
                CommandConsole.LogError("Steam not initialized.");
                logger.LogError("Steam not initialized.");
                
                yield break;
            }

            multiplayerManager.StartHost();
        }

        private void JoinLobbyAndStartClient(ulong steamId)
        {
            if (!SteamClient.IsValid || !SteamClient.IsLoggedOn)
            {
                CommandConsole.LogError("Steam not initialized.");
                logger.LogError("Steam not initialized.");
                return;
            }

            var transport = multiplayerManager.GetTransport();
            if (transport == null)
            {
                CommandConsole.LogError("Transport unavailable.");
                logger.LogError("Transport unavailable.");
                return;
            }

            transport.targetSteamId = steamId;

            try
            {
                multiplayerManager.StartClient();
            }
            catch (Exception ex)
            {
                CommandConsole.LogError($"Client start error: {ex.Message}");
                logger.LogError($"Client start error: {ex.Message}");
            }
        }

        /*private void LeaveLobbyAndDisconnectClient()
        {
            var transport = multiplayerManager.GetTransport();

            if (!SteamClient.IsValid || transport == null)
            {
                CommandConsole.LogError("Steam not initialized.");
                logger.LogError("Steam not initialized.");
                return;
            }

            if (transport == null)
            {
                logger.LogError("Transport unavailable.");
                return;
            }

            ulong localClientId = NetworkManager.Singleton.LocalClientId;

            if (!multiplayerManager.Listener.IsClientConnected(localClientId))
            {
                CommandConsole.Log("You are not connected to a lobby or server.");
                logger.LogWarning("You are not connected to a lobby or server.");
                return;
            }

            if (Lobby.Id.IsValid)
            {
                Lobby.Leave();
                logger.LogDebug($"Left Steam lobby {Lobby.Id}.");
                CommandConsole.Log($"Left Steam lobby {Lobby.Id}.");
                Lobby = default;
            }
            try
            {
                NetworkManager.Singleton.Shutdown();
            }
            catch (Exception ex)
            {
                CommandConsole.LogError($"Client shutdown error: {ex.Message}");
                logger.LogError($"Client shutdown error: {ex.Message}");
            }

        }*/

        private void ShutdownHostAndDisconnectClients()
        {
            logger.LogInfo(
                $"Shutting down host and disconnecting {multiplayerManager.Listener.ConnectedClientIds.Count} client(s)...");
            
            var connectedClientsList = multiplayerManager.Listener.ConnectedClientIds;
            
            foreach (var connectionId in connectedClientsList)
            {
                multiplayerManager.Transport.CloseConnectionWithClient(connectionId);
            }

            NetworkManager.Singleton.Shutdown();
            logger.LogInfo("Host and all client connections shut down.");
        }
        
    }
}
