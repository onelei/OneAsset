using System;

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
        private static OneAssetPackage _package;

        public static void SetPlayMode(EPlayMode playMode)
        {
            _playMode = playMode;
        }

        public static void SetPackage(OneAssetPackage package)
        {
            _package = package;
        }

        public static EPlayMode GetPlayMode() => _playMode;

        public static T LoadAsset<T>(string assetPath) where T : UnityEngine.Object
        {
            return _package.LoadAsset<T>(assetPath);
        }

        public static void LoadAssetAsync<T>(string assetPath, Action<T> onComplete) where T : UnityEngine.Object
        {
            _package.LoadAssetAsync<T>(assetPath, onComplete);
        }

        public static void UnloadAsset(string assetPath)
        {
            _package.UnloadAsset(assetPath);
        }

        public static void UnloadUnusedBundles(bool immediate = false, bool unloadAllLoadedObjects = true)
        {
            _package.UnloadUnusedBundles(immediate, unloadAllLoadedObjects);
        }
    }
}