using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using White_Knuckle_Multiplayer.Managers;
using White_Knuckle_Multiplayer.Networking;
using White_Knuckle_Multiplayer.Utils;

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
        
        if (!AssetBundleLoader.Load())
        {
            LogManager.Error($"{MyPluginInfo.PLUGIN_GUID} failed to load critical assets from bundle. CL_Player prefab will be null.");
        }
        GameManager = new GameManager();

        var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        harmony.PatchAll();

        SceneManager.sceneLoaded += OnSceneLoad;
        
        LogManager.Info($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }
    
    
    private void OnSceneLoad(Scene scene, LoadSceneMode mode)
    {
        GameManager.InitializeWKNetworking();
        switch (loaded)
        {
            case false when scene.name == "Game-Main":
                
                    
                if (coroutineRunner == null)
                {
                    var coroutineObject = new UnityEngine.GameObject("CoroutineRunner");
                    UnityEngine.Object.DontDestroyOnLoad(coroutineObject);
                    coroutineRunner = coroutineObject.AddComponent<CoroutineRunner>();
                }
                    
                commandManager = new CommandManager(GameManager, coroutineRunner, coroutineRunner);
                    
                AddCommands();
                loaded = true;
                break;
                    
            case true when scene.name == "Game-Main":
                AddCommands();
                break;
        }

        if (scene.name == "Main-Menu")
        {
            var menuButtons = GameObject.Find("Canvas/Main Menu/Main Menu Buttons");
            if (menuButtons == null)
            {
               LogManager.Error("Failed to find 'Main Menu Buttons'");
               return;
            }

            var screens = GameObject.Find("Screens");
            if (screens == null)
            {
               LogManager.Error("Failed to find 'Screens'");
               return;
            }


            menuButtons.transform.Find("Image").gameObject.SetActive(false);
            var mpButton = Instantiate(AssetBundleLoader.LobbyButton, menuButtons.transform);
            mpButton.transform.SetSiblingIndex(3);

            var lobbyPane = Instantiate(AssetBundleLoader.LobbyScreen, screens.transform);
            lobbyPane.AddComponent<UiManager>();
            
            mpButton.GetComponent<Button>().onClick.AddListener(() =>
            { 
                lobbyPane.GetComponent<UI_LerpOpen>().Show();
                LobbyManager.Instance.CreateLobby();
            });
            
            
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
        CommandConsole.AddCommand("steamhost", commandManager.HandleSteamHostCommand, false);
        CommandConsole.AddCommand("steamjoin", commandManager.HandleSteamJoinCommand, false);
        CommandConsole.AddCommand("lobbycreate", commandManager.HandleSteamLobbyCreate, false);
        CommandConsole.AddCommand("lobbyjoin", commandManager.HandleSteamLobbyJoin, false);
        CommandConsole.AddCommand("lobbyleave", commandManager.HandleSteamLobbyLeave, false);
        
        LogManager.Info("Commands registered successfully");
    }
}