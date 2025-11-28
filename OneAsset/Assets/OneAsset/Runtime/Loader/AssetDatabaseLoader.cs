#if UNITY_EDITOR
using System;
using Cysharp.Threading.Tasks;
using OneAsset.Runtime.Rule;
using UnityEditor;

namespace OneAsset.Runtime.Loader
{
    public class AssetDatabaseLoader : BaseLoader
    {
        public AssetDatabaseLoader(string packageName, IEncryptRule encryptRule) : base(packageName, encryptRule)
        {
        }

        public override T LoadAsset<T>(string address)
        {
            var assetPath = GetVirtualManifest().GetAssetPathByAddress(address);
            return AssetDatabase.LoadAssetAtPath<T>(assetPath);
        }

        public async override UniTaskVoid LoadAssetAsync<T>(string address, Action<T> onComplete)
        {
            var asset = LoadAsset<T>(address);
            onComplete?.Invoke(asset);
        }

        public override void UnloadAsset(string address, bool unloadAllLoadedObjects = false)
        {
        }

        public override void UnloadUnusedBundles(bool immediate = false, bool unloadAllLoadedObjects = true)
        {
        }
    }
}
#endif