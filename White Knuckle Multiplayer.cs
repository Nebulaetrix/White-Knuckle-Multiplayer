using BepInEx;
using BepInEx.Logging;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using White_Knuckle_Multiplayer.Networking;

namespace White_Knuckle_Multiplayer
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class WkMultiplayer : BaseUnityPlugin
    {
        private ManualLogSource logger;
        private bool loaded = false;

        public static MultiplayerManager MultiplayerManager;
        private CommandManager commandManager;

        private void Awake()
        {
            logger = base.Logger;

            MultiplayerManager = new MultiplayerManager(logger);

            SceneManager.sceneLoaded += OnSceneLoad;

            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        }

        private void OnSceneLoad(Scene scene, LoadSceneMode mode)
        {
            switch (loaded)
            {
                case false when scene.name == "Game-Main":
                    MultiplayerManager.InitializeNetworkManager();
                    commandManager = new CommandManager(MultiplayerManager, logger,
                        NetworkManager.Singleton.GetComponent<CoroutineRunner>());

                    NetworkManager.Singleton.LogLevel = Unity.Netcode.LogLevel.Developer;
                    AddCommands();
                    loaded = true;
                    break;
                
                case true when scene.name == "Game-Main":
                    AddCommands();
                    break;
            }
        }

        private void AddCommands()
        {
            CommandConsole.AddCommand("host", commandManager.HandleHostCommand, false);
            CommandConsole.AddCommand("join", commandManager.HandleJoinCommand, false);
            CommandConsole.AddCommand("disconnect", commandManager.HandleDisconnectCommand, false);
            
            // Add local commands for testing without Steam
            CommandConsole.AddCommand("localhost", commandManager.HandleLocalHostCommand, false);
            CommandConsole.AddCommand("localjoin", commandManager.HandleLocalJoinCommand, false);
            CommandConsole.AddCommand("netinfo", commandManager.HandleNetworkInfoCommand, false);
            CommandConsole.AddCommand("help", commandManager.HandleHelpCommand, false);
            CommandConsole.AddCommand("testnet", commandManager.HandleTestConnectionCommand, false);
        }
    }
}
