#if UNITY_EDITOR
using System;
using Cysharp.Threading.Tasks;
using OneAsset.Runtime.Manifest;
using UnityEditor;

namespace OneAsset.Runtime.Loader
{
    public class AssetDatabaseLoader : ILoader
    {

        public T LoadAsset<T>(string address) where T : UnityEngine.Object
        {
            var assetPath = VirtualManifest.Default.GetAssetPathByAddress(address);
            return AssetDatabase.LoadAssetAtPath<T>(assetPath);
        }

        public async UniTaskVoid LoadAssetAsync<T>(string assetName, Action<T> onComplete) where T : UnityEngine.Object
        {
            var asset = LoadAsset<T>(assetName);
            onComplete?.Invoke(asset);
        }

        public void UnloadAsset(string assetName, bool unloadAllLoadedObjects = false)
        {
        }
    }
}
#endif