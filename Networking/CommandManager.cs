using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using BepInEx.Logging;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using White_Knuckle_Multiplayer.deps;
using White_Knuckle_Multiplayer.Listeners;

namespace White_Knuckle_Multiplayer.Networking
{
    public class CommandManager
    {
        private readonly MultiplayerManager multiplayerManager;
        private readonly ManualLogSource logger;
        private readonly MonoBehaviour coroutineHost;

        private const string ErrorCoroutineHostNull = "Coroutine host is null! Cannot start host command.";
        private const string ErrorSteamNotInitialized = "Steam not initialized.";
        private const string ErrorInvalidSteamId = "Please provide a valid Steam ID.";
        private const string ErrorTransportUnavailable = "Transport unavailable.";
        private const string MessageShutdown = "Shutting down host and disconnecting all clients...";
        private const string SceneMainMenu = "Main-Menu";
        
        private const ushort DefaultPort = 7777;

        public CommandManager(MultiplayerManager multiplayerManager, ManualLogSource logger, MonoBehaviour coroutineHost)
        {
            this.multiplayerManager = multiplayerManager;
            this.logger = logger;
            this.coroutineHost = coroutineHost;
        }

        // Steam Networking Commands

        public void HandleHostCommand(string[] args)
        {
            if (!ValidateCoroutineHost()) return;

            coroutineHost.StartCoroutine(CreateLobbyAndHostGame());
        }

        public void HandleJoinCommand(string[] args)
        {
            if (!ValidateArgsAndParseSteamId(args, out ulong steamId)) return;

            JoinLobbyAndStartClient(steamId);
        }
        
        public void HandleDisconnectCommand(string[] args)
        {
            ShutdownHostAndDisconnectClients();
        }

        // Local Networking Commands
        
        public void HandleLocalHostCommand(string[] args)
        {
            ushort port = DefaultPort;
            
            if (args.Length > 0 && ushort.TryParse(args[0], out ushort customPort))
            {
                port = customPort;
            }
            
            StartLocalHost(port);
        }
        
        public void HandleLocalJoinCommand(string[] args)
        {
            if (args.Length == 0)
            {
                CommandConsole.LogError("Please provide the host IP address to connect to.");
                CommandConsole.LogError("Usage: localjoin <host-ip> [port]");
                CommandConsole.LogError("Example: localjoin 192.168.1.5 7777");
                logger.LogWarning("Missing host IP parameter.");
                return;
            }
            
            string hostIp = args[0];
            ushort port = DefaultPort;
            
            // If a second argument is provided, use it as the port
            if (args.Length > 1 && ushort.TryParse(args[1], out ushort customPort))
            {
                port = customPort;
            }
            
            JoinLocalHost(hostIp, port);
        }
        
        public void HandleNetworkInfoCommand(string[] args)
        {
            DisplayNetworkInfo();
        }
        
        public void HandleHelpCommand(string[] args)
        {
            CommandConsole.Log("--- White Knuckle Multiplayer Commands ---");
            CommandConsole.Log("Steam Multiplayer:");
            CommandConsole.Log("  host                - Start a Steam hosted game");
            CommandConsole.Log("  join <SteamID>      - Join a Steam hosted game by Steam ID");
            CommandConsole.Log("");
            CommandConsole.Log("Local Testing:");
            CommandConsole.Log("  localhost [port]    - Start a local host on specified port (default: 7777)");
            CommandConsole.Log("  localjoin <IP> [port] - Join a locally hosted game at IP:port");
            CommandConsole.Log("");
            CommandConsole.Log("Other Commands:");
            CommandConsole.Log("  disconnect         - Disconnect from current session");
            CommandConsole.Log("  netinfo            - Display network connection info");
            CommandConsole.Log("  help               - Show this help message");
            CommandConsole.Log("");
            CommandConsole.Log("To test locally:");
            CommandConsole.Log("1. Start a host in one game instance with: localhost");
            CommandConsole.Log("2. Note the displayed IP address in the console");
            CommandConsole.Log("3. Start another game instance and join with: localjoin <IP>");
        }

        public void HandleTestConnectionCommand(string[] args)
        {
            TestConnectivity();
        }

        // Implementation Methods

        private IEnumerator CreateLobbyAndHostGame()
        {
            if (!IsSteamClientValid()) yield break;

            // Initialize and start host
            multiplayerManager.InitializeNetworkManager();
            multiplayerManager.StartHost();
        }

        private void JoinLobbyAndStartClient(ulong steamId)
        {
            if (!IsSteamClientValid()) return;

            var transport = multiplayerManager.GetTransport();
            if (transport == null)
            {
                LogError(ErrorTransportUnavailable);
                return;
            }

            transport.targetSteamId = steamId;

            try
            {
                multiplayerManager.InitializeNetworkManager();
                multiplayerManager.StartClient();
                CommandConsole.Log($"Connecting to Steam user {steamId}...");
            }
            catch (Exception ex)
            {
                LogError($"Client start error: {ex.Message}");
            }
        }
        
        private void StartLocalHost(ushort port)
        {
            logger.LogInfo($"Starting local host on port {port}");
            
            try
            {
                // Initialize our network manager
                multiplayerManager.InitializeNetworkManager();
                
                // Configure transport for direct IP
                multiplayerManager.SetupDirectIP(true, null, port);
                
                // Start the host
                multiplayerManager.StartHost();
                
                // Display connection info
                var localIp = GetLocalIpAddress();
                CommandConsole.Log($"✓ Local host started on {localIp}:{port}");
                CommandConsole.Log($"► Other players can connect using: localjoin {localIp} {port}");
                CommandConsole.Log("► For cross-machine connections, make sure your firewall allows incoming connections");
            }
            catch (Exception ex)
            {
                logger.LogError($"Error starting local host: {ex.Message}");
                logger.LogDebug(ex.StackTrace);
                CommandConsole.LogError($"✗ Error starting local host: {ex.Message}");
            }
        }
        
