using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace White_Knuckle_Multiplayer.Utils;

public class AssetBundleLoader
{
    private static AssetBundle _assets;
    internal static GameObject PlayerPrefab;
    internal static GameObject LobbyScreen;
    internal static GameObject LobbyButton;

    
    internal static bool Load()
    {
        _assets = AssetBundle.LoadFromFile(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/Assets/multiplayer_assets");
        if (!_assets)
        {
            LogManager.Error("Failed to load AssetBundle, aborting!");
            return false;
        }

        List<bool> loadResults =
        [
            //LoadFile(_assets, "Assets/Neb.Assets/White Knuckle Multiplayer/playerPrefab.prefab", out PlayerPrefab),
            LoadFile(_assets, "Assets/Neb.Assets/White Knuckle Multiplayer/Multiplayer Lobby.prefab", out LobbyScreen),
            LoadFile(_assets, "Assets/Neb.Assets/White Knuckle Multiplayer/Multiplayer Button.prefab", out LobbyButton)
        ];
        
        if (loadResults.Any(result => result == false))
        {
            LogManager.Warn("Failed to load one or more assets, aborting!");
            return false;
        }

        return true;
    }
    
    private static bool LoadFile<T>(AssetBundle assets, string path, out T loadedObject) where T : Object
    {
        loadedObject = assets.LoadAsset<T>(path);
        if (!loadedObject)
        {
            LogManager.Error($"Failed to load '{path}'");
            return false;
        }
        
        return true;
    }
}