using HarmonyLib;
using UnityEngine;
using WhiteKnuckleMP.Framework.Managers;

namespace WhiteKnuckleMP.Core.Patches;

[HarmonyPatch(typeof(Item_Object))]
public static class ItemSpawnPatch
{
    [HarmonyPatch("Start"), HarmonyPrefix]
    public static bool StartPrefix(Item_Object __instance)
    {
        if (NetworkItemManager.Instance != null && !NetworkManager.Instance.IsServer &&
            StateManager.Instance.CurrentState == StateManager.State.InGame)
        {
            Object.Destroy(__instance.gameObject);
            return false; // Skip spawning logic on client
        }

        return true; // Continue as normal on host
    }

    [HarmonyPatch("Start"), HarmonyPostfix]
    public static void StartPostfix(Item_Object __instance)
    {
        if (NetworkManager.Instance != null && NetworkManager.Instance.IsServer)
        {
            NetworkItemManager.Instance.RegisterItem(__instance);
        }
    }
}