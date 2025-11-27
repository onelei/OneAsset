using System;
using Cysharp.Threading.Tasks;

namespace OneAsset.Runtime.Loader
{
    public interface ILoader
    {
        T LoadAsset<T>(string address) where T : UnityEngine.Object;

        UniTaskVoid LoadAssetAsync<T>(string address, Action<T> onComplete) where T : UnityEngine.Object;

        void UnloadAsset(string address, bool unloadAllLoadedObjects = false);
    }
}