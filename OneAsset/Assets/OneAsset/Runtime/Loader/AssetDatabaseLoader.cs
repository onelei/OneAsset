#if UNITY_EDITOR
using System;
using Cysharp.Threading.Tasks;
using OneAsset.Runtime.Manifest;
using OneAsset.Runtime.Rule;
using UnityEditor;

namespace OneAsset.Runtime.Loader
{
    public class AssetDatabaseLoader : ILoader
    {
        private readonly string _packageName;
        private readonly IEncryptRule _encryptRule;

        public AssetDatabaseLoader(string packageName, IEncryptRule encryptRule)
        {
            _packageName = packageName;
            _encryptRule = encryptRule;
        }

        public T LoadAsset<T>(string address) where T : UnityEngine.Object
        {
            var assetPath = VirtualManifest.Default.GetAssetPathByAddress(address);
            return AssetDatabase.LoadAssetAtPath<T>(assetPath);
        }

        public async UniTaskVoid LoadAssetAsync<T>(string address, Action<T> onComplete) where T : UnityEngine.Object
        {
            var asset = LoadAsset<T>(address);
            onComplete?.Invoke(asset);
        }

        public void UnloadAsset(string address, bool unloadAllLoadedObjects = false)
        {
        }

        public void UnloadUnusedBundles(bool immediate = false, bool unloadAllLoadedObjects = true)
        {
        }
    }
}
#endif