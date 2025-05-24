using System.IO;
using System.Reflection;
using UnityEngine;

namespace White_Knuckle_Multiplayer.Utils
{
  public static class AssetBundleLoader
  {
    private static AssetBundle _mainAssetBundle;
    private static GameObject _clPlayerPrefabInternal;

    public static GameObject CL_Player_Prefab => AssetBundleLoader._clPlayerPrefabInternal;

    public static bool InitializeAndLoadAssets()
    {
      AssetBundleLoader._mainAssetBundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "playerprefab"));
      if ((Object) AssetBundleLoader._mainAssetBundle == (Object) null)
      {
        LogManager.Error("AssetBundleLoader: Failed to load 'playerprefab' asset bundle.");
        return false;
      }
      if (!AssetBundleLoader.LoadAssetFromBundle<GameObject>(AssetBundleLoader._mainAssetBundle, "CL_Player", out AssetBundleLoader._clPlayerPrefabInternal))
      {
        LogManager.Error("AssetBundleLoader: Failed to load prefab 'CL_Player' from 'playerprefab' bundle.");
        AssetBundleLoader._mainAssetBundle.Unload(true);
        AssetBundleLoader._mainAssetBundle = (AssetBundle) null;
        return false;
      }
      LogManager.Info("AssetBundleLoader: CL_Player prefab loaded successfully.");
      return true;
    }

    private static bool LoadAssetFromBundle<T>(
      AssetBundle bundle,
      string assetPathInBundle,
      out T loadedObject)
      where T : Object
    {
      loadedObject = bundle.LoadAsset<T>(assetPathInBundle);
      if ((bool) (Object) loadedObject)
        return true;
      LogManager.Error("AssetBundleLoader: LoadAssetFromBundle - Could not load '" + assetPathInBundle + "' as " + typeof (T).Name + ".");
      return false;
    }

    public static void UnloadAllAssets()
    {
      if (!((Object) AssetBundleLoader._mainAssetBundle != (Object) null))
        return;
      AssetBundleLoader._mainAssetBundle.Unload(true);
      AssetBundleLoader._mainAssetBundle = (AssetBundle) null;
      AssetBundleLoader._clPlayerPrefabInternal = (GameObject) null;
      LogManager.Info("AssetBundleLoader: Unloaded all assets.");
    }
  }
}
