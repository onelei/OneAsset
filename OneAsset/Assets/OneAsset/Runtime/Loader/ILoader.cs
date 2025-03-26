using System;
using Cysharp.Threading.Tasks;

namespace OneAsset.Runtime.Loader
{
    public interface ILoader
    {
        string GetAssetBundleName(string assetName);

        T LoadAsset<T>(string assetName) where T : UnityEngine.Object;

        UniTaskVoid LoadAssetAsync<T>(string assetName, Action<T> onComplete) where T : UnityEngine.Object;

        void UnloadAsset(string bundleName, bool unloadAllLoadedObjects = false);
    }
}