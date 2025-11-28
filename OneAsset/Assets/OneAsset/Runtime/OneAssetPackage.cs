using System;
using OneAsset.Runtime.Loader;
using OneAsset.Runtime.Rule;

namespace OneAsset.Runtime
{
    public class OneAssetPackage
    {
        public readonly string PackageName;
        private readonly IEncryptRule _encryptRule;
        private ILoader _loader;

        public OneAssetPackage(string packageName, IEncryptRule encryptRule)
        {
            PackageName = packageName;
            _encryptRule = encryptRule;
            InitializeLoader();
        }

        private void InitializeLoader()
        {
            var playMode = OneAssets.GetPlayMode();
#if !UNITY_EDITOR
            playMode = EPlayMode.AssetBundle;
#endif
            if (playMode == EPlayMode.AssetBundle)
            {
                _loader = new AssetBundleLoader(PackageName, _encryptRule);
            }
            else
            {
                _loader = new AssetDatabaseLoader(PackageName, _encryptRule);
            }
        }
        
        public bool ContainsAsset(string assetPath)
        {
            return _loader.ContainsAsset(assetPath);
        }

        public T LoadAsset<T>(string assetPath) where T : UnityEngine.Object
        {
            return _loader.LoadAsset<T>(assetPath);
        }

        public void LoadAssetAsync<T>(string assetPath, Action<T> onComplete) where T : UnityEngine.Object
        {
            _loader.LoadAssetAsync<T>(assetPath, onComplete);
        }

        public void UnloadAsset(string assetPath)
        {
            _loader.UnloadAsset(assetPath);
        }

        public void UnloadUnusedBundles(bool immediate = false, bool unloadAllLoadedObjects = true)
        {
            _loader.UnloadUnusedBundles(immediate, unloadAllLoadedObjects);
        }
    }
}