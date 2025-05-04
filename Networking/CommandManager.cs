using System;
using System.Collections;
using BepInEx.Logging;
using Steamworks;
using Unity.Netcode;
using UnityEngine;

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
            LeaveLobbyAndDisconnectClient();
        }

        
        private IEnumerator CreateLobbyAndHostGame()
        {
            if (!SteamClient.IsValid)
            {
                CommandConsole.LogError("Steam not initialized.");
                logger.LogError("Steam not initialized.");
                
                yield break;
            }

            var lobbyCreation = SteamMatchmaking.CreateLobbyAsync(4);
            while (!lobbyCreation.IsCompleted)
                yield return null;

            if (lobbyCreation.Result.HasValue)
            {
                var lobby = lobbyCreation.Result.Value;
                lobby.SetData("name", SteamClient.Name + "'s Lobby");
                lobby.SetData("owner", SteamClient.SteamId.ToString());

                CommandConsole.Log("Lobby created. Starting host...");
                logger.LogDebug("Lobby created. Starting host...");
                
                multiplayerManager.StartHost();
            }
            else
            {
                CommandConsole.LogError("Failed to create Steam lobby.");
                logger.LogError("Failed to create Steam lobby.");
            }
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

        private void LeaveLobbyAndDisconnectClient()
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
            multiplayerManager.Shutdown();
            
            CommandConsole.Log("Disconnected from server/lobby.");
            logger.LogInfo("Disconnected from server/lobby.");
        }
        
        
    }
}
