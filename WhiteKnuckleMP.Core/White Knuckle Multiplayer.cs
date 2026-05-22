using BepInEx;
using HarmonyLib;
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
    
    private bool _loaded;

    private bool _engineSetup;
    
    private CommandManager _commandManager;

    private Harmony _harmony = new($"{GUID}.patches");
    

    private void Awake()
    {
        
        // Windows
        LibAPI.AddWindow(WindowDeclarations.MainWin);
        LibAPI.AddWindow(WindowDeclarations.JoinHostWin);
        
        // Mod List Buttons
        LibAPI.AddToModList(new WKModTab());
        
        
        LogManager.Init(Logger);
        
        
        SceneManager.sceneLoaded += OnSceneLoad;

        LogManager.Info($"Plugin {MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} is loaded!");
    }

    private void OnSceneLoad(Scene scene, LoadSceneMode mode)
    {
        // ADDED CASE FOR "Intro" SCENE
        if (scene.name == "Intro")
        {
            // ? why
            // SceneManager.LoadScene("Main-Menu");
            return; 
        } 
        if (scene.name == "Main-Menu")
        {
            if (!_engineSetup)
                SetupEngine();
        }

        switch (_loaded)
        {
            case false when scene.name == "Game-Main":
                _commandManager = new CommandManager(NetworkManager.Instance);
                    
                AddCommands();
                _loaded = true;
                break;
                    
            case true when scene.name == "Game-Main":
                AddCommands();
                break;
        }
    }

    private void AddCommands()
    {
        if (_commandManager == null)
        {
            LogManager.Error("Cannot add commands - CommandManager is null");
            return;
        }
        
        // Add commands to CommandConsole
        CommandConsole.AddCommand("host", _commandManager.HandleLocalHostCommand, false);
        CommandConsole.AddCommand("join", _commandManager.HandleLocalJoinCommand, false);
        CommandConsole.AddCommand("disconnect", _commandManager.HandleDisconnectCommand, false);
        
        LogManager.Info("Commands registered successfully");
    }

    private void SetupEngine()
    {
        LogManager.Info("Setting up WhiteKnuckleMP Engine...");
        GameObject engine = new GameObject("WhiteKnuckleMP_Engine");
        DontDestroyOnLoad(engine);

        engine.AddComponent<StateManager>();
        engine.AddComponent<NetworkManager>();
        engine.AddComponent<NetworkItemManager>();
        engine.AddComponent<PlayerManager>();
        engine.AddComponent<LobbyManager>();
        LogManager.Info("WhiteKnuckleMP Engine was successfully setup!");

        _harmony.PatchAll();
        _engineSetup = true;
    }
    
    private void OnDisable()
    {
        LibAPI.Destroy();
    }
}