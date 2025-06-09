using System;
using UnityEngine;
using HarmonyLib;
using Steamworks;

namespace White_Knuckle_Multiplayer.Managers;

[DisallowMultipleComponent]
[HarmonyPatch(typeof(SteamManager), "Awake")]
public class SteamManagerAwakePatch : MonoBehaviour
{

	private const uint AppID = 3195790;

    [HarmonyPrefix]
    private static bool AwakePrefix()
    {
	    return false;
    }
    
    [HarmonyPostfix]
    private static void AwakePostfix(SteamManager __instance) {
	    if (SteamManager.instance == null && SteamManager.instance != __instance)
	    {
		    SteamManager.instance = __instance;
	    }
	    else
	    {
		    Destroy(__instance.gameObject);
		    return;
	    }

	    GameObject lobbyManagerObject;
	    
	    LogManager.SteamClient.Warn("Patching SteamManager, expect breakage for scores");
	    var steamAPIInitialized = SteamAPI.Init();
	    
	    if (!steamAPIInitialized) {
		    Debug.LogError("[Steamworks.NET] SteamAPI_Init() failed. Refer to Valve's documentation or the comment above this line for more information.", __instance);
		    SteamManager.initialized = false;
		    
		    LogManager.SteamClient.Warn("LobbyManager created From patch!");
		    lobbyManagerObject = new GameObject("LobbyManager");
		    DontDestroyOnLoad(lobbyManagerObject);
		    lobbyManagerObject.AddComponent<LobbyManager>();
		    
		    return;
	    }
	    SteamManager.initialized = true;
	    
	    try {
		    if (SteamAPI.RestartAppIfNecessary((AppId_t)AppID)) {
			    SteamManager.connected = false;
			    Application.Quit();
			    return;
		    }
	    }
	    catch (System.DllNotFoundException e) {
		    Debug.LogError("[Steamworks.NET] Could not load [lib]steam_api.dll/so/dylib. It's likely not in the correct location. Refer to the README for more details.\n" + e, __instance);
			
		    SteamManager.connected = false;
		    
		    Application.Quit();
		    return;
	    }

	    SteamManager.connected = true;
	    
	    // var instanceField = AccessTools.Field(typeof(SteamManager), "instance");
	    // instanceField.SetValue(__instance, __instance);
	    
	    DontDestroyOnLoad(__instance.gameObject);
	    
		LogManager.SteamClient.Warn("LobbyManager created From patch!");
		lobbyManagerObject = new GameObject("LobbyManager");
		DontDestroyOnLoad(lobbyManagerObject);
		lobbyManagerObject.AddComponent<LobbyManager>();
	}
    
}

[HarmonyPatch(typeof(SteamManager), "Update")]
public class SteamManagerUpdatePatch : MonoBehaviour
{

	[HarmonyPrefix]
	private static bool UpdatePrefix()
	{
		try
		{
			SteamAPI.RunCallbacks();
		}
		catch
		{
			return false;
		}
		
		return false;
	}
}

[HarmonyPatch(typeof(SteamManager), "Shutdown")]
public class SteamManagerShutdownPatch : MonoBehaviour
{
	[HarmonyPrefix]
	private static bool ShutdownPrefix()
	{
		if (SteamManager.hasShutdown)
		{
			return false;
		}
		LogManager.SteamClient.Warn("Patch on Shutdown says hi :3");
		SteamManager.hasShutdown = true;
		SteamAPI.RunCallbacks();

		return false;
	}
}