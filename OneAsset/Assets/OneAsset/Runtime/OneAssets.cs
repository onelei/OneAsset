using System;
using OneAsset.Runtime.Manifest;

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
        private static OneAssetPackage _oneAssetPackage;

        public static void Init(EPlayMode playMode)
        {
            _playMode = playMode;
        }
        
        public static void SetPackage(OneAssetPackage oneAssetPackage)
        {
            _oneAssetPackage = oneAssetPackage;
        }

        public static EPlayMode GetPlayMode() => _playMode;

        public static T LoadAsset<T>(string assetPath) where T : UnityEngine.Object
        {
            return _oneAssetPackage.LoadAsset<T>(assetPath);
        }

        public static void LoadAssetAsync<T>(string assetPath, Action<T> onComplete) where T : UnityEngine.Object
        {
            _oneAssetPackage.LoadAssetAsync<T>(assetPath, onComplete);
        }

        public static void UnloadAsset(string assetPath)
        {
            _oneAssetPackage.UnloadAsset(assetPath);
        }
    }
}