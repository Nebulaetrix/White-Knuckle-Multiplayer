using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;
using White_Knuckle_Multiplayer.Managers;

namespace White_Knuckle_Multiplayer;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class WkMultiplayer : BaseUnityPlugin
{
    private bool loaded = false;

    private CommandManager commandManager;

    private MultiplayerGameManager gMan;

    private void Awake()
    {
        LogManager.Init(Logger);

        GameObject gmObj = new GameObject("MultiplayerManager");
        DontDestroyOnLoad(gmObj);
        gMan = gmObj.AddComponent<MultiplayerGameManager>();
        
        
        SceneManager.sceneLoaded += OnSceneLoad;

        LogManager.Info($"Plugin {MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} is loaded!");
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
                commandManager = new CommandManager(gMan);
                    
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