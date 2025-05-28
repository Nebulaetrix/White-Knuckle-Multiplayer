using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using White_Knuckle_Multiplayer.Networking.Routing;

namespace White_Knuckle_Multiplayer.Networking.Controllers;

[HarmonyPatch(typeof(M_Gamemode))]
public class WorldLoaderController : MonoBehaviour
{
    private static List<M_Level> _generationList;

    internal void NameToLevel(List<string> levelNames)
    {
        var result = levelNames.Select(level => CL_AssetManager.instance.assetDatabase.levelPrefabs.FirstOrDefault(x => x.name == level)?.GetComponent<M_Level>()).ToList();
        _generationList = result;
    }

    [HarmonyPatch("GetGenerationList"), HarmonyPostfix]
    private static void GetGenerationListPatch(List<M_Level> __result)
    {
        LogManager.Info("GetGenerationListPatch: Started");
    
        if (IsClientAndHasGenerationList())
        {
            SyncClientLevels(__result);
            return;
        }
    
        if (!IsServerRunning())
        {
            LogManager.Info("GetGenerationListPatch: Server not running, skipping sync");
            return;
        }
    
        SyncServerLevels(__result);
    }
    
    private static bool IsClientAndHasGenerationList() => 
        NetworkClient.Instance != null && _generationList != null;
    
    private static bool IsServerRunning() =>
        NetworkServer.Instance?.Server.IsRunning ?? false;
    
    private static void SyncClientLevels(List<M_Level> result)
    {
        LogManager.Info($"GetGenerationListPatch: Syncing {_generationList.Count} client levels");
        result.Clear();
        result.AddRange(_generationList);
        _generationList.Clear();
    }
    
    private static void SyncServerLevels(List<M_Level> result)
    {
        var levelNames = result.Select(level => level.levelName).ToArray();
        LogManager.Info($"GetGenerationListPatch: Sending {levelNames.Length} server levels");
        MessageSender.SendLevelData(levelNames);
    }
}