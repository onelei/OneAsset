using System;
using System.Collections.Generic;

namespace OneAsset.Runtime
{
    public enum EPlayMode
    {
        Simulate,
        AssetBundle,
    }

    public static class OneAssets
    {
        private static EPlayMode _playMode = EPlayMode.Simulate;

        private static readonly Dictionary<string, OneAssetPackage>
            Packages = new Dictionary<string, OneAssetPackage>();

        public static void SetPlayMode(EPlayMode playMode)
        {
            _playMode = playMode;
        }

        public static void AddPackage(OneAssetPackage package)
        {
            Packages[package.PackageName] = package;
        }

        public static EPlayMode GetPlayMode() => _playMode;

        private static OneAssetPackage GetPackageByAssetPath(string assetPath)
        {
            using (var tor = Packages.GetEnumerator())
            {
                while (tor.MoveNext())
                {
                    var package = tor.Current.Value;
                    if (package.ContainsAsset(assetPath))
                    {
                        return package;
                    }
                }
            }

            OneAssetLogger.LogError($"Asset not found: {assetPath}");
            return null;
        }

        public static T LoadAsset<T>(string assetPath) where T : UnityEngine.Object
        {
            return GetPackageByAssetPath(assetPath).LoadAsset<T>(assetPath);
        }

        public static void LoadAssetAsync<T>(string assetPath, Action<T> onComplete) where T : UnityEngine.Object
        {
            GetPackageByAssetPath(assetPath).LoadAssetAsync<T>(assetPath, onComplete);
        }

        public static void UnloadAsset(string assetPath)
        {
            GetPackageByAssetPath(assetPath).UnloadAsset(assetPath);
        }

        public static void UnloadUnusedBundles(bool immediate = false, bool unloadAllLoadedObjects = true)
        {
            using (var tor = Packages.GetEnumerator())
            {
                while (tor.MoveNext())
                {
                    var package = tor.Current.Value;
                    package.UnloadUnusedBundles(immediate, unloadAllLoadedObjects);
                }
            }
        }
    }
}