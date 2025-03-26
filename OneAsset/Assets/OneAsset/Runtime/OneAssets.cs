using System;
using OneAsset.Runtime.Loader;

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

        public static void Init(EPlayMode playMode)
        {
            _playMode = playMode;
        }

        public static EPlayMode GetPlayMode() => _playMode;

        public static T LoadAsset<T>(string assetPath) where T : UnityEngine.Object
        {
            return LoaderHandler.Default().LoadAsset<T>(assetPath);
        }

        public static void LoadAssetAsync<T>(string assetPath, Action<T> onComplete) where T : UnityEngine.Object
        {
            LoaderHandler.Default().LoadAssetAsync<T>(assetPath, onComplete);
        }

        public static void UnloadAsset(string assetPath)
        {
            LoaderHandler.Default().UnloadAsset(assetPath);
        }
    }
}