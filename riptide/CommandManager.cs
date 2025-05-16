using System;
using BepInEx.Logging;
using UnityEngine;
using White_Knuckle_Multiplayer.Managers;

namespace White_Knuckle_Multiplayer.Networking
{
   
    public class CommandManager
    {
        private readonly NetworkManager networkManager;
        private readonly ManualLogSource logger;
        private readonly MonoBehaviour coroutineHost;
        
        private RiptideNetworking riptideNetworking => networkManager.GetRiptideNetworking();
        
        public CommandManager(NetworkManager networkManager, ManualLogSource logger, MonoBehaviour coroutineHost)
        {
            this.networkManager = networkManager;
            this.logger = logger;
            this.coroutineHost = coroutineHost;
        }
        
        // Host command
        public void HandleHostCommand(string[] args)
        {
            logger.LogInfo("Starting local Riptide host...");
            
            try
            {
                // Start server 
                networkManager.StartHost();
                
                CommandConsole.Log("Riptide server started!");
            }
            catch (Exception ex)
            {
                logger.LogError($"Error starting Riptide host: {ex.Message}");
                CommandConsole.LogError($"Error starting Riptide host: {ex.Message}");
            }
        }
        
        // Handle the command to start a server only
        public void HandleServerCommand(string[] args)
        {
            logger.LogInfo("Starting Riptide server...");
            
            try
            {
                // Start server only
                networkManager.StartServer();
                
                CommandConsole.Log("Riptide server started!");
            }
            catch (Exception ex)
            {
                logger.LogError($"Error starting Riptide server: {ex.Message}");
                CommandConsole.LogError($"Error starting Riptide server: {ex.Message}");
            }
        }
        
        // Handle the command to join a server
        public void HandleJoinCommand(string[] args)
        {
            string serverAddress = "localhost";
            if (args.Length >= 1 && !string.IsNullOrEmpty(args[0]))
            {
                serverAddress = args[0];
            }
            
            logger.LogInfo($"Joining Riptide server at {serverAddress}...");
            
            try
            {
                // Set the server address
                if (riptideNetworking != null)
                {
                    riptideNetworking.ClientConnectAddress = serverAddress;
                }
                
                // Connect to the server
                networkManager.StartClient();
                
                CommandConsole.Log($"Connecting to Riptide server at {serverAddress}...");
            }
            catch (Exception ex)
            {
                logger.LogError($"Error connecting to Riptide server: {ex.Message}");
                CommandConsole.LogError($"Error connecting to Riptide server: {ex.Message}");
            }
        }
        
        // Handle the command to disconnect
        public void HandleDisconnectCommand(string[] args)
        {
            logger.LogInfo("HandleDisconnectCommand called");
            try
            {
                CommandConsole.Log("Disconnecting from Riptide server/stopping Riptide server...");
                networkManager.DisconnectClient();
                CommandConsole.Log("Disconnected from Riptide network");
            }
            catch (Exception ex)
            {
                logger.LogError($"Error disconnecting: {ex.Message}");
                CommandConsole.LogError($"Error disconnecting: {ex.Message}");
            }
        }
        
        // Handle the command to list connected players
        public void HandlePlayersCommand(string[] args)
        {
            if (riptideNetworking == null)
            {
                CommandConsole.LogError("Riptide networking not initialized");
                return;
            }
            
            networkManager.LogConnectedPlayers();
            CommandConsole.Log("Checking for connected players...");
        }
        
        // Handle the command to simulate player spawning (for testing)
        public void HandleSpawnCommand(string[] args)
        {
            if (riptideNetworking == null)
            {
                CommandConsole.LogError("Riptide networking not initialized");
                return;
            }
            
            string playerName = args.Length > 0 ? args[0] : "Player";
            networkManager.SimulatePlayerJoin(playerName);
            CommandConsole.Log($"Spawned player {playerName}");
        }
        
        // Alias handlers for convenience
        public void HandleLocalHostCommand(string[] args) => HandleHostCommand(args);
        public void HandleLocalJoinCommand(string[] args) => HandleJoinCommand(args);
    }
    
    
    public static class CommandConsole
    {
        public static void Log(string message)
        {
            
            Debug.Log($"[Console] {message}");
        }
        
        public static void LogError(string message)
        {
           
            Debug.LogError($"[Console] {message}");
        }
        
        public static void LogWarning(string message)
        {
            
            Debug.LogWarning($"[Console] {message}");
        }
        
        // Method to add a command to the console system
        public static void AddCommand(string commandName, Func<string[], bool> callback, bool requiresNetwork = true)
        {
            // This would register the command with your command system
            Debug.Log($"Registered command: {commandName}");
        }
    }
} 