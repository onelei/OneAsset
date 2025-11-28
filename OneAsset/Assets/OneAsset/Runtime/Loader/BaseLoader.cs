using System;
using Cysharp.Threading.Tasks;
using OneAsset.Runtime.Manifest;
using OneAsset.Runtime.Rule;

namespace OneAsset.Runtime.Loader
{
    public class BaseLoader : ILoader
    {
        protected readonly string PackageName;
        protected readonly IEncryptRule EncryptRule;
        private VirtualManifest _virtualManifest;

        protected BaseLoader(string packageName, IEncryptRule encryptRule)
        {
            PackageName = packageName;
            EncryptRule = encryptRule;
        }

        protected VirtualManifest GetVirtualManifest()
        {
            return _virtualManifest ?? (_virtualManifest = VirtualManifest.Load(PackageName));
        }
        
        public virtual bool ContainsAsset(string address)
        {
            return GetVirtualManifest().ContainsAddress(address);
        }

        public virtual T LoadAsset<T>(string address) where T : UnityEngine.Object
        {
            return default;
        }

        public virtual async UniTaskVoid LoadAssetAsync<T>(string address, Action<T> onComplete)
            where T : UnityEngine.Object
        {
        }

        public virtual void UnloadAsset(string address, bool unloadAllLoadedObjects = false)
        {
        }

        public virtual void UnloadUnusedBundles(bool immediate = false, bool unloadAllLoadedObjects = true)
        {
        }
    }
}