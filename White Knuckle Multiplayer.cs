using BepInEx;
using BepInEx.Logging;
using UnityEngine.SceneManagement;
using White_Knuckle_Multiplayer.Managers;
using White_Knuckle_Multiplayer.Networking;

namespace White_Knuckle_Multiplayer;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class WkMultiplayer : BaseUnityPlugin
{
    private ManualLogSource logger;
    private bool loaded = false;

    public static GameManager GameManager;
    private CommandManager commandManager;
    private CoroutineRunner coroutineRunner;

    private void Awake()
    {
        logger = base.Logger;

        GameManager = new GameManager(logger);

        SceneManager.sceneLoaded += OnSceneLoad;

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    private void OnSceneLoad(Scene scene, LoadSceneMode mode)
    {
        switch (loaded)
        {
            case false when scene.name == "Game-Main":
                // Initialize only Mirage networking
                GameManager.InitializeMirageNetworking();
                
                // Setup the coroutine runner
                if (coroutineRunner == null)
                {
                    var coroutineObject = new UnityEngine.GameObject("CoroutineRunner");
                    UnityEngine.Object.DontDestroyOnLoad(coroutineObject);
                    coroutineRunner = coroutineObject.AddComponent<CoroutineRunner>();
                    logger.LogInfo("Created CoroutineRunner");
                }
                
                // Initialize command manager with all required parameters
                commandManager = new CommandManager(GameManager, logger, coroutineRunner, coroutineRunner);
                
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
        if (commandManager == null)
        {
            logger.LogError("Cannot add commands - CommandManager is null");
            return;
        }
        
        // Direct Mirage networking commands
        CommandConsole.AddCommand("host", commandManager.HandleLocalHostCommand, false);
        CommandConsole.AddCommand("join", commandManager.HandleLocalJoinCommand, false);
        CommandConsole.AddCommand("disconnect", commandManager.HandleDisconnectCommand, false);
        CommandConsole.AddCommand("localhost", commandManager.HandleLocalHostCommand, false);
        CommandConsole.AddCommand("localjoin", commandManager.HandleLocalJoinCommand, false);
        
        // Debugging commands for Mirage
        CommandConsole.AddCommand("players", commandManager.HandlePlayersCommand, false);
        
        logger.LogInfo("Commands registered successfully");
    }
}