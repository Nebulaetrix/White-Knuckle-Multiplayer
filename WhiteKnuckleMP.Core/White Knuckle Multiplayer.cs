using BepInEx;
using UnityEngine;
using UnityEngine.SceneManagement;
using WhiteKnuckleMP.Framework.Managers;
using WhiteKnuckleMP.UI;
using WhiteKnuckleMP.Utils;
using WKLib;
using WKLib.API;

namespace WhiteKnuckleMP.Core;

[BepInPlugin(GUID, NAME, VERSION)]
[BepInDependency(WKLibPlugin.GUID, BepInDependency.DependencyFlags.HardDependency)]
public class WkMultiplayer : BaseUnityPlugin
{
    public const string GUID = "com.monksilly.WKMultiplayer";
    public const string NAME = "White Knuckle Multiplayer";
    public const string VERSION = "0.0.2";

    public static WKLibAPI LibAPI = WKLibAPI.Create(NAME, GUID);
    
    private bool loaded = false;

    private CommandManager commandManager;

    private NetworkManager gMan;
    
    
    

    private void Awake()
    {
        
        // Windows
        LibAPI.AddWindow(WindowDeclarations.MainWin);
        
        // Mod List Buttons
        LibAPI.AddToModList(new WKModTab());
        
        
        LogManager.Init(Logger);

        GameObject gmObj = new GameObject("MultiplayerManager");
        DontDestroyOnLoad(gmObj);
        gMan = gmObj.AddComponent<NetworkManager>();
        
        
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

    private void OnDisable()
    {
        LibAPI.Destroy();
    }
}