        private void JoinLocalHost(string hostIp, ushort port)
        {
            logger.LogInfo($"Joining local host at {hostIp}:{port}");
            
            try
            {
                // Initialize our network manager
                multiplayerManager.InitializeNetworkManager();
                
                // Configure transport for direct IP
                multiplayerManager.SetupDirectIP(true, hostIp, port);
                
                // Start the client
                multiplayerManager.StartClient();
                
                CommandConsole.Log($"► Connecting to {hostIp}:{port}");
                CommandConsole.Log("► Connection process started...");
            }
            catch (Exception ex)
            {
                logger.LogError($"Error joining local host: {ex.Message}");
                logger.LogDebug(ex.StackTrace);
                CommandConsole.LogError($"✗ Error joining local host: {ex.Message}");
            }
        }

        private void ShutdownHostAndDisconnectClients()
        {
            logger.LogInfo(MessageShutdown);
            
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.NetworkConfig.NetworkTransport != null)
            {
                NetworkManager.Singleton.NetworkConfig.NetworkTransport.Shutdown();
                logger.LogInfo("Network transport shut down.");
            }
            
            SceneManager.LoadScene(SceneMainMenu);
            logger.LogInfo("Host and client connections shut down.");
            CommandConsole.Log("Disconnected from network session.");
        }
        
        private void DisplayNetworkInfo()
        {
            try
            {
                CommandConsole.Log("--- Network Connection Info ---");
                
                // Get local IP information
                string localIp = GetLocalIpAddress();
                CommandConsole.Log($"Local IP: {localIp}");
                
                // Get Steam information
                if (SteamClient.IsValid && SteamClient.IsLoggedOn)
                {
                    CommandConsole.Log($"Steam ID: {SteamClient.SteamId}");
                    CommandConsole.Log($"Steam Username: {SteamClient.Name}");
                }
                else
                {
                    CommandConsole.LogError("Steam is not initialized or logged on.");
                }
                
                // Get current network state
                if (NetworkManager.Singleton != null)
                {
                    bool isActive = NetworkManager.Singleton.IsListening;
                    CommandConsole.Log($"Network Manager Active: {(isActive ? "✓ Yes" : "✗ No")}");
                    
                    if (isActive)
                    {
                        string role = NetworkManager.Singleton.IsHost ? "Host" : 
                                     NetworkManager.Singleton.IsServer ? "Server" : 
                                     NetworkManager.Singleton.IsClient ? "Client" : "Unknown";
                        
                        CommandConsole.Log($"Network Role: {role}");
                        
                        var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport;
                        CommandConsole.Log($"Transport Type: {transport.GetType().Name}");
                        
                        // Display transport-specific info
                        if (transport is DirectIPTransport directIP)
                        {
                            CommandConsole.Log($"Using direct IP mode on port {directIP.port}");
                            if (NetworkManager.Singleton.IsClient)
                            {
                                CommandConsole.Log($"Connected to: {directIP.ipAddress}");
                            }
                        }
                        else if (transport is FacepunchTransport steamTransport)
                        {
                            CommandConsole.Log("Using Steam networking mode");
                            if (NetworkManager.Singleton.IsClient)
                            {
                                CommandConsole.Log($"Connected to Steam ID: {steamTransport.targetSteamId}");
                            }
                        }
                        
                        // Connection counts
                        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
                        {
                            int clientCount = NetworkManager.Singleton.ConnectedClientsIds.Count;
                            CommandConsole.Log($"Connected clients: {clientCount}");
                        }
                    }
                    else
                    {
                        CommandConsole.Log("Not connected to any network session.");
                    }
                }
                else
                {
                    CommandConsole.LogError("Network Manager not initialized.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error getting network info: {ex.Message}");
                CommandConsole.LogError($"Error getting network info: {ex.Message}");
            }
        }

        private void TestConnectivity()
        {
            var localIp = GetLocalIpAddress();
            
            CommandConsole.Log("Testing local connectivity...");
            CommandConsole.Log($"Your local IP address is: {localIp}");
            CommandConsole.Log("");
            CommandConsole.Log("To test local multiplayer:");
            CommandConsole.Log("1. Start a host with:");
            CommandConsole.Log($"   localhost 7777");
            CommandConsole.Log("");
            CommandConsole.Log("2. Then in another instance, connect with:");
            CommandConsole.Log($"   localjoin {localIp} 7777");
        }

        private string GetLocalIpAddress()
        {
            return Dns.GetHostEntry(Dns.GetHostName())
                .AddressList
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                ?.ToString() ?? "127.0.0.1";
        }

        private bool ValidateCoroutineHost()
        {
            if (coroutineHost == null)
            {
                LogError(ErrorCoroutineHostNull);
                return false;
            }
            return true;
        }

        private bool ValidateArgsAndParseSteamId(string[] args, out ulong steamId)
        {
            if (args.Length != 1 || !ulong.TryParse(args[0], out steamId))
            {
                LogWarning(ErrorInvalidSteamId);
                steamId = 0;
                return false;
            }
            return true;
        }

        private bool IsSteamClientValid()
        {
            if (!SteamClient.IsValid || !SteamClient.IsLoggedOn)
            {
                LogError(ErrorSteamNotInitialized);
                return false;
            }
            return true;
        }

        private void LogError(string message)
        {
            CommandConsole.LogError(message);
            logger.LogError(message);
        }

        private void LogWarning(string message)
        {
            CommandConsole.LogError(message);
            logger.LogWarning(message);
        }
    }
}


