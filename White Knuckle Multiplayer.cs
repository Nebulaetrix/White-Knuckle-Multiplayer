using BepInEx;
using BepInEx.Logging;
using UnityEngine.SceneManagement;
using White_Knuckle_Multiplayer.Managers;
using White_Knuckle_Multiplayer.Networking;

namespace White_Knuckle_Multiplayer;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class WkMultiplayer : BaseUnityPlugin
{
    private bool loaded = false;

    public static GameManager GameManager;
    private CommandManager commandManager;
    private CoroutineRunner coroutineRunner;

    private void Awake()
    {
        LogManager.Init(base.Logger);

        GameManager = new GameManager();

        SceneManager.sceneLoaded += OnSceneLoad;

        LogManager.Info($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    private void OnSceneLoad(Scene scene, LoadSceneMode mode)
    {
        // ADDED CASE FOR "Intro" SCENE
        if (scene.name == "Intro")
        {
            SceneManager.LoadScene("Main-Menu");
            return; 
        }

        switch (loaded)
        {
            case false when scene.name == "Game-Main":
                GameManager.InitializeWKNetworking();
                    
                if (coroutineRunner == null)
                {
                    var coroutineObject = new UnityEngine.GameObject("CoroutineRunner");
                    UnityEngine.Object.DontDestroyOnLoad(coroutineObject);
                    coroutineRunner = coroutineObject.AddComponent<CoroutineRunner>();
                    // LogManager.Info("Created CoroutineRunner"); // Original comment
                }
                    
                commandManager = new CommandManager(GameManager, coroutineRunner, coroutineRunner);
                    
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
            LogManager.Error("Cannot add commands - CommandManager is null");
            return;
        }
        
        // Add commands to CommandConsole
        CommandConsole.AddCommand("host", commandManager.HandleLocalHostCommand, false);
        CommandConsole.AddCommand("join", commandManager.HandleLocalJoinCommand, false);
        CommandConsole.AddCommand("disconnect", commandManager.HandleDisconnectCommand, false);
        
        LogManager.Info("Commands registered successfully");
    }
}